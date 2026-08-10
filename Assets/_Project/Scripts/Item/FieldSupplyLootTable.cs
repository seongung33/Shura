using UnityEngine;

public enum FieldItemType : byte
{
    Health,
    Magnet,
    EnemyFreeze,
    SkillCooldownReset
}

public static class FieldSupplyLootTable
{
    public const float ItemDropChance = 0.3f;
    public const float HealthWeight = 0.5f;
    public const float MagnetWeight = 0.3f;
    public const float EnemyFreezeWeight = 0.1f;
    public const float SkillCooldownResetWeight = 0.1f;

    private const float TotalWeight = HealthWeight +
        MagnetWeight +
        EnemyFreezeWeight +
        SkillCooldownResetWeight;

    public static bool TryRoll(out FieldItemType itemType)
    {
        return TryRoll(Random.value, Random.value, out itemType);
    }

    public static bool TryRoll(
        float dropRoll,
        float itemRoll,
        out FieldItemType itemType
    )
    {
        itemType = FieldItemType.Health;

        if (dropRoll < 0f || dropRoll >= ItemDropChance)
        {
            return false;
        }

        float weightedRoll = Mathf.Clamp01(itemRoll) * TotalWeight;

        if (weightedRoll < HealthWeight)
        {
            itemType = FieldItemType.Health;
        }
        else if (weightedRoll < HealthWeight + MagnetWeight)
        {
            itemType = FieldItemType.Magnet;
        }
        else if (weightedRoll <
            HealthWeight + MagnetWeight + EnemyFreezeWeight)
        {
            itemType = FieldItemType.EnemyFreeze;
        }
        else
        {
            itemType = FieldItemType.SkillCooldownReset;
        }

        return true;
    }
}
