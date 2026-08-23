using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Shura.Player
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private UnityEvent onDeath;

        [Header("Hit Invulnerability")]
        [Tooltip("피해를 정상적으로 받은 뒤 모든 추가 피해를 무시하는 시간(초)")]
        [SerializeField, Min(0f)]
        private float hitInvulnerabilityDuration = 0.7f;

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
            hitFeedback.Play(
                amount,
                currentHealth <= 0f,
                hitInvulnerabilityDuration
            );
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
                hitFeedback.Play(
                    previousHealth - currentHealth,
                    isDead,
                    hitInvulnerabilityDuration
                );
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

        private Coroutine feedbackRoutine;
        private SpriteRenderer[] sourceRenderers;
        private SpriteRenderer[] overlayRenderers;
        private Transform motionRoot;
        private Vector3 originalLocalPosition;
        private Vector3 originalLocalScale;

        public void Play(
            float damage,
            bool defeated,
            float invulnerabilityDuration
        )
        {
            if (!isActiveAndEnabled || damage <= 0f)
            {
                return;
            }

            RestoreVisuals();
            feedbackRoutine = StartCoroutine(
                PlayFeedback(defeated, invulnerabilityDuration)
            );

            if (!IsLocalPlayer())
            {
                return;
            }

            CombatFeedbackPresenter.PlayPlayerHit(transform, damage);

            Shura.Camera.CameraFollow cameraFollow =
                FindFirstObjectByType<Shura.Camera.CameraFollow>();
            cameraFollow?.Shake(defeated ? 0.055f : 0.035f, 0.09f);
            GameAudioController.PlayPlayerHurt(defeated);
        }

        private IEnumerator PlayFeedback(
            bool defeated,
            float invulnerabilityDuration
        )
        {
            CreateSilhouetteOverlays();

            motionRoot = ResolveMotionRoot();

            if (motionRoot != null)
            {
                originalLocalPosition = motionRoot.localPosition;
                originalLocalScale = motionRoot.localScale;
            }

            float duration = Mathf.Max(0.01f, invulnerabilityDuration);
            float elapsed = 0f;
            float recoilDirection = (++recoilSequence & 1) == 0 ? -1f : 1f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Abs(
                    Mathf.Sin(progress * Mathf.PI * 3f)
                );
                float fadeOut = Mathf.Clamp01((1f - progress) / 0.15f);
                float overlayAlpha = (0.2f + pulse *
                    (defeated ? 0.4f : 0.34f)) * fadeOut;

                for (int index = 0; index < sourceRenderers.Length; index++)
                {
                    UpdateSilhouetteOverlay(index, overlayAlpha);
                }

                if (motionRoot != null)
                {
                    float motionPulse = Mathf.Sin(progress * Mathf.PI);
                    float recoil = motionPulse * 0.065f;
                    motionRoot.localPosition = originalLocalPosition +
                        Vector3.right * recoil * recoilDirection;
                    motionRoot.localScale = new Vector3(
                        originalLocalScale.x * (1f + motionPulse * 0.035f),
                        originalLocalScale.y * (1f - motionPulse * 0.055f),
                        originalLocalScale.z
                    );
                }

                yield return null;
            }

            feedbackRoutine = null;
            RestoreVisuals();
        }

        private void CreateSilhouetteOverlays()
        {
            SpriteRenderer[] renderers =
                GetComponentsInChildren<SpriteRenderer>(true);
            List<SpriteRenderer> validRenderers = new();

            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer != null &&
                    renderer.gameObject.name != "PlayerHitSilhouette")
                {
                    validRenderers.Add(renderer);
                }
            }

            sourceRenderers = validRenderers.ToArray();
            overlayRenderers = new SpriteRenderer[sourceRenderers.Length];

            for (int index = 0; index < sourceRenderers.Length; index++)
            {
                SpriteRenderer source = sourceRenderers[index];
                GameObject overlayObject = new GameObject(
                    "PlayerHitSilhouette",
                    typeof(SpriteRenderer)
                );
                overlayObject.layer = source.gameObject.layer;
                overlayObject.transform.SetParent(source.transform, false);
                overlayRenderers[index] =
                    overlayObject.GetComponent<SpriteRenderer>();
                UpdateSilhouetteOverlay(index, 0f);
            }
        }

        private void UpdateSilhouetteOverlay(int index, float alpha)
        {
            SpriteRenderer source = sourceRenderers[index];
            SpriteRenderer overlay = overlayRenderers[index];

            if (source == null || overlay == null)
            {
                return;
            }

            overlay.enabled = source.enabled &&
                source.gameObject.activeInHierarchy;
            overlay.sprite = source.sprite;
            overlay.flipX = source.flipX;
            overlay.flipY = source.flipY;
            overlay.drawMode = source.drawMode;
            overlay.size = source.size;
            overlay.maskInteraction = source.maskInteraction;
            overlay.spriteSortPoint = source.spriteSortPoint;
            overlay.sortingLayerID = source.sortingLayerID;
            overlay.sortingOrder = source.sortingOrder + 1;
            overlay.color = new Color(1f, 0.22f, 0.2f, alpha);
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

            if (overlayRenderers != null)
            {
                foreach (SpriteRenderer overlay in overlayRenderers)
                {
                    if (overlay != null)
                    {
                        overlay.gameObject.SetActive(false);
                        Destroy(overlay.gameObject);
                    }
                }
            }

            if (motionRoot != null)
            {
                motionRoot.localPosition = originalLocalPosition;
                motionRoot.localScale = originalLocalScale;
            }

            sourceRenderers = null;
            overlayRenderers = null;
            motionRoot = null;
        }

        private void OnDisable()
        {
            RestoreVisuals();
        }
    }
}
