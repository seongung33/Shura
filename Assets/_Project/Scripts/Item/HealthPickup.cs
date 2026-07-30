using Shura.Player;
using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    [SerializeField, Min(0.1f)]
    private float healAmount = 20f;

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

        bool healed = playerHealth.Heal(healAmount);

        if (healed)
        {
            Destroy(gameObject);
        }
    }
}