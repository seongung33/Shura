using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class NetworkSkillCastRelay : NetworkBehaviour
{
    [SerializeField]
    private List<SkillData> allowedSkills = new List<SkillData>();

    [SerializeField]
    private LayerMask enemyLayer;

    [Min(0.1f)]
    [SerializeField]
    private float maxOriginDistance = 1.5f;

    private readonly Dictionary<int, float> nextServerCastTimes =
        new Dictionary<int, float>();
    private readonly Dictionary<int, ElementType> serverSkillElements =
        new Dictionary<int, ElementType>();
    private readonly List<SkillData> configuredStartingSkills = new();
    private PlayerRuntimeGrowth runtimeGrowth;
    private SkillData configuredBasicSkill;
    private SkillData configuredUltimateSkill;

    private void Awake()
    {
        runtimeGrowth = GetComponent<PlayerRuntimeGrowth>();
    }

    public void ConfigureAllowedSkills(
        SkillData basicSkill,
        IReadOnlyList<SkillData> startingSkills,
        IReadOnlyList<SkillData> levelUpSkills = null,
        SkillData ultimateSkill = null
    )
    {
        allowedSkills.Clear();
        configuredStartingSkills.Clear();
        configuredBasicSkill = basicSkill;
        configuredUltimateSkill = ultimateSkill;

        AddAllowedSkill(basicSkill);
        AddAllowedSkill(ultimateSkill);

        if (startingSkills != null)
        {
            foreach (SkillData skill in startingSkills)
            {
                AddAllowedSkill(skill);

                if (skill != null && !configuredStartingSkills.Contains(skill))
                {
                    configuredStartingSkills.Add(skill);
                }
            }
        }

        if (levelUpSkills != null)
        {
            foreach (SkillData skill in levelUpSkills)
            {
                AddAllowedSkill(skill);
            }
        }

        nextServerCastTimes.Clear();
        serverSkillElements.Clear();
    }

    public bool TryCast(
        SkillData skill,
        Vector2 origin,
        Vector2 direction,
        ElementType element
    )
    {
        if (!IsSpawned || !IsOwner || skill == null)
        {
            return false;
        }

        int skillIndex = allowedSkills.IndexOf(skill);

        if (skillIndex < 0)
        {
            Debug.LogWarning($"네트워크 허용 목록에 없는 스킬입니다: {skill.name}");
            return false;
        }

        RequestCastRpc(skillIndex, origin, direction, element);
        return true;
    }

    public void ResetSkillCooldownsServer()
    {
        if (!IsSpawned || !IsServer)
        {
            return;
        }

        for (int index = 0; index < allowedSkills.Count; index++)
        {
            if (allowedSkills[index] != configuredBasicSkill)
            {
                nextServerCastTimes.Remove(index);
            }
        }

        ResetSkillCooldownsRpc();
    }

    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
    private void ResetSkillCooldownsRpc()
    {
        GetComponent<AutoSkillCaster>()?.ResetSkillCooldowns();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void RequestCastRpc(
        int skillIndex,
        Vector2 origin,
        Vector2 direction,
        ElementType element
    )
    {
        if (GameplayPauseState.IsLevelUpActive ||
            !IsValidElement(element) ||
            !TryGetValidatedCast(
                skillIndex,
                origin,
                direction,
                out SkillData skill,
                out Vector2 normalizedDirection
            ) ||
            !TryValidateSkillElement(skillIndex, skill, element))
        {
            return;
        }

        SkillCastRuntime runtime = runtimeGrowth != null
            ? runtimeGrowth.GetCastRuntime(skill)
            : SkillCastRuntime.FromBase(skill);

        RelicCombat.ApplyNetworkCastModifiers(
            gameObject,
            skill,
            ref runtime
        );

        Vector2 resolvedOrigin = ResolveCastOrigin(
            skill,
            origin,
            normalizedDirection,
            runtime.Range
        );

        if (!IsFinite(resolvedOrigin))
        {
            return;
        }

        nextServerCastTimes[skillIndex] = Time.time + runtime.Cooldown;
        SpawnSkillVolley(
            skill,
            resolvedOrigin,
            normalizedDirection,
            element,
            runtime,
            false
        );
        SpawnSkillVisualRpc(
            skillIndex,
            resolvedOrigin,
            normalizedDirection,
            element,
            runtime
        );
    }

    [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
    private void SpawnSkillVisualRpc(
        int skillIndex,
        Vector2 origin,
        Vector2 direction,
        ElementType element,
        SkillCastRuntime runtime
    )
    {
        if (TryGetAllowedSkill(skillIndex, out SkillData skill))
        {
            SpawnSkillVolley(
                skill,
                origin,
                direction,
                element,
                runtime,
                true
            );
        }
    }

    private bool TryGetValidatedCast(
        int skillIndex,
        Vector2 origin,
        Vector2 direction,
        out SkillData skill,
        out Vector2 normalizedDirection
    )
    {
        normalizedDirection = Vector2.right;

        if (!TryGetAllowedSkill(skillIndex, out skill) ||
            !IsFinite(origin) ||
            !IsFinite(direction) ||
            Vector2.Distance(transform.position, origin) > maxOriginDistance ||
            direction.sqrMagnitude < 0.001f)
        {
            return false;
        }

        float now = Time.time;

        if (nextServerCastTimes.TryGetValue(skillIndex, out float nextCastTime) &&
            now < nextCastTime)
        {
            return false;
        }

        normalizedDirection = direction.normalized;
        return true;
    }

    private bool TryGetAllowedSkill(int index, out SkillData skill)
    {
        skill = null;

        if (index < 0 || index >= allowedSkills.Count)
        {
            return false;
        }

        skill = allowedSkills[index];
        return skill != null && skill.SkillPrefab != null;
    }

    private void AddAllowedSkill(SkillData skill)
    {
        if (skill != null && !allowedSkills.Contains(skill))
        {
            allowedSkills.Add(skill);
        }
    }

    private bool TryValidateSkillElement(
        int skillIndex,
        SkillData skill,
        ElementType element
    )
    {
        if (skill == configuredBasicSkill)
        {
            return element == ElementType.None;
        }

        if (skill == configuredUltimateSkill && skill.ForceNoElement)
        {
            return element == ElementType.None;
        }

        if (element == ElementType.None)
        {
            return false;
        }

        bool isConfiguredInnateSkill =
            configuredStartingSkills.Contains(skill) ||
            skill == configuredUltimateSkill;

        if (!isConfiguredInnateSkill &&
            runtimeGrowth != null &&
            runtimeGrowth.HasAuthoritativeSkillState)
        {
            return runtimeGrowth.TryGetSkillState(
                    skill,
                    out int level,
                    out ElementType assignedElement
                ) &&
                level > 0 &&
                assignedElement == element;
        }

        if (serverSkillElements.TryGetValue(skillIndex, out ElementType assigned))
        {
            return assigned == element;
        }

        serverSkillElements[skillIndex] = element;
        return true;
    }

    private Vector2 ResolveCastOrigin(
        SkillData skill,
        Vector2 requestedOrigin,
        Vector2 direction,
        float searchRange
    )
    {
        ISkillCastOriginResolver resolver =
            skill.SkillPrefab.GetComponent<ISkillCastOriginResolver>();
        return resolver != null
            ? resolver.ResolveCastOrigin(
                gameObject,
                direction,
                searchRange,
                enemyLayer
            )
            : requestedOrigin;
    }

    private void SpawnSkillVolley(
        SkillData skill,
        Vector2 origin,
        Vector2 direction,
        ElementType element,
        SkillCastRuntime runtime,
        bool visualOnly
    )
    {
        int projectileCount = Mathf.Max(1, runtime.ProjectileCount);

        for (int index = 0; index < projectileCount; index++)
        {
            Vector2 volleyDirection = PlayerRuntimeGrowth.GetVolleyDirection(
                direction,
                index,
                projectileCount,
                runtime.ProjectileSpreadAngle
            );
            SpawnSkill(
                skill,
                origin,
                volleyDirection,
                element,
                runtime,
                visualOnly
            );
        }
    }

    private void SpawnSkill(
        SkillData skill,
        Vector2 origin,
        Vector2 direction,
        ElementType element,
        SkillCastRuntime runtime,
        bool visualOnly
    )
    {
        GameObject skillObject = Instantiate(
            skill.SkillPrefab,
            origin,
            Quaternion.identity
        );

        ISkillBehaviour behaviour =
            skillObject.GetComponent<ISkillBehaviour>();

        if (behaviour == null)
        {
            Debug.LogError($"{skill.SkillPrefab.name}에 ISkillBehaviour가 없습니다.");
            Destroy(skillObject);
            return;
        }

        behaviour.Cast(new SkillCastContext
        {
            Owner = gameObject,
            Origin = origin,
            Direction = direction,
            Damage = runtime.Damage,
            Range = runtime.Range,
            ProjectileSpeed = runtime.ProjectileSpeed,
            SkillLevel = runtime.SkillLevel,
            PierceBonus = runtime.PierceBonus,
            ExplosionRadiusMultiplier = runtime.ExplosionRadiusMultiplier,
            ActivationIntervalMultiplier = runtime.ActivationIntervalMultiplier,
            ZoneRadiusMultiplier = runtime.ZoneRadiusMultiplier,
            ZoneDurationMultiplier = runtime.ZoneDurationMultiplier,
            MovementSpeedMultiplier = runtime.MovementSpeedMultiplier,
            ZoneDamageMultiplier = runtime.ZoneDamageMultiplier,
            BounceCount = runtime.BounceCount,
            BounceRange = runtime.BounceRange,
            Element = element,
            EnemyLayer = enemyLayer,
            VisualOnly = visualOnly,
            SourcePlayerId = OwnerClientId
        });
    }

    private static bool IsFinite(Vector2 value)
    {
        return !float.IsNaN(value.x) &&
            !float.IsNaN(value.y) &&
            !float.IsInfinity(value.x) &&
            !float.IsInfinity(value.y);
    }

    private static bool IsValidElement(ElementType element)
    {
        int value = (int)element;
        return value >= (int)ElementType.None && value <= (int)ElementType.Fire;
    }
}
