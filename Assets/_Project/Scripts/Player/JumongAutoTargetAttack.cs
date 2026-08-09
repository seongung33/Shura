using UnityEngine;

/// <summary>
/// Jumong's basic attack. It aims at the nearest enemy only when firing,
/// then delegates movement and damage to the existing straight projectile path.
/// </summary>
public sealed class JumongAutoTargetAttack : MonoBehaviour
{
    private const int JumongCharacterId = 0;

    [SerializeField]
    private SkillData basicSkill;

    [SerializeField]
    private Transform firePoint;

    [SerializeField]
    private LayerMask enemyLayer;

    private PlayerRuntimeGrowth runtimeGrowth;
    private NetworkSkillCastRelay networkRelay;
    private float nextAttackTime;

    public static bool Supports(CharacterData character)
    {
        return character != null &&
            character.CharacterId == JumongCharacterId;
    }

    public void ConfigureBasicSkill(SkillData skill)
    {
        basicSkill = skill;
        runtimeGrowth?.ConfigureBasicSkill(skill);
        nextAttackTime = 0f;
    }

    private void Awake()
    {
        runtimeGrowth = GetComponent<PlayerRuntimeGrowth>();
        networkRelay = GetComponent<NetworkSkillCastRelay>();
        runtimeGrowth?.ConfigureBasicSkill(basicSkill);
    }

    private void Update()
    {
        if (GameplayPauseState.IsLevelUpActive ||
            basicSkill == null ||
            Time.time < nextAttackTime)
        {
            return;
        }

        SkillCastRuntime runtime = runtimeGrowth != null
            ? runtimeGrowth.GetCastRuntime(basicSkill)
            : SkillCastRuntime.FromBase(basicSkill);

        Transform target = EnemyTargetFinder.FindNearestEnemy(
            transform.position,
            runtime.Range,
            enemyLayer
        );

        if (!IsAlive(target))
        {
            return;
        }

        Vector2 spawnPosition = firePoint != null
            ? firePoint.position
            : transform.position;
        Vector2 direction = (Vector2)target.position - spawnPosition;

        if (direction.sqrMagnitude < 0.001f ||
            !Fire(spawnPosition, direction.normalized, runtime))
        {
            return;
        }

        nextAttackTime = Time.time + runtime.Cooldown;
    }

    private bool Fire(
        Vector2 spawnPosition,
        Vector2 direction,
        SkillCastRuntime runtime
    )
    {
        if (basicSkill.SkillPrefab == null)
        {
            Debug.LogWarning(
                $"{basicSkill.name} has no Skill Prefab assigned."
            );
            return false;
        }

        if (networkRelay != null && networkRelay.IsSpawned)
        {
            return networkRelay.TryCast(
                basicSkill,
                spawnPosition,
                direction,
                ElementType.None
            );
        }

        int projectileCount = Mathf.Max(1, runtime.ProjectileCount);

        for (int index = 0; index < projectileCount; index++)
        {
            GameObject skillObject = Instantiate(
                basicSkill.SkillPrefab,
                spawnPosition,
                Quaternion.identity
            );

            ISkillBehaviour behaviour =
                skillObject.GetComponent<ISkillBehaviour>();

            if (behaviour == null)
            {
                Debug.LogError(
                    $"{basicSkill.SkillPrefab.name} has no ISkillBehaviour component."
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
                Origin = spawnPosition,
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
                Element = ElementType.None,
                EnemyLayer = enemyLayer,
                VisualOnly = false,
                SourcePlayerId = ulong.MaxValue
            });
        }

        return true;
    }

    private static bool IsAlive(Transform target)
    {
        if (target == null)
        {
            return false;
        }

        EnemyHealth health = target.GetComponent<EnemyHealth>();
        return health == null || health.CurrentHealth > 0f;
    }
}
