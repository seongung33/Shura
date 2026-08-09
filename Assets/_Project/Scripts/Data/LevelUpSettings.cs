using System;
using System.Collections.Generic;
using UnityEngine;

public enum GeneralUpgradeType
{
    Damage,
    AttackInterval,
    SkillCooldown,
    MoveSpeed,
    ProjectileSpeed,
    ProjectileCount,
    BasicAttackRicochet
}

[Serializable]
public sealed class GeneralUpgradeDefinition
{
    [SerializeField]
    private GeneralUpgradeType type;

    [SerializeField]
    private string displayName;

    [TextArea]
    [SerializeField]
    private string description;

    [SerializeField]
    private float amount;

    public GeneralUpgradeDefinition(
        GeneralUpgradeType configuredType,
        string configuredDisplayName,
        string configuredDescription,
        float configuredAmount
    )
    {
        type = configuredType;
        displayName = configuredDisplayName;
        description = configuredDescription;
        amount = configuredAmount;
    }

    public GeneralUpgradeType Type => type;
    public string DisplayName => displayName;
    public string Description => description;
    public float Amount => amount;
}

[Serializable]
public sealed class LevelUpSettings
{
    [Header("Session")]
    [Min(1f)]
    [SerializeField]
    private float choiceDuration = 15f;

    [SerializeField]
    private int[] skillChoiceLevels = { 3, 6, 9 };

    [SerializeField]
    private ElementType[] allowedLevelUpElements =
    {
        ElementType.Water,
        ElementType.Lightning,
        ElementType.Ice,
        ElementType.Earth
    };

    [Header("Combat Limits")]
    [Min(0.05f)]
    [SerializeField]
    private float minimumAttackInterval = 0.15f;

    [Min(0.05f)]
    [SerializeField]
    private float minimumSkillCooldown = 0.25f;

    [Range(0f, 45f)]
    [SerializeField]
    private float projectileSpreadAngle = 10f;

    [Min(0.1f)]
    [SerializeField]
    private float ricochetRange = 5f;

    [Header("General Upgrade Pool")]
    [SerializeField]
    private List<GeneralUpgradeDefinition> generalUpgrades = new()
    {
        new GeneralUpgradeDefinition(
            GeneralUpgradeType.Damage,
            "피해 증가",
            "모든 피해 +15%",
            0.15f
        ),
        new GeneralUpgradeDefinition(
            GeneralUpgradeType.AttackInterval,
            "공격 속도 증가",
            "기본 공격 주기 -10%",
            0.10f
        ),
        new GeneralUpgradeDefinition(
            GeneralUpgradeType.SkillCooldown,
            "스킬 쿨다운 감소",
            "스킬 쿨다운 -10%",
            0.10f
        ),
        new GeneralUpgradeDefinition(
            GeneralUpgradeType.MoveSpeed,
            "이동속도 증가",
            "이동속도 +10%",
            0.10f
        ),
        new GeneralUpgradeDefinition(
            GeneralUpgradeType.ProjectileSpeed,
            "투사체 속도 증가",
            "투사체 속도 +15%",
            0.15f
        ),
        new GeneralUpgradeDefinition(
            GeneralUpgradeType.ProjectileCount,
            "투사체 증가",
            "Projectile +1",
            1f
        ),
        new GeneralUpgradeDefinition(
            GeneralUpgradeType.BasicAttackRicochet,
            "팅기기",
            "기본 화살 Bounce +1",
            1f
        )
    };

    public float ChoiceDuration => Mathf.Max(1f, choiceDuration);
    public float MinimumAttackInterval => Mathf.Max(0.05f, minimumAttackInterval);
    public float MinimumSkillCooldown => Mathf.Max(0.05f, minimumSkillCooldown);
    public float ProjectileSpreadAngle => Mathf.Clamp(projectileSpreadAngle, 0f, 45f);
    public float RicochetRange => Mathf.Max(0.1f, ricochetRange);
    public IReadOnlyList<GeneralUpgradeDefinition> GeneralUpgrades => generalUpgrades;

    public bool IsSkillChoiceLevel(int level)
    {
        if (skillChoiceLevels == null)
        {
            return false;
        }

        foreach (int candidate in skillChoiceLevels)
        {
            if (candidate == level)
            {
                return true;
            }
        }

        return false;
    }

    public bool IsAllowedElement(ElementType element)
    {
        if (allowedLevelUpElements == null)
        {
            return false;
        }

        foreach (ElementType candidate in allowedLevelUpElements)
        {
            if (candidate == element)
            {
                return true;
            }
        }

        return false;
    }

    public ElementType GetRandomAllowedElement()
    {
        if (allowedLevelUpElements == null || allowedLevelUpElements.Length == 0)
        {
            return ElementType.Water;
        }

        int index = UnityEngine.Random.Range(0, allowedLevelUpElements.Length);
        ElementType element = allowedLevelUpElements[index];
        return element == ElementType.None ? ElementType.Water : element;
    }

    public bool TryGetGeneralUpgrade(
        GeneralUpgradeType type,
        out GeneralUpgradeDefinition definition
    )
    {
        if (generalUpgrades != null)
        {
            foreach (GeneralUpgradeDefinition candidate in generalUpgrades)
            {
                if (candidate != null && candidate.Type == type)
                {
                    definition = candidate;
                    return true;
                }
            }
        }

        definition = null;
        return false;
    }
}
