using UnityEngine;

[CreateAssetMenu(
    fileName = "LevelCurveData",
    menuName = "Shura/Balance/Level Curve"
)]
public sealed class LevelCurveData : ScriptableObject
{
    [SerializeField, Min(0f)]
    private float baseConstant = 6f;

    [SerializeField, Min(0f)]
    private float linearCoefficient = 2f;

    [SerializeField, Min(0f)]
    private float quadraticCoefficient = 0.32f;

    [SerializeField, Min(1f)]
    private float teamExperienceMultiplier = 1.75f;

    [SerializeField]
    private int[] skillChoiceLevels =
    {
        3, 6, 9, 12, 15, 18, 21, 24, 27, 30
    };

    public int GetBaseNextExperience(int currentLevel)
    {
        int level = Mathf.Max(1, currentLevel);
        float value = baseConstant +
            linearCoefficient * level +
            quadraticCoefficient * level * level;
        return Mathf.Max(1, RoundPositive(value));
    }

    public int GetTeamNextExperience(int currentLevel)
    {
        return Mathf.Max(
            1,
            RoundPositive(
                GetBaseNextExperience(currentLevel) *
                teamExperienceMultiplier
            )
        );
    }

    public bool IsSkillChoiceLevel(int level)
    {
        if (skillChoiceLevels == null)
        {
            return false;
        }

        foreach (int choiceLevel in skillChoiceLevels)
        {
            if (choiceLevel == level)
            {
                return true;
            }
        }

        return false;
    }

    private static int RoundPositive(float value)
    {
        return Mathf.FloorToInt(Mathf.Max(0f, value) + 0.5f);
    }
}
