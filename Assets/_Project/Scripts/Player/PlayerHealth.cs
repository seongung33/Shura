using UnityEngine;
using UnityEngine.Events;

namespace Shura.Player
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private UnityEvent onDeath;

        [Header("Debug (읽기 전용, Play 모드에서 확인용)")]
        [SerializeField] private float currentHealth;
        [SerializeField] private bool isDead;

        private NetworkPlayerHealth networkPlayerHealth;

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
        }

        public void TakeDamage(float amount)
        {
            if (isDead || amount <= 0f)
            {
                return;
            }

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

            currentHealth = Mathf.Clamp(health, 0f, maxHealth);
            isDead = dead;

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
}
