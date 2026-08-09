using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 보유 스킬을 쿨다운마다 자동 시전한다 (뱀서류 방식).
/// 각 스킬은 장착 시 랜덤 속성을 부여받는다 (D-010).
/// 스킬 습득 UI(특정 레벨 3택1, D-019)가 생기기 전까지는
/// Inspector에서 스킬을 직접 넣어 테스트한다.
/// </summary>
public class AutoSkillCaster : MonoBehaviour
{
    [Serializable]
    public class EquippedSkill
    {
        public SkillData data;

        [Tooltip("None이면 시작할 때 랜덤 속성이 부여된다")]
        public ElementType element = ElementType.None;

        [NonSerialized]
        public float nextCastTime;

        [NonSerialized]
        public int level = 1;
    }

    [SerializeField]
    private List<EquippedSkill> equippedSkills = new List<EquippedSkill>();

    [SerializeField]
    private Transform firePoint;

    [SerializeField]
    private LayerMask enemyLayer;

    [Tooltip("켜면 스킬 사거리(Range) 안에 적이 있을 때만 시전한다")]
    [SerializeField]
    private bool requireEnemyInRange = true;

    private PlayerAimDirection aim;
    private PlayerRuntimeGrowth runtimeGrowth;
    private NetworkSkillCastRelay networkRelay;
    private bool hasStarted;
    private readonly HashSet<SkillData> persistentSkills = new();
    private EquippedSkill ultimateSkill;

    private void Awake()
    {
        aim = GetComponent<PlayerAimDirection>();
        runtimeGrowth = GetComponent<PlayerRuntimeGrowth>();
        networkRelay = GetComponent<NetworkSkillCastRelay>();
    }

    private void Start()
    {
        hasStarted = true;
        AssignRandomElements();
    }

    public void ConfigureSkills(IReadOnlyList<SkillData> skills)
    {
        equippedSkills.Clear();
        persistentSkills.Clear();

        if (skills != null)
        {
            foreach (SkillData skill in skills)
            {
                if (skill == null)
                {
                    continue;
                }

                equippedSkills.Add(new EquippedSkill
                {
                    data = skill,
                    element = ElementType.None,
                    level = 1
                });
                persistentSkills.Add(skill);
            }
        }

        if (hasStarted)
        {
            AssignRandomElements();
        }
    }

    public void ConfigureUltimateSkill(SkillData skill)
    {
        ultimateSkill = skill == null
            ? null
            : new EquippedSkill
            {
                data = skill,
                element = skill.ForceNoElement
                    ? ElementType.None
                    : ElementUtil.GetRandomElement(),
                level = 1
            };
    }

    public void ResetSkillCooldowns()
    {
        foreach (EquippedSkill skill in equippedSkills)
        {
            skill.nextCastTime = 0f;
        }

        if (ultimateSkill != null)
        {
            ultimateSkill.nextCastTime = 0f;
        }
    }

    public void ConfigureProgressionSkills(
        IReadOnlyList<RuntimeSkillLoadout> skills
    )
    {
        runtimeGrowth ??= GetComponent<PlayerRuntimeGrowth>();
        Dictionary<SkillData, EquippedSkill> existing = new();

        foreach (EquippedSkill equipped in equippedSkills)
        {
            if (equipped.data != null)
            {
                existing[equipped.data] = equipped;
            }
        }

        List<EquippedSkill> synchronized = new();

        foreach (EquippedSkill equipped in equippedSkills)
        {
            if (equipped.data != null && persistentSkills.Contains(equipped.data))
            {
                synchronized.Add(equipped);
            }
        }

        if (skills != null)
        {
            foreach (RuntimeSkillLoadout loadout in skills)
            {
                if (loadout.Data == null || loadout.Level <= 0)
                {
                    continue;
                }

                if (!existing.TryGetValue(
                        loadout.Data,
                        out EquippedSkill equipped
                    ))
                {
                    equipped = new EquippedSkill
                    {
                        data = loadout.Data
                    };
                }

                equipped.element = loadout.Element;
                equipped.level = loadout.Level;

                if (!persistentSkills.Contains(equipped.data))
                {
                    synchronized.Add(equipped);
                }
            }
        }

        equippedSkills = synchronized;
    }

    private void Update()
    {
        if (GameplayPauseState.IsLevelUpActive)
        {
            return;
        }

        bool manualCastRequested =
            CanReadManualInput() &&
            Keyboard.current != null &&
            Keyboard.current.rKey.wasPressedThisFrame;

        if (manualCastRequested && ultimateSkill != null)
        {
            float previousCastTime = ultimateSkill.nextCastTime;
            TryCast(ultimateSkill, false);

            if (ultimateSkill.nextCastTime > previousCastTime)
            {
                UltimateCutInPresenter.Show(gameObject);
            }
        }

        foreach (EquippedSkill skill in equippedSkills)
        {
            TryCast(skill, requireEnemyInRange);
        }
    }

    private bool CanReadManualInput()
    {
        return networkRelay == null ||
            !networkRelay.IsSpawned ||
            networkRelay.IsOwner;
    }

    /// <summary>
    /// 스킬 습득 시 호출. 랜덤 속성이 부여된다.
    /// </summary>
    public void EquipSkill(SkillData skillData)
    {
        if (skillData == null)
        {
            return;
        }

        EquippedSkill newSkill = new EquippedSkill
        {
            data = skillData,
            element = ElementUtil.GetRandomElement(),
            level = 1
        };

        equippedSkills.Add(newSkill);

        Debug.Log(
            $"스킬 습득: {skillData.DisplayName} " +
            $"[{ElementUtil.GetKoreanName(newSkill.element)}]"
        );
    }

    private void AssignRandomElements()
    {
        foreach (EquippedSkill skill in equippedSkills)
        {
            if (skill.data == null)
            {
                continue;
            }

            if (skill.element == ElementType.None)
            {
                skill.element = ElementUtil.GetRandomElement();
            }

            Debug.Log(
                $"보유 스킬: {skill.data.DisplayName} " +
                $"[{ElementUtil.GetKoreanName(skill.element)}]"
            );
        }
    }

    private void TryCast(EquippedSkill skill, bool needsEnemyInRange)
    {
        if (skill.data == null || skill.data.SkillPrefab == null)
        {
            return;
        }

        if (Time.time < skill.nextCastTime)
        {
            return;
        }

        SkillCastRuntime runtime = runtimeGrowth != null
            ? runtimeGrowth.GetCastRuntime(skill.data)
            : SkillCastRuntime.FromBase(skill.data);

        if (needsEnemyInRange)
        {
            Transform nearestEnemy = EnemyTargetFinder.FindNearestEnemy(
                transform.position,
                runtime.Range,
                enemyLayer
            );

            if (nearestEnemy == null)
            {
                return;
            }
        }

        Vector3 spawnPosition =
            firePoint != null
                ? firePoint.position
                : transform.position;

        Vector2 direction =
            aim != null ? aim.AimDirection : Vector2.right;

        if (networkRelay != null && networkRelay.IsSpawned)
        {
            bool requestSent = networkRelay.TryCast(
                skill.data,
                spawnPosition,
                direction,
                skill.element
            );

            if (requestSent)
            {
                skill.nextCastTime = Time.time + runtime.Cooldown;
            }

            return;
        }

        Vector2 resolvedOrigin = ResolveLocalCastOrigin(
            skill.data,
            spawnPosition,
            direction,
            runtime.Range
        );

        if (!CastLocalVolley(
                skill.data,
                resolvedOrigin,
                direction,
                skill.element,
                runtime
            ))
        {
            return;
        }

        skill.nextCastTime = Time.time + runtime.Cooldown;
    }

    private Vector2 ResolveLocalCastOrigin(
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

    private bool CastLocalVolley(
        SkillData skill,
        Vector2 origin,
        Vector2 direction,
        ElementType element,
        SkillCastRuntime runtime
    )
    {
        int projectileCount = Mathf.Max(1, runtime.ProjectileCount);

        for (int index = 0; index < projectileCount; index++)
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
                Debug.LogError(
                    $"{skill.SkillPrefab.name}에 ISkillBehaviour 컴포넌트가 없습니다."
                );
                Destroy(skillObject);
                return false;
            }

            Vector2 volleyDirection = PlayerRuntimeGrowth.GetVolleyDirection(
                direction,
                index,
                projectileCount,
                runtime.ProjectileSpreadAngle
            );

            behaviour.Cast(new SkillCastContext
            {
                Owner = gameObject,
                Origin = origin,
                Direction = volleyDirection,
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
                VisualOnly = false,
                SourcePlayerId = ulong.MaxValue
            });
        }

        return true;
    }

    [ContextMenu("모든 스킬 속성 랜덤 재부여")]
    private void RerollAllElements()
    {
        foreach (EquippedSkill skill in equippedSkills)
        {
            skill.element = ElementUtil.GetRandomElement();

            if (skill.data != null)
            {
                Debug.Log(
                    $"속성 재부여: {skill.data.DisplayName} " +
                    $"[{ElementUtil.GetKoreanName(skill.element)}]"
                );
            }
        }
    }
}
