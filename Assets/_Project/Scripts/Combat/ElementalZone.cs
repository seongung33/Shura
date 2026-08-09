using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 원소 장판 (공용 시스템). 적토마·궁사의 패러독스·화살비틀기가 공유한다.
/// 일정 시간 동안 남아 있으면서 범위 안의 적에게 주기적으로 피해를 준다.
/// 프리팹: 반투명 원 스프라이트 + (선택) 파티클. 색은 속성에 따라 자동 적용.
/// </summary>
public class ElementalZone : MonoBehaviour
{
    [Header("Zone")]

    [Min(0.1f)]
    [SerializeField]
    private float duration = 3f;

    [Min(0.05f)]
    [SerializeField]
    private float radius = 1f;

    [Header("Damage")]

    [Min(0.05f)]
    [SerializeField]
    private float tickInterval = 0.5f;

    private float damagePerTick;
    private ElementType element;
    private LayerMask enemyLayer;
    private bool visualOnly;
    private ulong sourcePlayerId;

    private bool isInitialized;
    private float nextTickTime;
    private float activeRadius;
    private float remainingDuration;

    public void Initialize(
        ElementType newElement,
        float newDamagePerTick,
        LayerMask newEnemyLayer,
        bool newVisualOnly = false,
        ulong newSourcePlayerId = ulong.MaxValue,
        float radiusMultiplier = 1f,
        float durationMultiplier = 1f
    )
    {
        element = newElement;
        damagePerTick = newDamagePerTick;
        enemyLayer = newEnemyLayer;
        visualOnly = newVisualOnly;
        sourcePlayerId = newSourcePlayerId;
        activeRadius = radius * Mathf.Max(0.01f, radiusMultiplier);
        remainingDuration = duration * Mathf.Max(0.01f, durationMultiplier);

        ElementVisuals.ApplyColor(gameObject, element);

        // 스프라이트가 지름 1 유닛 기준일 때 radius에 맞게 크기 조정
        transform.localScale = Vector3.one * (activeRadius * 2f);

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

        remainingDuration -= Time.deltaTime;

        if (remainingDuration <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (Time.time < nextTickTime)
        {
            return;
        }

        nextTickTime = Time.time + tickInterval;

        if (!visualOnly)
        {
            DamageEnemiesInside();
        }
    }

    private void DamageEnemiesInside()
    {
        Collider2D[] enemiesInRange =
            Physics2D.OverlapCircleAll(
                transform.position,
                activeRadius,
                enemyLayer
            );

        HashSet<int> damagedIds = new HashSet<int>();

        foreach (Collider2D enemyCollider in enemiesInRange)
        {
            IDamageable damageable =
                enemyCollider.GetComponentInParent<IDamageable>();

            if (damageable == null)
            {
                continue;
            }

            Transform enemyRoot =
                enemyCollider.attachedRigidbody != null
                    ? enemyCollider.attachedRigidbody.transform
                    : enemyCollider.transform.root;

            int enemyId = enemyRoot.gameObject.GetInstanceID();

            if (!damagedIds.Add(enemyId))
            {
                continue;
            }

            IElementReceiver receiver =
                enemyCollider.GetComponentInParent<IElementReceiver>();
            receiver?.RecordElement(element, sourcePlayerId);

            damageable.TakeDamage(damagePerTick);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        float drawRadius = Application.isPlaying && activeRadius > 0f
            ? activeRadius
            : radius;
        Gizmos.DrawWireSphere(transform.position, drawRadius);
    }
}
