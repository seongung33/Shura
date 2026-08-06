using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적토마 (P0): 일정 시간 이동 속도 증가 + 접촉한 적에게 피해 +
/// 지나간 자리에 원소 장판을 남긴다.
/// 프리팹 루트에 이 컴포넌트를 붙이고, 시전 시 플레이어를 따라다닌다.
/// 이동 속도 증가는 PlayerController.SpeedMultiplier를 사용한다.
/// </summary>
public class JeoktomaDash : MonoBehaviour, ISkillBehaviour
{
    [Header("Buff")]

    [Min(0.1f)]
    [SerializeField]
    private float duration = 4f;

    [Min(1f)]
    [SerializeField]
    private float speedMultiplier = 1.6f;

    [Header("Contact Damage")]

    [Tooltip("플레이어 주변 이 반경 안의 적을 밟은 것으로 판정")]
    [Min(0.1f)]
    [SerializeField]
    private float trampleRadius = 0.7f;

    [Min(0.05f)]
    [SerializeField]
    private float trampleInterval = 0.4f;

    [Header("Elemental Trail")]

    [SerializeField]
    private ElementalZone zonePrefab;

    [Tooltip("장판을 남기는 거리 간격")]
    [Min(0.1f)]
    [SerializeField]
    private float zoneSpacing = 1.2f;

    [Tooltip("장판 틱 피해 = 스킬 피해 x 배율")]
    [Min(0f)]
    [SerializeField]
    private float zoneDamageMultiplier = 0.3f;

    private GameObject owner;
    private Shura.Player.PlayerController ownerController;
    private NetworkPlayerMovement ownerNetworkMovement;
    private float damage;
    private ElementType element;
    private LayerMask enemyLayer;
    private bool visualOnly;
    private ulong sourcePlayerId;

    private bool isActive;
    private float endTime;
    private float nextTrampleTime;
    private Vector2 lastZonePosition;

    public void Cast(SkillCastContext context)
    {
        owner = context.Owner;
        damage = context.Damage;
        element = context.Element;
        enemyLayer = context.EnemyLayer;
        visualOnly = context.VisualOnly;
        sourcePlayerId = context.SourcePlayerId;

        ownerController =
            owner.GetComponent<Shura.Player.PlayerController>();
        ownerNetworkMovement =
            owner.GetComponent<NetworkPlayerMovement>();

        if (ownerController != null)
        {
            ownerController.SpeedMultiplier = speedMultiplier;
        }

        if (ownerNetworkMovement != null)
        {
            ownerNetworkMovement.SpeedMultiplier = speedMultiplier;
        }

        // 플레이어를 따라다니도록 부착
        transform.SetParent(owner.transform);
        transform.localPosition = Vector3.zero;

        ElementVisuals.ApplyColor(gameObject, element);

        endTime = Time.time + duration;
        lastZonePosition = owner.transform.position;

        SpawnZone();

        isActive = true;
    }

    private void Update()
    {
        if (!isActive)
        {
            return;
        }

        if (owner == null || Time.time >= endTime)
        {
            EndDash();
            return;
        }

        if (!visualOnly)
        {
            TrampleEnemies();
        }
        TrySpawnZone();
    }

    private void TrampleEnemies()
    {
        if (Time.time < nextTrampleTime)
        {
            return;
        }

        nextTrampleTime = Time.time + trampleInterval;

        Collider2D[] enemiesInRange =
            Physics2D.OverlapCircleAll(
                owner.transform.position,
                trampleRadius,
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

            damageable.TakeDamage(damage);
        }
    }

    private void TrySpawnZone()
    {
        Vector2 currentPosition = owner.transform.position;

        float distanceMoved =
            Vector2.Distance(currentPosition, lastZonePosition);

        if (distanceMoved < zoneSpacing)
        {
            return;
        }

        lastZonePosition = currentPosition;

        SpawnZone();
    }

    private void SpawnZone()
    {
        if (zonePrefab == null)
        {
            return;
        }

        ElementalZone zone = Instantiate(
            zonePrefab,
            owner.transform.position,
            Quaternion.identity
        );

        zone.Initialize(
            element,
            damage * zoneDamageMultiplier,
            enemyLayer,
            visualOnly,
            sourcePlayerId
        );
    }

    private void EndDash()
    {
        if (ownerController != null)
        {
            ownerController.SpeedMultiplier = 1f;
        }
        if (ownerNetworkMovement != null)
        {
            ownerNetworkMovement.SpeedMultiplier = 1f;
        }

        isActive = false;

        if (Application.isPlaying)
        {
            Destroy(gameObject);
        }
    }
}
