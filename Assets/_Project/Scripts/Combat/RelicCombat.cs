using Unity.Netcode;
using UnityEngine;

public enum RelicAttackType
{
    Projectile,
    Melee,
    Area,
    DamageOverTime,
    Dash
}

public readonly struct RelicTriggerContext
{
    public ulong SourcePlayerId { get; }
    public Vector2 HitPosition { get; }
    public bool CanTriggerRelics { get; }
    public ElementType Element { get; }
    public RelicAttackType AttackType { get; }

    private RelicTriggerContext(
        ulong sourcePlayerId,
        Vector2 hitPosition,
        bool canTriggerRelics,
        ElementType element,
        RelicAttackType attackType
    )
    {
        SourcePlayerId = sourcePlayerId;
        HitPosition = hitPosition;
        CanTriggerRelics = canTriggerRelics;
        Element = element;
        AttackType = attackType;
    }

    public static RelicTriggerContext PlayerDirect(
        ulong sourcePlayerId,
        Vector2 hitPosition,
        ElementType element,
        RelicAttackType attackType
    )
    {
        return new RelicTriggerContext(
            sourcePlayerId,
            hitPosition,
            true,
            element,
            attackType
        );
    }

    public static RelicTriggerContext RelicEffect(
        ulong sourcePlayerId,
        Vector2 hitPosition
    )
    {
        return new RelicTriggerContext(
            sourcePlayerId,
            hitPosition,
            false,
            ElementType.None,
            RelicAttackType.Area
        );
    }
}

public static class RelicCombat
{
    public static void ApplyDamage(
        IDamageable damageable,
        Component hitComponent,
        float damage,
        RelicTriggerContext context
    )
    {
        if (damageable == null ||
            hitComponent == null ||
            damage <= 0f ||
            float.IsNaN(damage) ||
            float.IsInfinity(damage))
        {
            return;
        }

        EnemyHealth health = hitComponent.GetComponentInParent<EnemyHealth>();
        Transform primaryTarget = health != null
            ? health.transform
            : hitComponent.transform;
        bool wasAlive = health == null || health.CurrentHealth > 0f;
        bool directKill = health != null &&
            health.CurrentHealth > 0f &&
            damage >= health.CurrentHealth;

        damageable.TakeDamage(damage);

        if (!context.CanTriggerRelics ||
            !wasAlive ||
            !PlayerRelicInventory.TryGet(
                context.SourcePlayerId,
                out PlayerRelicInventory inventory
            ) ||
            !inventory.CanRunAuthoritativeEffects)
        {
            return;
        }

        inventory.HandleDirectHit(
            primaryTarget,
            context.HitPosition,
            damage,
            context.Element,
            context.AttackType
        );

        if (directKill)
        {
            inventory.HandleDirectKill(
                context.HitPosition,
                damage,
                context.Element
            );
        }
    }

    public static void ApplyRelicDamage(
        Component hitComponent,
        float damage,
        ulong sourcePlayerId,
        Vector2 hitPosition,
        ElementType element
    )
    {
        IDamageable damageable = hitComponent != null
            ? hitComponent.GetComponentInParent<IDamageable>()
            : null;

        IElementReceiver receiver = hitComponent != null
            ? hitComponent.GetComponentInParent<IElementReceiver>()
            : null;
        receiver?.RecordElement(element, sourcePlayerId);

        ApplyDamage(
            damageable,
            hitComponent,
            damage,
            RelicTriggerContext.RelicEffect(sourcePlayerId, hitPosition)
        );
    }

    public static void ApplyNetworkCastModifiers(
        GameObject owner,
        SkillData skill,
        ref SkillCastRuntime runtime
    )
    {
        if (owner == null ||
            skill == null ||
            skill.SkillPrefab == null ||
            skill.SkillPrefab.GetComponent<StraightProjectile>() == null)
        {
            return;
        }

        PlayerRelicInventory inventory =
            owner.GetComponent<PlayerRelicInventory>();

        if (inventory != null && inventory.TryGrantAdditionalPierce())
        {
            runtime.PierceBonus++;
        }
    }

    public static void ApplyOfflineProjectileModifiers(
        GameObject owner,
        ref SkillCastContext context
    )
    {
        NetworkManager manager = NetworkManager.Singleton;

        if (manager != null && manager.IsListening)
        {
            return;
        }

        PlayerRelicInventory inventory = owner != null
            ? owner.GetComponent<PlayerRelicInventory>()
            : null;

        if (inventory != null && inventory.TryGrantAdditionalPierce())
        {
            context.PierceBonus++;
        }
    }
}
