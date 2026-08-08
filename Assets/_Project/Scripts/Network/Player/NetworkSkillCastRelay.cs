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

    public void ConfigureAllowedSkills(
        SkillData basicSkill,
        IReadOnlyList<SkillData> startingSkills
    )
    {
        allowedSkills.Clear();

        AddAllowedSkill(basicSkill);

        if (startingSkills != null)
        {
            foreach (SkillData skill in startingSkills)
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

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void RequestCastRpc(
        int skillIndex,
        Vector2 origin,
        Vector2 direction,
        ElementType element
    )
    {
        if (!IsValidElement(element) ||
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

        nextServerCastTimes[skillIndex] = Time.time + skill.Cooldown;
        SpawnSkill(skill, origin, normalizedDirection, element, false);
        SpawnSkillVisualRpc(skillIndex, origin, normalizedDirection, element);
    }

    [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
    private void SpawnSkillVisualRpc(
        int skillIndex,
        Vector2 origin,
        Vector2 direction,
        ElementType element
    )
    {
        if (TryGetAllowedSkill(skillIndex, out SkillData skill))
        {
            SpawnSkill(skill, origin, direction, element, true);
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
        if (skill.SkillId == "basic_arrow")
        {
            return element == ElementType.None;
        }

        if (element == ElementType.None)
        {
            return false;
        }

        if (serverSkillElements.TryGetValue(skillIndex, out ElementType assigned))
        {
            return assigned == element;
        }

        serverSkillElements[skillIndex] = element;
        return true;
    }

    private void SpawnSkill(
        SkillData skill,
        Vector2 origin,
        Vector2 direction,
        ElementType element,
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
            Damage = skill.Damage,
            ProjectileSpeed = skill.ProjectileSpeed,
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
