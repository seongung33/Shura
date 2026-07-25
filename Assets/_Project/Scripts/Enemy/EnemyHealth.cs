using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField]
    private float maxHealth = 100f;

    [Header("Experience Drop")]

    private float currentHealth;

    [SerializeField]
    private ExperienceOrb experienceOrbPrefab;

    [SerializeField]
    private int experienceReward = 1;

    private bool isDead;
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
        if (isDead)
        {
            return;
        }
        isDead = true;
        if (experienceOrbPrefab != null)
        {
            ExperienceOrb experienceOrb = Instantiate(
                experienceOrbPrefab,
                transform.position,
                Quaternion.identity
            );
            experienceOrb.Initialize(experienceReward);

        }
        Destroy(gameObject);
    }

    [ContextMenu("Test Damage")]
    private void TestDamage()
    {
        TakeDamage(25f);
    }
}