using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NewSkillData",
    menuName = "Shura/Skill Data"
)]
public class SkillData : ScriptableObject
{
    [Header("Information")]

    [SerializeField]
    private string skillId;

    [SerializeField]
    private string displayName;

    [TextArea]
    [SerializeField]
    private string description;

    [Header("Combat")]

    [Min(0.01f)]
    [SerializeField]
    private float cooldown = 1f;

    [Min(0f)]
    [SerializeField]
    private float damage = 10f;

    [Min(0f)]
    [SerializeField]
    private float range = 5f;

    [Min(0f)]
    [SerializeField]
    private float projectileSpeed = 8f;

    [Header("Prefab")]

    [SerializeField]
    private GameObject skillPrefab;

    [Header("Activation")]

    [SerializeField]
    private bool forceNoElement;

    [SerializeField]
    private bool ignoreCooldownModifiers;

    [Header("Progression")]

    [Min(1)]
    [SerializeField]
    private int maxLevel = 5;

    [SerializeField]
    private List<SkillLevelUpgradeDefinition> levelUpgrades = new();

    public string SkillId => skillId;
    public string DisplayName => displayName;
    public string Description => description;

    public float Cooldown => cooldown;
    public float Damage => damage;
    public float Range => range;
    public float ProjectileSpeed => projectileSpeed;

    public GameObject SkillPrefab => skillPrefab;
    public bool ForceNoElement => forceNoElement;
    public bool IgnoreCooldownModifiers => ignoreCooldownModifiers;

    public int MaxLevel => Mathf.Max(1, maxLevel);

    public SkillRuntimeModifiers GetRuntimeModifiers(int level)
    {
        SkillRuntimeModifiers modifiers = SkillRuntimeModifiers.Default;

        if (levelUpgrades == null)
        {
            return modifiers;
        }

        int clampedLevel = Mathf.Clamp(level, 1, MaxLevel);

        foreach (SkillLevelUpgradeDefinition upgrade in levelUpgrades)
        {
            if (upgrade == null || upgrade.Level > clampedLevel)
            {
                continue;
            }

            foreach (SkillUpgradeEffectDefinition effect in upgrade.Effects)
            {
                modifiers.Apply(effect);
            }
        }

        return modifiers;
    }

    public string GetUpgradeSummary(int targetLevel)
    {
        SkillLevelUpgradeDefinition upgrade = GetLevelUpgrade(targetLevel);

        if (upgrade == null)
        {
            return targetLevel <= 1 ? "신규 스킬 획득" : "스킬 성능 강화";
        }

        if (!string.IsNullOrWhiteSpace(upgrade.Summary))
        {
            return upgrade.Summary;
        }

        List<string> descriptions = new();

        foreach (SkillUpgradeEffectDefinition effect in upgrade.Effects)
        {
            string description = DescribeEffect(effect);

            if (!string.IsNullOrEmpty(description))
            {
                descriptions.Add(description);
            }
        }

        return descriptions.Count > 0
            ? string.Join(", ", descriptions)
            : "스킬 성능 강화";
    }

    private SkillLevelUpgradeDefinition GetLevelUpgrade(int targetLevel)
    {
        if (levelUpgrades == null)
        {
            return null;
        }

        foreach (SkillLevelUpgradeDefinition upgrade in levelUpgrades)
        {
            if (upgrade != null && upgrade.Level == targetLevel)
            {
                return upgrade;
            }
        }

        return null;
    }

    private static string DescribeEffect(SkillUpgradeEffectDefinition effect)
    {
        if (effect == null)
        {
            return string.Empty;
        }

        int percent = Mathf.RoundToInt(effect.Amount * 100f);
        int count = Mathf.Max(0, Mathf.RoundToInt(effect.Amount));

        switch (effect.Type)
        {
            case SkillUpgradeType.DamagePercent:
                return $"피해 +{percent}%";
            case SkillUpgradeType.ActivationIntervalReduction:
                return $"발동 주기 -{percent}%";
            case SkillUpgradeType.CooldownReduction:
                return $"쿨다운 -{percent}%";
            case SkillUpgradeType.ProjectileCount:
                return $"투사체 +{count}";
            case SkillUpgradeType.ProjectileSpeedPercent:
                return $"투사체 속도 +{percent}%";
            case SkillUpgradeType.PierceCount:
                return $"관통 +{count}";
            case SkillUpgradeType.ExplosionRadiusPercent:
                return $"폭발 범위 +{percent}%";
            case SkillUpgradeType.ZoneRadiusPercent:
                return $"장판 범위 +{percent}%";
            case SkillUpgradeType.ZoneDurationPercent:
                return $"장판 지속시간 +{percent}%";
            case SkillUpgradeType.MovementSpeedPercent:
                return $"이동속도 보너스 +{percent}%";
            case SkillUpgradeType.ZoneDamagePercent:
                return $"장판 피해 +{percent}%";
            default:
                return string.Empty;
        }
    }
}
