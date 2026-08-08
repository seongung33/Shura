using System;
using System.Collections.Generic;
using UnityEngine;

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
    private bool hasStarted;

    private void Awake()
    {
        aim = GetComponent<PlayerAimDirection>();
    }

    private void Start()
    {
        hasStarted = true;
        AssignRandomElements();
    }

    public void ConfigureSkills(IReadOnlyList<SkillData> skills)
    {
        equippedSkills.Clear();

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
                    element = ElementType.None
                });
            }
        }

        if (hasStarted)
        {
            AssignRandomElements();
        }
    }

    private void Update()
    {
        foreach (EquippedSkill skill in equippedSkills)
        {
            TryCast(skill);
        }
    }

    /// <summary>
    /// 스킬 습득 시 호출. 랜덤 속성이 부여된다.
    /// </summary>
    public void EquipSkill(SkillData skillData)
    {
        EquippedSkill newSkill = new EquippedSkill
        {
            data = skillData,
            element = ElementUtil.GetRandomElement()
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

    private void TryCast(EquippedSkill skill)
    {
        if (skill.data == null || skill.data.SkillPrefab == null)
        {
            return;
        }

        if (Time.time < skill.nextCastTime)
        {
            return;
        }

        if (requireEnemyInRange)
        {
            Transform nearestEnemy = EnemyTargetFinder.FindNearestEnemy(
                transform.position,
                skill.data.Range,
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

        NetworkSkillCastRelay networkRelay =
            GetComponent<NetworkSkillCastRelay>();

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
                skill.nextCastTime = Time.time + skill.data.Cooldown;
            }

            return;
        }

        GameObject skillObject = Instantiate(
            skill.data.SkillPrefab,
            spawnPosition,
            Quaternion.identity
        );

        ISkillBehaviour skillBehaviour =
            skillObject.GetComponent<ISkillBehaviour>();

        if (skillBehaviour == null)
        {
            Debug.LogError(
                $"{skill.data.SkillPrefab.name}에 ISkillBehaviour 컴포넌트가 없습니다."
            );

            Destroy(skillObject);
            return;
        }

        SkillCastContext context = new SkillCastContext
        {
            Owner = gameObject,
            Origin = spawnPosition,
            Direction = direction,
            Damage = skill.data.Damage,
            ProjectileSpeed = skill.data.ProjectileSpeed,
            Element = skill.element,
            EnemyLayer = enemyLayer,
            VisualOnly = false,
            SourcePlayerId = ulong.MaxValue
        };

        skillBehaviour.Cast(context);

        skill.nextCastTime = Time.time + skill.data.Cooldown;
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
