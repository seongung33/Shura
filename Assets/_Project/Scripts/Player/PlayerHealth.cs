using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField]
    private float maxHealth = 100f;

    private float currentHealth;
    private bool IsDead;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    private void Awake()
    {
        currentHealth = maxHealth;
        IsDead = false;
    }

    public void TakeDamage(float damage)
    {
        if (IsDead)
        {
            return;
        }

        if (damage <= 0f)
        {
            return;
        }

        currentHealth -= damage;

        if (currentHealth < 0f)
        {
            currentHealth = 0f;
        }

        Debug.Log($"플레이어 체력: {currentHealth} / {maxHealth}");

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;

        Debug.Log("플레이어 사망");
        Destroy(gameObject);
    }

    [ContextMenu("Test Damage")]
    private void TestDamage()
    {
        TakeDamage(10f);
    }
}