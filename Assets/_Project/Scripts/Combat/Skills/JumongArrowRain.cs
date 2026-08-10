using System.Collections.Generic;
using UnityEngine;

public sealed class JumongArrowRain : MonoBehaviour,
    ISkillBehaviour,
    ISkillCastOriginResolver
{
    private sealed class FallingArrow
    {
        public GameObject GameObject;
        public Vector2 Start;
        public Vector2 End;
        public float Age;
    }

    [SerializeField, Min(0f)]
    private float warningDuration = 0.3f;

    [SerializeField, Min(0.1f)]
    private float activeDuration = 2.5f;

    [SerializeField, Min(0.1f)]
    private float attackRadius = 4.5f;

    [SerializeField, Min(0.02f)]
    private float damageTickInterval = 0.25f;

    [SerializeField, Min(0f)]
    private float fallbackDistance = 4f;

    [SerializeField, Min(1)]
    private int arrowsPerBurst = 2;

    [SerializeField, Min(0.02f)]
    private float visualSpawnInterval = 0.08f;

    [SerializeField, Min(0.05f)]
    private float arrowFallDuration = 0.18f;

    [SerializeField, Min(0.1f)]
    private float arrowHeight = 1.6f;

    [SerializeField]
    private Sprite arrowSprite;

    [SerializeField]
    private Color warningColor = new Color(1f, 0.25f, 0.15f, 0.28f);

    [SerializeField]
    private Color activeColor = new Color(1f, 0.85f, 0.35f, 0.18f);

    [SerializeField]
    private Color arrowColor = new Color(1f, 0.95f, 0.7f, 1f);

    private readonly List<FallingArrow> fallingArrows = new();
    private SkillCastContext castContext;
    private SpriteRenderer warningRenderer;
    private System.Random visualRandom;
    private float elapsed;
    private float nextDamageTick;
    private float nextVisualSpawn;
    private bool active;

    private void Awake()
    {
        warningRenderer = GetComponent<SpriteRenderer>();
    }

    public Vector2 ResolveCastOrigin(
        GameObject owner,
        Vector2 direction,
        float searchRange,
        LayerMask enemyLayer
    )
    {
        Vector2 ownerPosition = owner != null
            ? owner.transform.position
            : Vector2.zero;
        Vector2 fallbackDirection = direction.sqrMagnitude > 0.001f
            ? direction.normalized
            : Vector2.right;
        List<Vector2> enemyPositions = CollectEnemyPositions(
            ownerPosition,
            Mathf.Max(0.1f, searchRange),
            enemyLayer
        );

        if (enemyPositions.Count == 0)
        {
            return ownerPosition + fallbackDirection * fallbackDistance;
        }

        Vector2 bestCenter = enemyPositions[0];
        int bestCount = -1;
        float bestDistanceSquared = float.MaxValue;
        float radiusSquared = attackRadius * attackRadius;

        foreach (Vector2 candidate in enemyPositions)
        {
            int count = 0;

            foreach (Vector2 enemyPosition in enemyPositions)
            {
                if ((enemyPosition - candidate).sqrMagnitude <= radiusSquared)
                {
                    count++;
                }
            }

            float distanceSquared = (candidate - ownerPosition).sqrMagnitude;

            if (count > bestCount ||
                (count == bestCount && distanceSquared < bestDistanceSquared))
            {
                bestCenter = candidate;
                bestCount = count;
                bestDistanceSquared = distanceSquared;
            }
        }

        return bestCenter;
    }

    public void Cast(SkillCastContext context)
    {
        castContext = context;
        transform.position = context.Origin;
        transform.localScale = Vector3.one * (attackRadius * 2f);
        visualRandom = new System.Random(CreateVisualSeed(context));
        elapsed = 0f;
        nextDamageTick = 0f;
        nextVisualSpawn = 0f;
        active = true;

        if (warningRenderer != null)
        {
            warningRenderer.enabled = true;
            warningRenderer.color = warningColor;
        }
    }

    private void Update()
    {
        if (!active || GameplayPauseState.IsLevelUpActive)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        elapsed += deltaTime;
        UpdateFallingArrows(deltaTime);

        if (elapsed < warningDuration)
        {
            UpdateWarningVisual();
            return;
        }

        float activeElapsed = elapsed - warningDuration;
        UpdateActiveVisual();

        while (nextVisualSpawn < activeDuration &&
            activeElapsed >= nextVisualSpawn)
        {
            SpawnArrowBurst();
            nextVisualSpawn += visualSpawnInterval;
        }

        if (!castContext.VisualOnly)
        {
            while (nextDamageTick < activeDuration &&
                activeElapsed >= nextDamageTick)
            {
                DamageEnemies();
                nextDamageTick += damageTickInterval;
            }
        }

        if (activeElapsed < activeDuration)
        {
            return;
        }

        if (warningRenderer != null)
        {
            warningRenderer.enabled = false;
        }

        if (fallingArrows.Count == 0)
        {
            active = false;
            Destroy(gameObject);
        }
    }

    private List<Vector2> CollectEnemyPositions(
        Vector2 center,
        float searchRange,
        LayerMask enemyLayer
    )
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            center,
            searchRange,
            enemyLayer
        );
        HashSet<int> enemyIds = new();
        List<Vector2> positions = new();

        foreach (Collider2D collider in colliders)
        {
            EnemyHealth health = collider.GetComponentInParent<EnemyHealth>();

            if (health == null ||
                health.CurrentHealth <= 0f ||
                !enemyIds.Add(health.GetInstanceID()))
            {
                continue;
            }

            positions.Add(health.transform.position);
        }

        return positions;
    }

    private void DamageEnemies()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            transform.position,
            attackRadius,
            castContext.EnemyLayer
        );
        HashSet<int> damagedIds = new();

        foreach (Collider2D collider in colliders)
        {
            IDamageable damageable = collider.GetComponentInParent<IDamageable>();
            Component damageableComponent = damageable as Component;

            if (damageable == null ||
                damageableComponent == null ||
                !damagedIds.Add(damageableComponent.GetInstanceID()))
            {
                continue;
            }

            EnemyHealth health = damageableComponent as EnemyHealth;

            if (health != null && health.CurrentHealth <= 0f)
            {
                continue;
            }

            RelicCombat.ApplyDamage(
                damageable,
                collider,
                Mathf.Max(0f, castContext.Damage),
                RelicTriggerContext.PlayerDirect(
                    castContext.SourcePlayerId,
                    damageableComponent.transform.position,
                    castContext.Element,
                    RelicAttackType.DamageOverTime
                )
            );
        }
    }

    private void UpdateWarningVisual()
    {
        if (warningRenderer == null)
        {
            return;
        }

        Color color = warningColor;
        color.a *= 0.65f + Mathf.PingPong(elapsed * 5f, 0.35f);
        warningRenderer.color = color;
    }

    private void UpdateActiveVisual()
    {
        if (warningRenderer != null)
        {
            warningRenderer.color = activeColor;
        }
    }

    private void SpawnArrowBurst()
    {
        if (arrowSprite == null || visualRandom == null)
        {
            return;
        }

        for (int index = 0; index < arrowsPerBurst; index++)
        {
            double angle = visualRandom.NextDouble() * Mathf.PI * 2f;
            float distance = Mathf.Sqrt((float)visualRandom.NextDouble()) *
                attackRadius;
            Vector2 landing = new Vector2(
                Mathf.Cos((float)angle),
                Mathf.Sin((float)angle)
            ) * distance;
            Vector2 start = landing + Vector2.up * arrowHeight;
            Vector2 worldStart = (Vector2)transform.position + start;
            Vector2 worldLanding = (Vector2)transform.position + landing;
            GameObject arrowObject = new GameObject("ArrowRainDrop");
            arrowObject.transform.position = worldStart;
            arrowObject.transform.rotation = Quaternion.Euler(0f, 0f, -90f);
            arrowObject.transform.localScale = Vector3.one * 0.55f;

            SpriteRenderer renderer = arrowObject.AddComponent<SpriteRenderer>();
            renderer.sprite = arrowSprite;
            renderer.color = arrowColor;

            if (warningRenderer != null)
            {
                renderer.sortingLayerID = warningRenderer.sortingLayerID;
                renderer.sortingOrder = warningRenderer.sortingOrder + 1;
                renderer.sharedMaterial = warningRenderer.sharedMaterial;
            }

            fallingArrows.Add(new FallingArrow
            {
                GameObject = arrowObject,
                Start = worldStart,
                End = worldLanding,
                Age = 0f
            });
        }
    }

    private void UpdateFallingArrows(float deltaTime)
    {
        for (int index = fallingArrows.Count - 1; index >= 0; index--)
        {
            FallingArrow arrow = fallingArrows[index];

            if (arrow.GameObject == null)
            {
                fallingArrows.RemoveAt(index);
                continue;
            }

            arrow.Age += deltaTime;
            float progress = Mathf.Clamp01(arrow.Age / arrowFallDuration);
            arrow.GameObject.transform.position = Vector2.Lerp(
                arrow.Start,
                arrow.End,
                progress
            );

            if (progress < 1f)
            {
                continue;
            }

            Destroy(arrow.GameObject);
            fallingArrows.RemoveAt(index);
        }
    }

    private static int CreateVisualSeed(SkillCastContext context)
    {
        unchecked
        {
            int x = Mathf.RoundToInt(context.Origin.x * 100f);
            int y = Mathf.RoundToInt(context.Origin.y * 100f);
            int source = (int)(
                context.SourcePlayerId ^ (context.SourcePlayerId >> 32)
            );
            return ((x * 397) ^ y) * 397 ^ source;
        }
    }

    private void OnDestroy()
    {
        foreach (FallingArrow arrow in fallingArrows)
        {
            if (arrow.GameObject != null)
            {
                Destroy(arrow.GameObject);
            }
        }

        fallingArrows.Clear();
    }
}
