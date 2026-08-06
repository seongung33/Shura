using UnityEngine;

/// <summary>
/// 주몽 기본공격: 유도 없는 단발 화살을 마지막 이동 방향으로 자동 발사한다 (D-013, D-018).
/// 기존 PlayerAutoAttack(유도형)은 수정하지 않고 이 컴포넌트로 교체 장착한다.
/// 기본공격은 무속성(ElementType.None)이다.
/// </summary>
[RequireComponent(typeof(PlayerAimDirection))]
public class DirectionalAutoAttack : MonoBehaviour
{
    [SerializeField]
    private SkillData basicSkill;

    [SerializeField]
    private Transform firePoint;

    [SerializeField]
    private LayerMask enemyLayer;

    [Tooltip("켜면 사거리 안에 적이 있을 때만 발사한다")]
    [SerializeField]
    private bool requireEnemyInRange = true;

    private PlayerAimDirection aim;
    private float nextAttackTime;

    private void Awake()
    {
        aim = GetComponent<PlayerAimDirection>();
    }

    private void Update()
    {
        if (basicSkill == null)
        {
            return;
        }

        if (Time.time < nextAttackTime)
        {
            return;
        }

        if (requireEnemyInRange)
        {
            Transform nearestEnemy = EnemyTargetFinder.FindNearestEnemy(
                transform.position,
                basicSkill.Range,
                enemyLayer
            );

            if (nearestEnemy == null)
            {
                return;
            }
        }

        bool wasFired = Fire();

        if (!wasFired)
        {
            return;
        }

        nextAttackTime = Time.time + basicSkill.Cooldown;
    }

    private bool Fire()
    {
        if (basicSkill.SkillPrefab == null)
        {
            Debug.LogWarning(
                $"{basicSkill.name}에 Skill Prefab이 연결되지 않았습니다."
            );

            return false;
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
            return networkRelay.TryCast(
                basicSkill,
                spawnPosition,
                direction,
                ElementType.None
            );
        }

        GameObject skillObject = Instantiate(
            basicSkill.SkillPrefab,
            spawnPosition,
            Quaternion.identity
        );

        ISkillBehaviour skillBehaviour =
            skillObject.GetComponent<ISkillBehaviour>();

        if (skillBehaviour == null)
        {
            Debug.LogError(
                $"{basicSkill.SkillPrefab.name}에 ISkillBehaviour 컴포넌트가 없습니다. " +
                "StraightProjectile을 붙였는지 확인하세요."
            );

            Destroy(skillObject);
            return false;
        }

        SkillCastContext context = new SkillCastContext
        {
            Owner = gameObject,
            Origin = spawnPosition,
            Direction = direction,
            Damage = basicSkill.Damage,
            ProjectileSpeed = basicSkill.ProjectileSpeed,
            Element = ElementType.None,
            EnemyLayer = enemyLayer,
            VisualOnly = false,
            SourcePlayerId = ulong.MaxValue
        };

        skillBehaviour.Cast(context);

        return true;
    }
}
