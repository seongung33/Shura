using Shura.Player;
using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    [SerializeField, Min(0.1f)]
    private float healAmount = 20f;

    [SerializeField]
    private bool useMaxHealthPercentage;

    [SerializeField, Range(0f, 1f)]
    private float maxHealthPercentage = 0.3f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (!other.TryGetComponent(
                out PlayerHealth playerHealth))
        {
            return;
        }

        bool healed = useMaxHealthPercentage
            ? TryHealMaxHealthFraction(
                playerHealth,
                maxHealthPercentage
            )
            : playerHealth.Heal(healAmount);

        if (healed)
        {
            Destroy(gameObject);
        }
    }

    public static bool TryHealMaxHealthFraction(
        PlayerHealth playerHealth,
        float fraction
    )
    {
        if (playerHealth == null || fraction <= 0f)
        {
            return false;
        }

        return playerHealth.Heal(
            playerHealth.MaxHealth * Mathf.Clamp01(fraction)
        );
    }
}
