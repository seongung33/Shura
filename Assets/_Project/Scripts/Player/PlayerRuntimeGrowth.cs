using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerRuntimeGrowth : MonoBehaviour
{
    private sealed class OwnedSkill
    {
        public int Level;
        public ElementType Element;
    }

    private readonly Dictionary<SkillData, OwnedSkill> ownedSkills = new();
    private readonly Dictionary<SkillData, bool> projectileSkillCache = new();

    private PlayerGrowthNetworkState commonGrowth =
        PlayerGrowthNetworkState.Default;
    private LevelUpSettings settings = new();
    private SkillData basicSkill;

    public bool HasAuthoritativeSkillState { get; private set; }

    public void ConfigureBasicSkill(SkillData configuredBasicSkill)
    {
        basicSkill = configuredBasicSkill;
    }

    public void Configure(LevelUpSettings configuredSettings)
    {
        settings = configuredSettings ?? new LevelUpSettings();
        ApplyMovementMultiplier();
    }

    public void ApplyCommonGrowth(PlayerGrowthNetworkState state)
    {
        commonGrowth = state;
        ApplyMovementMultiplier();
    }

    public void ApplySkillStates(
        IReadOnlyList<SkillProgressNetworkState> states,
        IReadOnlyList<SkillData> skillPool
    )
    {
        ownedSkills.Clear();

        if (states != null && skillPool != null)
        {
            foreach (SkillProgressNetworkState state in states)
            {
                if (state.SkillPoolIndex < 0 ||
                    state.SkillPoolIndex >= skillPool.Count)
                {
                    continue;
                }

                SkillData skill = skillPool[state.SkillPoolIndex];

                if (skill == null || state.Level <= 0)
                {
                    continue;
                }

                ownedSkills[skill] = new OwnedSkill
                {
                    Level = state.Level,
                    Element = state.Element
                };
            }
        }

        HasAuthoritativeSkillState = true;
    }

    public bool TryGetSkillState(
        SkillData skill,
        out int level,
        out ElementType element
    )
    {
        if (skill != null && ownedSkills.TryGetValue(skill, out OwnedSkill state))
        {
            level = state.Level;
            element = state.Element;
            return true;
        }

        level = 0;
        element = ElementType.None;
        return false;
    }

    public SkillCastRuntime GetCastRuntime(SkillData skill)
    {
        SkillCastRuntime runtime = SkillCastRuntime.FromBase(skill);

        if (skill == null)
        {
            return runtime;
        }

        bool isBasicAttack = skill == basicSkill ||
            (basicSkill == null && skill.SkillId == "basic_arrow");
        SkillRuntimeModifiers skillModifiers = SkillRuntimeModifiers.Default;
        int skillLevel = 1;

        if (!isBasicAttack &&
            ownedSkills.TryGetValue(skill, out OwnedSkill ownedSkill))
        {
            skillLevel = ownedSkill.Level;
            skillModifiers = skill.GetRuntimeModifiers(ownedSkill.Level);
        }

        runtime.Damage = Mathf.Max(
            0f,
            skill.Damage * commonGrowth.DamageMultiplier *
            skillModifiers.DamageMultiplier
        );
        runtime.ProjectileSpeed = Mathf.Max(
            0f,
            skill.ProjectileSpeed * commonGrowth.ProjectileSpeedMultiplier *
            skillModifiers.ProjectileSpeedMultiplier
        );
        runtime.Range = Mathf.Max(
            0f,
            skill.Range * skillModifiers.ZoneRadiusMultiplier
        );
        runtime.SkillLevel = skillLevel;

        float cooldownMultiplier = skill.IgnoreCooldownModifiers
            ? 1f
            : isBasicAttack
                ? commonGrowth.AttackIntervalMultiplier
                : commonGrowth.SkillCooldownMultiplier *
                    skillModifiers.CooldownMultiplier;
        float minimumCooldown = isBasicAttack
            ? settings.MinimumAttackInterval
            : settings.MinimumSkillCooldown;
        runtime.Cooldown = Mathf.Max(
            minimumCooldown,
            skill.Cooldown * cooldownMultiplier
        );

        bool isProjectile = IsProjectileSkill(skill);
        runtime.ProjectileCount = isProjectile
            ? Mathf.Max(
                1,
                1 + commonGrowth.ProjectileCountBonus +
                skillModifiers.ProjectileCountBonus
            )
            : 1;
        runtime.ProjectileSpreadAngle = settings.ProjectileSpreadAngle;
        runtime.PierceBonus = skillModifiers.PierceBonus;
        runtime.ExplosionRadiusMultiplier =
            skillModifiers.ExplosionRadiusMultiplier;
        runtime.ActivationIntervalMultiplier =
            skillModifiers.ActivationIntervalMultiplier;
        runtime.ZoneRadiusMultiplier = skillModifiers.ZoneRadiusMultiplier;
        runtime.ZoneDurationMultiplier = skillModifiers.ZoneDurationMultiplier;
        runtime.MovementSpeedMultiplier =
            skillModifiers.MovementSpeedMultiplier;
        runtime.ZoneDamageMultiplier = skillModifiers.ZoneDamageMultiplier;
        runtime.BounceCount = isBasicAttack
            ? Mathf.Max(0, commonGrowth.BasicAttackBounceCount)
            : 0;
        runtime.BounceRange = settings.RicochetRange;

        if (isBasicAttack &&
            TryGetComponent(out CheokJunGyeongCombatState combatState))
        {
            runtime.Cooldown = Mathf.Max(
                settings.MinimumAttackInterval,
                runtime.Cooldown * combatState.BasicAttackIntervalMultiplier
            );
            runtime.Range *= combatState.BasicAttackRangeMultiplier;
        }

        return runtime;
    }

    public static Vector2 GetVolleyDirection(
        Vector2 direction,
        int projectileIndex,
        int projectileCount,
        float spreadAngle
    )
    {
        Vector2 normalized = direction.sqrMagnitude > 0.001f
            ? direction.normalized
            : Vector2.right;

        if (projectileCount <= 1 || spreadAngle <= 0f)
        {
            return normalized;
        }

        float center = (projectileCount - 1) * 0.5f;
        float angle = (projectileIndex - center) * spreadAngle;
        return Quaternion.Euler(0f, 0f, angle) * normalized;
    }

    private bool IsProjectileSkill(SkillData skill)
    {
        if (projectileSkillCache.TryGetValue(skill, out bool isProjectile))
        {
            return isProjectile;
        }

        isProjectile = skill.SkillPrefab != null &&
            skill.SkillPrefab.GetComponent<StraightProjectile>() != null;
        projectileSkillCache[skill] = isProjectile;
        return isProjectile;
    }

    private void ApplyMovementMultiplier()
    {
        float multiplier = Mathf.Max(0.1f, commonGrowth.MoveSpeedMultiplier);

        Shura.Player.PlayerController localMovement =
            GetComponent<Shura.Player.PlayerController>();
        NetworkPlayerMovement networkMovement =
            GetComponent<NetworkPlayerMovement>();

        if (localMovement != null)
        {
            localMovement.GrowthSpeedMultiplier = multiplier;
        }

        if (networkMovement != null)
        {
            networkMovement.GrowthSpeedMultiplier = multiplier;
        }
    }
}
