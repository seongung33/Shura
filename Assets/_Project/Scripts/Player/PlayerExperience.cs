using UnityEngine;

public class PlayerExperience : MonoBehaviour
{
    [SerializeField]
    private int currentLevel = 1;

    [SerializeField]
    private int currentExperience = 0;

    [SerializeField]
    private int experienceToNextLevel = 10;

    [SerializeField]
    private int experienceGrowthPerLevel = 5;

    public int CurrentLevel => currentLevel;
    public int CurrentExperience => currentExperience;
    public int ExperienceToNextLevel => experienceToNextLevel;

    public void AddExperience(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentExperience += amount;

        Debug.Log($"경험치 획득: {currentExperience} / {experienceToNextLevel}");

        while (currentExperience >= experienceToNextLevel)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        currentExperience -= experienceToNextLevel;
        currentLevel++;
        experienceToNextLevel += experienceGrowthPerLevel;

        Debug.Log($"레벨업! 현재 레벨: {currentLevel}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ExperienceOrb orb = other.GetComponent<ExperienceOrb>();

        if (orb != null)
        {
            AddExperience(orb.ExperienceAmount);
            Destroy(other.gameObject);
        }
    }

    [ContextMenu("Test Add Experience")]
    private void TestAddExperience()
    {
        AddExperience(5);
    }
}
