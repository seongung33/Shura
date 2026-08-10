using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유도 없이 직선으로 날아가는 투사체.
/// - 주몽 기본공격: 관통 0, 폭발 없음
/// - 원소 폭발 화살: 폭발 반경 설정
/// - 편전: 관통 수 설정 + 빠른 속도
/// 관통 수·폭발 반경 같은 "스킬 고유 특성"은 프리팹 Inspector에서 설정하고,
/// 피해량·속도·속성은 SkillData/캐스터가 Cast로 전달한다.
/// 기존 유도형 Projectile.cs는 수정하지 않는다 (방법 2 결정).
/// </summary>
public class StraightProjectile : MonoBehaviour, ISkillBehaviour
{
    [Header("Lifetime")]

    [Min(0.1f)]
    [SerializeField]
    private float lifeTime = 3f;

    [Header("Pierce (편전)")]

    [Tooltip("관통 가능한 적 수. 0이면 첫 명중에서 소멸")]
    [Min(0)]
    [SerializeField]
    private int pierceCount = 0;

    [Header("Explosion (원소 폭발 화살)")]

    [Tooltip("0이면 폭발 없음")]
    [Min(0f)]
    [SerializeField]
    private float explosionRadius = 0f;

    [Tooltip("폭발 피해 = 기본 피해 x 배율")]
    [Min(0f)]
    [SerializeField]
    private float explosionDamageMultiplier = 0.7f;

    [Header("Effects")]

    [SerializeField]
    private AutoDestroyEffect hitEffectPrefab;

    [SerializeField]
    private AutoDestroyEffect explosionEffectPrefab;

    private Vector2 direction;
    private float moveSpeed;
    private float damage;
    private ElementType element;
    private LayerMask enemyLayer;
    private bool visualOnly;
    private ulong sourcePlayerId;

    private bool isInitialized;
    private int remainingPierce;
    private int remainingBounces;
    private float bounceRange;
    private float effectiveExplosionRadius;
    private float remainingLifeTime;

    // 같은 적을 두 번 때리는 것 방지 (관통 시 콜라이더 중복 진입 대응)
    private readonly HashSet<int> hitEnemyIds = new HashSet<int>();

    private void Awake()
    {
        remainingLifeTime = lifeTime;
    }

    public void Cast(SkillCastContext context)
    {
        transform.position = context.Origin;

        direction = context.Direction.sqrMagnitude > 0.001f
            ? context.Direction.normalized
            : Vector2.right;

        moveSpeed = context.ProjectileSpeed;
        damage = context.Damage;
        element = context.Element;
        enemyLayer = context.EnemyLayer;
        visualOnly = context.VisualOnly;
        sourcePlayerId = context.SourcePlayerId;

        RelicCombat.ApplyOfflineProjectileModifiers(
            context.Owner,
            ref context
        );

        remainingPierce = pierceCount + Mathf.Max(0, context.PierceBonus);
        remainingBounces = Mathf.Max(0, context.BounceCount);
        bounceRange = Mathf.Max(0f, context.BounceRange);
        float explosionMultiplier = context.ExplosionRadiusMultiplier > 0f
            ? context.ExplosionRadiusMultiplier
            : 1f;
        effectiveExplosionRadius = explosionRadius * explosionMultiplier;

        float sizeMultiplier = context.ZoneRadiusMultiplier > 0f
            ? context.ZoneRadiusMultiplier
            : 1f;
        transform.localScale *= sizeMultiplier;

        // 화살이 날아가는 방향을 바라보게 회전
        transform.right = direction;

        ElementVisuals.ApplyColor(gameObject, element);

        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        if (GameplayPauseState.IsLevelUpActive)
        {
            return;
        }

        remainingLifeTime -= Time.deltaTime;

        if (remainingLifeTime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        transform.position +=
            (Vector3)(direction * moveSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isInitialized)
        {
            return;
        }

        Transform hitRoot =
            other.attachedRigidbody != null
                ? other.attachedRigidbody.transform
                : other.transform.root;

        bool isEnemyLayer =
            (enemyLayer.value & (1 << hitRoot.gameObject.layer)) != 0;

        if (!isEnemyLayer)
        {
            return;
        }

        IDamageable damageable =
            other.GetComponentInParent<IDamageable>();

        if (damageable == null)
        {
            return;
        }

        int enemyId = hitRoot.gameObject.GetInstanceID();

        if (hitEnemyIds.Contains(enemyId))
        {
            return;
        }

        hitEnemyIds.Add(enemyId);

        if (!visualOnly)
        {
            RecordElement(other, element);
            RelicCombat.ApplyDamage(
                damageable,
                other,
                damage,
                RelicTriggerContext.PlayerDirect(
                    sourcePlayerId,
                    hitRoot.position
                )
            );
        }

        SpawnEffect(hitEffectPrefab, hitRoot.position);

        // TODO(연계): 명중한 적에게 (element, 플레이어 ID, 시간) 기록 → SynergyResolver 연동

        if (effectiveExplosionRadius > 0f)
        {
            Explode(hitRoot.position, enemyId);
        }

        if (TryRicochet(hitRoot.position))
        {
            return;
        }

        if (remainingPierce > 0)
        {
            remainingPierce--;
            return;
        }

        Destroy(gameObject);
    }

    private void Explode(Vector2 center, int directHitEnemyId)
    {
        Collider2D[] enemiesInRange =
            Physics2D.OverlapCircleAll(
                center,
                effectiveExplosionRadius,
                enemyLayer
            );

        float explosionDamage = damage * explosionDamageMultiplier;

        // 같은 적을 폭발에서 두 번 때리지 않도록 정리
        HashSet<int> explodedIds = new HashSet<int>();

        foreach (Collider2D enemyCollider in enemiesInRange)
        {
            IDamageable enemyDamageable =
                enemyCollider.GetComponentInParent<IDamageable>();

            if (enemyDamageable == null)
            {
                continue;
            }

            Transform enemyRoot =
                enemyCollider.attachedRigidbody != null
                    ? enemyCollider.attachedRigidbody.transform
                    : enemyCollider.transform.root;

            int enemyId = enemyRoot.gameObject.GetInstanceID();

            // 직격 대상은 이미 피해를 받았으므로 제외
            if (enemyId == directHitEnemyId)
            {
                continue;
            }

            if (!explodedIds.Add(enemyId))
            {
                continue;
            }

            if (!visualOnly)
            {
                RecordElement(enemyCollider, element);
                RelicCombat.ApplyDamage(
                    enemyDamageable,
                    enemyCollider,
                    explosionDamage,
                    RelicTriggerContext.PlayerDirect(
                        sourcePlayerId,
                        enemyRoot.position
                    )
                );
            }
        }

        SpawnEffect(explosionEffectPrefab, center);
    }

    private bool TryRicochet(Vector2 center)
    {
        if (remainingBounces <= 0 || bounceRange <= 0f)
        {
            return false;
        }

        Collider2D[] nearby = Physics2D.OverlapCircleAll(
            center,
            bounceRange,
            enemyLayer
        );
        Transform nearest = null;
        float nearestDistance = float.MaxValue;
        HashSet<int> checkedIds = new HashSet<int>();

        foreach (Collider2D candidate in nearby)
        {
            IDamageable damageable =
                candidate.GetComponentInParent<IDamageable>();

            if (damageable == null)
            {
                continue;
            }

            Transform candidateRoot =
                candidate.attachedRigidbody != null
                    ? candidate.attachedRigidbody.transform
                    : candidate.transform.root;
            int candidateId = candidateRoot.gameObject.GetInstanceID();

            if (!checkedIds.Add(candidateId) || hitEnemyIds.Contains(candidateId))
            {
                continue;
            }

            float distance = Vector2.SqrMagnitude(
                (Vector2)candidateRoot.position - center
            );

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = candidateRoot;
            }
        }

        if (nearest == null)
        {
            return false;
        }

        Vector2 nextDirection = (Vector2)nearest.position - center;

        if (nextDirection.sqrMagnitude < 0.001f)
        {
            return false;
        }

        remainingBounces--;
        direction = nextDirection.normalized;
        transform.position = center + direction * 0.05f;
        transform.right = direction;
        return true;
    }

    private void SpawnEffect(AutoDestroyEffect effectPrefab, Vector2 position)
    {
        if (effectPrefab == null)
        {
            return;
        }

        AutoDestroyEffect effect = Instantiate(
            effectPrefab,
            position,
            Quaternion.identity
        );

        effect.SetElement(element);
    }

    private void RecordElement(Component target, ElementType appliedElement)
    {
        IElementReceiver receiver = target.GetComponentInParent<IElementReceiver>();
        receiver?.RecordElement(appliedElement, sourcePlayerId);
    }

    private void OnDrawGizmosSelected()
    {
        float radius = Application.isPlaying
            ? effectiveExplosionRadius
            : explosionRadius;

        if (radius > 0f)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
