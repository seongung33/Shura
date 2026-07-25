using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField]
    private float maxHealth = 100f;

    private float currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        Debug.Log($"적 체력: {currentHealth} / {maxHealth}");

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        Destroy(gameObject);
    }

    [ContextMenu("Test Damage")]
    private void TestDamage()
    {
        TakeDamage(25f);
    }
}