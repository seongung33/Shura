using UnityEngine;
using UnityEngine.Events;

namespace Shura.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private UnityEvent onDeath;

        [Header("Debug (읽기 전용, Play 모드에서 확인용)")]
        [SerializeField] private int currentHealth;
        [SerializeField] private bool isDead;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsDead => isDead;

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        public void TakeDamage(int amount)
        {
            if (isDead || amount <= 0) return;

            currentHealth = Mathf.Max(0, currentHealth - amount);

            if (currentHealth == 0)
            {
                Die();
            }
        }

        private void Die()
        {
            isDead = true;
            onDeath.Invoke();
        }
    }
}
