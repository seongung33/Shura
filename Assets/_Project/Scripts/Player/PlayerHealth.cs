using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Shura.Player
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private UnityEvent onDeath;

        [SerializeField, Min(0f)]
        private float hitInvulnerabilityDuration = 0.4f;

        [Header("Debug (읽기 전용, Play 모드에서 확인용)")]
        [SerializeField] private float currentHealth;
        [SerializeField] private bool isDead;

        private NetworkPlayerHealth networkPlayerHealth;
        private PlayerHitFeedback hitFeedback;
        private float nextDamageTime;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => isDead;

        public void ConfigureMaxHealth(float value)
        {
            if (value <= 0f || float.IsNaN(value) || float.IsInfinity(value))
            {
                return;
            }

            maxHealth = value;
            currentHealth = Mathf.Min(currentHealth, maxHealth);
        }

        private void Awake()
        {
            currentHealth = maxHealth;
            networkPlayerHealth = GetComponent<NetworkPlayerHealth>();
            hitFeedback = GetComponent<PlayerHitFeedback>();
            hitFeedback ??= gameObject.AddComponent<PlayerHitFeedback>();
        }

        public void TakeDamage(float amount)
        {
            if (isDead || amount <= 0f || Time.time < nextDamageTime)
            {
                return;
            }

            nextDamageTime = Time.time + hitInvulnerabilityDuration;

            if (networkPlayerHealth != null &&
                networkPlayerHealth.IsSpawned)
            {
                networkPlayerHealth.TakeDamageServer(amount);
                return;
            }

            currentHealth = Mathf.Max(
                0f,
                currentHealth - amount
            );
            hitFeedback.Play(amount, currentHealth <= 0f);
            Debug.Log($"Player 체력: {currentHealth}/ {maxHealth}");

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        public bool Heal(float amount)
        {
            if (isDead ||
                amount <= 0f ||
                currentHealth >= maxHealth)
            {
                return false;
            }

            if (networkPlayerHealth != null &&
                networkPlayerHealth.IsSpawned)
            {
                return networkPlayerHealth.RequestHeal(amount);
            }

            currentHealth = Mathf.Min(
                maxHealth,
                currentHealth + amount
            );
            Debug.Log($"Player 체력: {currentHealth}/ {maxHealth}");

            return true;
        }

        public void ApplyNetworkState(float health, bool dead)
        {
            bool wasDead = isDead;
            float previousHealth = currentHealth;

            currentHealth = Mathf.Clamp(health, 0f, maxHealth);
            isDead = dead;

            if (currentHealth < previousHealth)
            {
                hitFeedback.Play(previousHealth - currentHealth, isDead);
            }

            if (!wasDead && isDead)
            {
                onDeath?.Invoke();
            }
        }

        private void Die()
        {
            isDead = true;
            onDeath?.Invoke();
        }
    }

    public sealed class PlayerHitFeedback : MonoBehaviour
    {
        private static int recoilSequence;

        private readonly Color hitTint = new Color(1f, 0.38f, 0.32f, 1f);
        private Coroutine feedbackRoutine;
        private SpriteRenderer[] activeRenderers;
        private Color[] originalColors;
        private Transform motionRoot;
        private Vector3 originalLocalPosition;
        private Vector3 originalLocalScale;

        public void Play(float damage, bool defeated)
        {
            if (!isActiveAndEnabled || damage <= 0f)
            {
                return;
            }

            RestoreVisuals();
            feedbackRoutine = StartCoroutine(PlayFeedback(defeated));

            if (!IsLocalPlayer())
            {
                return;
            }

            Shura.Camera.CameraFollow cameraFollow =
                FindFirstObjectByType<Shura.Camera.CameraFollow>();
            cameraFollow?.Shake(defeated ? 0.055f : 0.035f, 0.09f);
            GameAudioController.PlayPlayerHurt(defeated);
        }

        private IEnumerator PlayFeedback(bool defeated)
        {
            activeRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            originalColors = new Color[activeRenderers.Length];

            for (int index = 0; index < activeRenderers.Length; index++)
            {
                SpriteRenderer renderer = activeRenderers[index];
                originalColors[index] = renderer != null
                    ? renderer.color
                    : Color.white;
            }

            motionRoot = ResolveMotionRoot();

            if (motionRoot != null)
            {
                originalLocalPosition = motionRoot.localPosition;
                originalLocalScale = motionRoot.localScale;
            }

            float duration = defeated ? 0.24f : 0.18f;
            float elapsed = 0f;
            float recoilDirection = (++recoilSequence & 1) == 0 ? -1f : 1f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(progress * Mathf.PI);
                float tintStrength = pulse * (defeated ? 0.58f : 0.42f);

                for (int index = 0; index < activeRenderers.Length; index++)
                {
                    SpriteRenderer renderer = activeRenderers[index];

                    if (renderer != null)
                    {
                        renderer.color = Color.Lerp(
                            originalColors[index],
                            hitTint,
                            tintStrength
                        );
                    }
                }

                if (motionRoot != null)
                {
                    float recoil = Mathf.Sin(progress * Mathf.PI) * 0.065f;
                    motionRoot.localPosition = originalLocalPosition +
                        Vector3.right * recoil * recoilDirection;
                    motionRoot.localScale = new Vector3(
                        originalLocalScale.x * (1f + pulse * 0.035f),
                        originalLocalScale.y * (1f - pulse * 0.055f),
                        originalLocalScale.z
                    );
                }

                yield return null;
            }

            feedbackRoutine = null;
            RestoreVisuals();
        }

        private Transform ResolveMotionRoot()
        {
            Transform visualRoot = transform.Find("CharacterVisualRoot");

            if (visualRoot != null)
            {
                return visualRoot;
            }

            for (int index = 0; index < transform.childCount; index++)
            {
                Transform child = transform.GetChild(index);

                if (child.name.EndsWith("Visual"))
                {
                    return child;
                }
            }

            return null;
        }

        private bool IsLocalPlayer()
        {
            NetworkObject networkObject = GetComponent<NetworkObject>();
            return networkObject == null ||
                !networkObject.IsSpawned ||
                networkObject.IsOwner;
        }

        private void RestoreVisuals()
        {
            if (feedbackRoutine != null)
            {
                StopCoroutine(feedbackRoutine);
                feedbackRoutine = null;
            }

            if (activeRenderers != null && originalColors != null)
            {
                int count = Mathf.Min(
                    activeRenderers.Length,
                    originalColors.Length
                );

                for (int index = 0; index < count; index++)
                {
                    if (activeRenderers[index] != null)
                    {
                        activeRenderers[index].color = originalColors[index];
                    }
                }
            }

            if (motionRoot != null)
            {
                motionRoot.localPosition = originalLocalPosition;
                motionRoot.localScale = originalLocalScale;
            }

            activeRenderers = null;
            originalColors = null;
            motionRoot = null;
        }

        private void OnDisable()
        {
            RestoreVisuals();
        }
    }
}
