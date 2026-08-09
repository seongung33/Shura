using UnityEngine;

public class MagnetPickup : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        AttractAllExperienceOrbs(other.transform);

        Destroy(gameObject);
    }

    public static void AttractAllExperienceOrbs(Transform target)
    {
        if (target == null)
        {
            return;
        }

        ExperienceOrb[] experienceOrbs =
            FindObjectsByType<ExperienceOrb>(
                FindObjectsSortMode.None
            );

        foreach (ExperienceOrb orb in experienceOrbs)
        {
            orb.AttractTo(target);
        }
    }
}
