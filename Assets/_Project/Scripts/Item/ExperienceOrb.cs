using UnityEngine;

public class ExperienceOrb : MonoBehaviour
{
    [SerializeField]
    private int experienceAmount = 1;

    [SerializeField, Min(0.1f)]
    private float attractionSpeed = 12f;

    private Transform attractionTarget;

    public int ExperienceAmount
    {
        get
        {
            return experienceAmount;
        }
    }

    private void Update()
    {
        if (attractionTarget == null)
        {
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            attractionTarget.position,
            attractionSpeed * Time.deltaTime
        );
    }

    public void Initialize(int amount)
    {
        experienceAmount = Mathf.Max(1, amount);
    }

    public void AttractTo(Transform target)
    {
        if (target == null)
        {
            return;
        }

        attractionTarget = target;
    }
}