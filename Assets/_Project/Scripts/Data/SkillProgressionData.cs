using System;
using System.Collections.Generic;
using UnityEngine;

public enum SkillUpgradeType
{
    DamagePercent,
    ActivationIntervalReduction,
    CooldownReduction,
    ProjectileCount,
    ProjectileSpeedPercent,
    PierceCount,
    ExplosionRadiusPercent,
    ZoneRadiusPercent,
    ZoneDurationPercent,
    MovementSpeedPercent,
    ZoneDamagePercent
}

[Serializable]
public sealed class SkillUpgradeEffectDefinition
{
    [SerializeField]
    private SkillUpgradeType type;

    [SerializeField]
    private float amount;

    public SkillUpgradeType Type => type;
    public float Amount => amount;
}

[Serializable]
public sealed class SkillLevelUpgradeDefinition
{
    [Min(2)]
    [SerializeField]
    private int level = 2;

    [SerializeField]
    private string summary;

    [SerializeField]
    private List<SkillUpgradeEffectDefinition> effects = new();

    public int Level => Mathf.Max(2, level);
    public string Summary => summary;
    public IReadOnlyList<SkillUpgradeEffectDefinition> Effects => effects;
}

public struct SkillRuntimeModifiers
{
    public float DamageMultiplier;
    public float ActivationIntervalMultiplier;
    public float CooldownMultiplier;
    public int ProjectileCountBonus;
    public float ProjectileSpeedMultiplier;
    public int PierceBonus;
    public float ExplosionRadiusMultiplier;
    public float ZoneRadiusMultiplier;
    public float ZoneDurationMultiplier;
    public float MovementSpeedMultiplier;
    public float ZoneDamageMultiplier;

    public static SkillRuntimeModifiers Default
    {
        get
        {
            return new SkillRuntimeModifiers
            {
                DamageMultiplier = 1f,
                ActivationIntervalMultiplier = 1f,
                CooldownMultiplier = 1f,
                ProjectileSpeedMultiplier = 1f,
                ExplosionRadiusMultiplier = 1f,
                ZoneRadiusMultiplier = 1f,
                ZoneDurationMultiplier = 1f,
                MovementSpeedMultiplier = 1f,
                ZoneDamageMultiplier = 1f
            };
        }
    }

    public void Apply(SkillUpgradeEffectDefinition effect)
    {
        if (effect == null)
        {
            return;
        }

        float amount = effect.Amount;

        switch (effect.Type)
        {
            case SkillUpgradeType.DamagePercent:
                DamageMultiplier += Mathf.Max(0f, amount);
                break;
            case SkillUpgradeType.ActivationIntervalReduction:
                ActivationIntervalMultiplier *=
                    Mathf.Clamp(1f - amount, 0.1f, 1f);
                break;
            case SkillUpgradeType.CooldownReduction:
                CooldownMultiplier *= Mathf.Clamp(1f - amount, 0.1f, 1f);
                break;
            case SkillUpgradeType.ProjectileCount:
                ProjectileCountBonus += Mathf.Max(0, Mathf.RoundToInt(amount));
                break;
            case SkillUpgradeType.ProjectileSpeedPercent:
                ProjectileSpeedMultiplier += Mathf.Max(0f, amount);
                break;
            case SkillUpgradeType.PierceCount:
                PierceBonus += Mathf.Max(0, Mathf.RoundToInt(amount));
                break;
            case SkillUpgradeType.ExplosionRadiusPercent:
                ExplosionRadiusMultiplier += Mathf.Max(0f, amount);
                break;
            case SkillUpgradeType.ZoneRadiusPercent:
                ZoneRadiusMultiplier += Mathf.Max(0f, amount);
                break;
            case SkillUpgradeType.ZoneDurationPercent:
                ZoneDurationMultiplier += Mathf.Max(0f, amount);
                break;
            case SkillUpgradeType.MovementSpeedPercent:
                MovementSpeedMultiplier += Mathf.Max(0f, amount);
                break;
            case SkillUpgradeType.ZoneDamagePercent:
                ZoneDamageMultiplier += Mathf.Max(0f, amount);
                break;
        }
    }
}

public readonly struct RuntimeSkillLoadout
{
    public RuntimeSkillLoadout(
        SkillData data,
        ElementType element,
        int level
    )
    {
        Data = data;
        Element = element;
        Level = level;
    }

    public SkillData Data { get; }
    public ElementType Element { get; }
    public int Level { get; }
}
