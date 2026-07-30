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

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => isDead;

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        public void TakeDamage(float amount)
        {
            if (isDead || amount <= 0f)
            {
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

            currentHealth = Mathf.Min(
                maxHealth,
                currentHealth + amount
            );
            Debug.Log($"Player 체력: {currentHealth}/ {maxHealth}");

            return true;
        }

        private void Die()
        {
            isDead = true;
            onDeath?.Invoke();
        }
    }
}