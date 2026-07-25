using UnityEngine;

public class ExperienceOrb : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField]
    private int experienceAmount = 1;

    public int ExperienceAmount
    {
        get
        {
            return experienceAmount;
        }
    }

    public void Initialize(int amount)
    {
        experienceAmount = Mathf.Max(1, amount);
    }
}