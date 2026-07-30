using UnityEngine;

public class MagnetPickup : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        ExperienceOrb[] experienceOrbs =
            FindObjectsByType<ExperienceOrb>(
                FindObjectsSortMode.None
            );

        foreach (ExperienceOrb orb in experienceOrbs)
        {
            orb.AttractTo(other.transform);
        }

        Destroy(gameObject);
    }
}