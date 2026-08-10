using Unity.Netcode;
using UnityEngine;

public sealed class PajinDash : MonoBehaviour, ISkillBehaviour
{
    [SerializeField, Min(0.1f)]
    private float dashDistance = 3.5f;

    [SerializeField, Min(0.05f)]
    private float dashDuration = 0.22f;

    [SerializeField, Min(0.1f)]
    private float pathHalfWidth = 0.6f;

    [SerializeField, Min(0.1f)]
    private float shockwaveRadius = 1.35f;

    [SerializeField, Min(0f)]
    private float shockwaveDamageMultiplier = 0.75f;

    [SerializeField]
    private ElementalZone zonePrefab;

    [SerializeField, Min(0.1f)]
    private float zoneSpacing = 1.1f;

    [SerializeField, Min(0f)]
    private float zoneDamageMultiplier = 0.25f;

    private GameObject owner;
    private Rigidbody2D ownerBody;
    private Shura.Player.PlayerController localMovement;
    private NetworkPlayerMovement networkMovement;
    private Vector2 start;
    private Vector2 end;
    private float elapsed;
    private bool drivesOwner;
    private bool initialized;
    private bool ended;
    private SkillCastContext castContext;

    public void Cast(SkillCastContext context)
    {
        castContext = context;
        owner = context.Owner;
        Vector2 direction = context.Direction.sqrMagnitude > 0.001f
            ? context.Direction.normalized
            : Vector2.right;
        float distance = dashDistance *
            (context.SkillLevel >= 3 ? 1.25f : 1f);
        float width = pathHalfWidth *
            (context.SkillLevel >= 2 ? 1.2f : 1f);

        start = context.Origin;
        end = ResolveEnd(start, direction, distance);
        transform.position = start;
        transform.right = direction;
        transform.localScale = new Vector3(
            Vector2.Distance(start, end),
            width * 2f,
            1f
        );
        ElementVisuals.ApplyColor(gameObject, context.Element);

        if (!context.VisualOnly)
        {
            Vector2 path = end - start;
            CheokJunGyeongDamage.Box(
                start + path * 0.5f,
                new Vector2(path.magnitude + width, width * 2f),
                Mathf.Atan2(path.y, path.x) * Mathf.Rad2Deg,
                context.Damage,
                context.Element,
                context.SourcePlayerId,
                context.EnemyLayer,
                RelicAttackType.Dash
            );
        }

        if (context.SkillLevel >= 5)
        {
            SpawnTrail(context);
        }

        BindOwnerMovement();
        initialized = true;
    }

    private void FixedUpdate()
    {
        if (!initialized || GameplayPauseState.IsLevelUpActive)
        {
            return;
        }

        elapsed += Time.fixedDeltaTime;
        float progress = Mathf.Clamp01(elapsed / dashDuration);
        Vector2 position = Vector2.Lerp(start, end, progress);
        transform.position = position;

        if (drivesOwner && owner != null)
        {
            if (ownerBody != null)
            {
                ownerBody.position = position;
            }
            else
            {
                owner.transform.position = position;
            }
        }

        if (progress >= 1f)
        {
            EndDash(true);
        }
    }

    private void BindOwnerMovement()
    {
        if (owner == null)
        {
            return;
        }

        NetworkObject networkObject = owner.GetComponent<NetworkObject>();
        drivesOwner = networkObject == null ||
            !networkObject.IsSpawned ||
            networkObject.IsOwner;

        if (!drivesOwner)
        {
            return;
        }

        ownerBody = owner.GetComponent<Rigidbody2D>();
        localMovement = owner.GetComponent<Shura.Player.PlayerController>();
        networkMovement = owner.GetComponent<NetworkPlayerMovement>();

        if (localMovement != null)
        {
            localMovement.SkillMovementOverride = true;
        }

        if (networkMovement != null)
        {
            networkMovement.SkillMovementOverride = true;
        }
    }

    private Vector2 ResolveEnd(
        Vector2 origin,
        Vector2 direction,
        float requestedDistance
    )
    {
        float allowedDistance = requestedDistance;
        RaycastHit2D[] hits = Physics2D.CircleCastAll(
            origin,
            0.3f,
            direction,
            requestedDistance
        );

        foreach (RaycastHit2D hit in hits)
        {
            Collider2D collider = hit.collider;

            if (collider == null ||
                collider.isTrigger ||
                (owner != null && collider.transform.IsChildOf(owner.transform)) ||
                collider.GetComponentInParent<IDamageable>() != null)
            {
                continue;
            }

            allowedDistance = Mathf.Min(
                allowedDistance,
                Mathf.Max(0f, hit.distance - 0.05f)
            );
        }

        return origin + direction * allowedDistance;
    }

    private void SpawnTrail(SkillCastContext context)
    {
        if (zonePrefab == null)
        {
            return;
        }

        float distance = Vector2.Distance(start, end);
        int count = Mathf.Max(1, Mathf.CeilToInt(distance / zoneSpacing));

        for (int index = 0; index <= count; index++)
        {
            Vector2 position = Vector2.Lerp(start, end, index / (float)count);
            ElementalZone zone = Instantiate(
                zonePrefab,
                position,
                Quaternion.identity
            );
            zone.Initialize(
                context.Element,
                context.Damage * zoneDamageMultiplier,
                context.EnemyLayer,
                context.VisualOnly,
                context.SourcePlayerId,
                context.ZoneRadiusMultiplier,
                context.ZoneDurationMultiplier
            );
        }
    }

    private void EndDash(bool completed)
    {
        if (ended)
        {
            return;
        }

        ended = true;
        ReleaseMovementOverride();

        if (completed && castContext.SkillLevel >= 4)
        {
            transform.position = end;
            transform.localScale = Vector3.one * (shockwaveRadius * 2f);

            if (!castContext.VisualOnly)
            {
                CheokJunGyeongDamage.Circle(
                    end,
                    shockwaveRadius,
                    castContext.Damage * shockwaveDamageMultiplier,
                    castContext.Element,
                    castContext.SourcePlayerId,
                    castContext.EnemyLayer,
                    RelicAttackType.Area
                );
            }
        }

        Destroy(gameObject, completed ? 0.12f : 0f);
    }

    private void OnDestroy()
    {
        ReleaseMovementOverride();
    }

    private void ReleaseMovementOverride()
    {
        if (localMovement != null)
        {
            localMovement.SkillMovementOverride = false;
        }

        if (networkMovement != null)
        {
            networkMovement.SkillMovementOverride = false;
        }
    }
}
