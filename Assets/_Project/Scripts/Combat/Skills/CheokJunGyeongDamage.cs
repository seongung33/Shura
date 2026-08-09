using System.Collections.Generic;
using UnityEngine;

internal static class CheokJunGyeongDamage
{
    public static void Circle(
        Vector2 center,
        float radius,
        float damage,
        ElementType element,
        ulong sourcePlayerId,
        LayerMask enemyLayer
    )
    {
        DamageColliders(
            Physics2D.OverlapCircleAll(center, radius, enemyLayer),
            damage,
            element,
            sourcePlayerId
        );
    }

    public static void Box(
        Vector2 center,
        Vector2 size,
        float angle,
        float damage,
        ElementType element,
        ulong sourcePlayerId,
        LayerMask enemyLayer
    )
    {
        DamageColliders(
            Physics2D.OverlapBoxAll(center, size, angle, enemyLayer),
            damage,
            element,
            sourcePlayerId
        );
    }

    public static void Arc(
        Vector2 center,
        Vector2 direction,
        float radius,
        float angle,
        float damage,
        ElementType element,
        ulong sourcePlayerId,
        LayerMask enemyLayer
    )
    {
        Collider2D[] candidates = Physics2D.OverlapCircleAll(
            center,
            radius,
            enemyLayer
        );
        HashSet<int> damagedIds = new();
        Vector2 forward = direction.sqrMagnitude > 0.001f
            ? direction.normalized
            : Vector2.right;
        float minimumDot = Mathf.Cos(Mathf.Clamp(angle, 0f, 360f) *
            0.5f * Mathf.Deg2Rad);

        foreach (Collider2D candidate in candidates)
        {
            Transform root = GetRoot(candidate);
            Vector2 toTarget = (Vector2)root.position - center;

            if (toTarget.sqrMagnitude > 0.001f &&
                Vector2.Dot(forward, toTarget.normalized) < minimumDot)
            {
                continue;
            }

            DamageCollider(
                candidate,
                root,
                damagedIds,
                damage,
                element,
                sourcePlayerId
            );
        }
    }

    private static void DamageColliders(
        Collider2D[] candidates,
        float damage,
        ElementType element,
        ulong sourcePlayerId
    )
    {
        HashSet<int> damagedIds = new();

        foreach (Collider2D candidate in candidates)
        {
            DamageCollider(
                candidate,
                GetRoot(candidate),
                damagedIds,
                damage,
                element,
                sourcePlayerId
            );
        }
    }

    private static void DamageCollider(
        Collider2D candidate,
        Transform root,
        HashSet<int> damagedIds,
        float damage,
        ElementType element,
        ulong sourcePlayerId
    )
    {
        IDamageable damageable = candidate.GetComponentInParent<IDamageable>();

        if (damageable == null || !damagedIds.Add(root.gameObject.GetInstanceID()))
        {
            return;
        }

        IElementReceiver receiver =
            candidate.GetComponentInParent<IElementReceiver>();
        receiver?.RecordElement(element, sourcePlayerId);
        damageable.TakeDamage(Mathf.Max(0f, damage));
    }

    private static Transform GetRoot(Collider2D collider)
    {
        return collider.attachedRigidbody != null
            ? collider.attachedRigidbody.transform
            : collider.transform.root;
    }
}
