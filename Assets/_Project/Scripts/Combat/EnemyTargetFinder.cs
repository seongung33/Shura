using UnityEngine;

public static class EnemyTargetFinder
{
    public static Transform FindNearestEnemy(
        Vector2 searchOrigin,
        float searchRange,
        LayerMask enemyLayer
    )
    {
        Collider2D[] enemyColliders =
            Physics2D.OverlapCircleAll(
                searchOrigin,
                searchRange,
                enemyLayer
            );

        Transform nearestEnemy = null;
        float nearestDistanceSquared = float.MaxValue;

        foreach (Collider2D enemyCollider in enemyColliders)
        {
            IDamageable damageable =
                enemyCollider.GetComponentInParent<IDamageable>();
            Component damageableComponent = damageable as Component;

            if (damageableComponent == null)
            {
                continue;
            }

            Vector2 enemyPosition =
                damageableComponent.transform.position;

            Vector2 difference =
                enemyPosition - searchOrigin;

            float distanceSquared =
                difference.sqrMagnitude;

            if (distanceSquared >= nearestDistanceSquared)
            {
                continue;
            }

            nearestDistanceSquared = distanceSquared;
            nearestEnemy = damageableComponent.transform;
        }

        return nearestEnemy;
    }
}
