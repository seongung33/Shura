using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class RelicEffectExecutor : MonoBehaviour
{
    private readonly HashSet<int> targetIds = new();
    private readonly List<Collider2D> targets = new();

    private PlayerRelicInventory inventory;
    private LayerMask enemyLayer;

    public void Configure(
        PlayerRelicInventory configuredInventory,
        LayerMask configuredEnemyLayer
    )
    {
        inventory = configuredInventory;
        enemyLayer = configuredEnemyLayer;
    }

    public void ExecuteHitEffect(
        RelicData relic,
        Transform primaryTarget,
        Vector2 hitPosition,
        float directDamage,
        ulong sourcePlayerId
    )
    {
        if (relic == null)
        {
            return;
        }

        switch (relic.Id)
        {
            case RelicId.ThunderFragment:
                ExecuteChainLightning(
                    relic,
                    primaryTarget,
                    hitPosition,
                    directDamage,
                    sourcePlayerId
                );
                break;
            case RelicId.WindTalisman:
                StartCoroutine(WindRoutine(
                    relic,
                    hitPosition,
                    directDamage,
                    sourcePlayerId
                ));
                break;
            case RelicId.BrokenCannon:
                ExecuteAreaDamage(
                    relic,
                    hitPosition,
                    directDamage,
                    sourcePlayerId,
                    0
                );
                inventory.ShowEffect(
                    relic.Id,
                    hitPosition,
                    hitPosition,
                    relic.Radius,
                    0.35f
                );
                break;
            case RelicId.GeneralJade:
                ExecuteAdditionalHit(
                    relic,
                    primaryTarget,
                    hitPosition,
                    directDamage,
                    sourcePlayerId
                );
                break;
        }
    }

    public void ExecuteKillEffect(
        RelicData relic,
        Vector2 killPosition,
        float directDamage,
        ulong sourcePlayerId
    )
    {
        if (relic != null && relic.Id == RelicId.GoblinFire)
        {
            StartCoroutine(GoblinFireRoutine(
                relic,
                killPosition,
                directDamage,
                sourcePlayerId
            ));
        }
    }

    private void ExecuteChainLightning(
        RelicData relic,
        Transform primaryTarget,
        Vector2 hitPosition,
        float directDamage,
        ulong sourcePlayerId
    )
    {
        int excludedId = primaryTarget != null
            ? primaryTarget.gameObject.GetInstanceID()
            : 0;
        CollectTargets(hitPosition, relic.Radius, excludedId);
        int count = Mathf.Min(relic.MaxTargets, targets.Count);

        for (int index = 0; index < count; index++)
        {
            Collider2D target = targets[index];
            Vector2 targetPosition = target.transform.position;
            RelicCombat.ApplyRelicDamage(
                target,
                directDamage * relic.DamageMultiplier,
                sourcePlayerId,
                targetPosition
            );
            inventory.ShowEffect(
                relic.Id,
                hitPosition,
                targetPosition,
                0f,
                0.25f
            );
        }
    }

    private IEnumerator WindRoutine(
        RelicData relic,
        Vector2 position,
        float directDamage,
        ulong sourcePlayerId
    )
    {
        inventory.ShowEffect(
            relic.Id,
            position,
            position,
            relic.Radius,
            relic.Duration
        );

        float elapsed = 0f;
        float nextTick = 0f;

        while (elapsed < relic.Duration)
        {
            if (!GameplayPauseState.IsLevelUpActive)
            {
                elapsed += Time.deltaTime;

                if (elapsed >= nextTick)
                {
                    nextTick += relic.TickInterval;
                    ExecuteAreaDamage(
                        relic,
                        position,
                        directDamage,
                        sourcePlayerId,
                        0
                    );
                }
            }

            yield return null;
        }
    }

    private void ExecuteAdditionalHit(
        RelicData relic,
        Transform primaryTarget,
        Vector2 hitPosition,
        float directDamage,
        ulong sourcePlayerId
    )
    {
        IDamageable damageable = primaryTarget != null
            ? primaryTarget.GetComponentInParent<IDamageable>()
            : null;
        Component target = damageable as Component;

        if (target == null)
        {
            return;
        }

        RelicCombat.ApplyRelicDamage(
            target,
            directDamage * relic.DamageMultiplier,
            sourcePlayerId,
            hitPosition
        );
        inventory.ShowEffect(
            relic.Id,
            hitPosition,
            hitPosition,
            0.45f,
            0.2f
        );
    }

    private IEnumerator GoblinFireRoutine(
        RelicData relic,
        Vector2 origin,
        float directDamage,
        ulong sourcePlayerId
    )
    {
        Transform target = EnemyTargetFinder.FindNearestEnemy(
            origin,
            relic.Radius,
            enemyLayer
        );

        if (target == null)
        {
            yield break;
        }

        Vector2 initialTargetPosition = target.position;
        inventory.ShowEffect(
            relic.Id,
            origin,
            initialTargetPosition,
            0f,
            relic.Duration
        );

        float elapsed = 0f;

        while (elapsed < relic.Duration)
        {
            if (!GameplayPauseState.IsLevelUpActive)
            {
                elapsed += Time.deltaTime;
            }

            yield return null;
        }

        if (target == null)
        {
            yield break;
        }

        IDamageable damageable = target.GetComponentInParent<IDamageable>();
        Component targetComponent = damageable as Component;

        if (targetComponent != null)
        {
            RelicCombat.ApplyRelicDamage(
                targetComponent,
                directDamage * relic.DamageMultiplier,
                sourcePlayerId,
                target.position
            );
        }
    }

    private void ExecuteAreaDamage(
        RelicData relic,
        Vector2 position,
        float directDamage,
        ulong sourcePlayerId,
        int excludedId
    )
    {
        CollectTargets(position, relic.Radius, excludedId);
        int count = Mathf.Min(relic.MaxTargets, targets.Count);

        for (int index = 0; index < count; index++)
        {
            Collider2D target = targets[index];
            RelicCombat.ApplyRelicDamage(
                target,
                directDamage * relic.DamageMultiplier,
                sourcePlayerId,
                target.transform.position
            );
        }
    }

    private void CollectTargets(Vector2 center, float radius, int excludedId)
    {
        targets.Clear();
        targetIds.Clear();

        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            center,
            radius,
            enemyLayer
        );

        foreach (Collider2D collider in colliders)
        {
            IDamageable damageable =
                collider.GetComponentInParent<IDamageable>();
            Component damageableComponent = damageable as Component;
            EnemyHealth health = damageableComponent as EnemyHealth;

            if (damageableComponent == null ||
                (health != null && health.CurrentHealth <= 0f))
            {
                continue;
            }

            int targetId = damageableComponent.gameObject.GetInstanceID();

            if (targetId == excludedId || !targetIds.Add(targetId))
            {
                continue;
            }

            targets.Add(collider);
        }

        targets.Sort((left, right) =>
            Vector2.SqrMagnitude((Vector2)left.transform.position - center)
                .CompareTo(
                    Vector2.SqrMagnitude(
                        (Vector2)right.transform.position - center
                    )
                )
        );
    }
}

public static class RelicEffectVisuals
{
    private const int CircleSegments = 32;
    private static Material lineMaterial;

    public static void Play(
        RelicId id,
        Vector2 origin,
        Vector2 target,
        float radius,
        float duration
    )
    {
        Color color = GetColor(id);
        float lifetime = Mathf.Max(0.15f, duration);

        if (id == RelicId.ThunderFragment || id == RelicId.GoblinFire)
        {
            CreateLine(origin, target, color, lifetime);
            return;
        }

        CreateCircle(target, Mathf.Max(0.35f, radius), color, lifetime);
    }

    private static void CreateLine(
        Vector2 origin,
        Vector2 target,
        Color color,
        float lifetime
    )
    {
        LineRenderer line = CreateRenderer("RelicLine", color, lifetime);

        if (line == null)
        {
            return;
        }

        line.positionCount = 2;
        line.SetPosition(0, origin);
        line.SetPosition(1, target);
    }

    private static void CreateCircle(
        Vector2 center,
        float radius,
        Color color,
        float lifetime
    )
    {
        LineRenderer line = CreateRenderer("RelicCircle", color, lifetime);

        if (line == null)
        {
            return;
        }

        line.loop = true;
        line.positionCount = CircleSegments;

        for (int index = 0; index < CircleSegments; index++)
        {
            float angle = index * Mathf.PI * 2f / CircleSegments;
            line.SetPosition(
                index,
                center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius
            );
        }
    }

    private static LineRenderer CreateRenderer(
        string name,
        Color color,
        float lifetime
    )
    {
        if (lineMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");

            if (shader == null)
            {
                return null;
            }

            lineMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        GameObject effect = new GameObject(name);
        LineRenderer line = effect.AddComponent<LineRenderer>();
        line.sharedMaterial = lineMaterial;
        line.startColor = color;
        line.endColor = color;
        line.startWidth = 0.12f;
        line.endWidth = 0.04f;
        line.useWorldSpace = true;
        line.sortingOrder = 25;
        Object.Destroy(effect, lifetime);
        return line;
    }

    private static Color GetColor(RelicId id)
    {
        return id switch
        {
            RelicId.ThunderFragment => new Color(0.45f, 0.85f, 1f),
            RelicId.WindTalisman => new Color(0.55f, 1f, 0.75f),
            RelicId.BrokenCannon => new Color(1f, 0.42f, 0.18f),
            RelicId.GoblinFire => new Color(0.45f, 0.9f, 1f),
            RelicId.GeneralJade => new Color(1f, 0.9f, 0.3f),
            _ => Color.white
        };
    }
}
