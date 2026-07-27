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
            EnemyHealth enemyHealth =
                enemyCollider.GetComponentInParent<EnemyHealth>();

            if (enemyHealth == null)
            {
                continue;
            }

            Vector2 enemyPosition =
                enemyHealth.transform.position;

            Vector2 difference =
                enemyPosition - searchOrigin;

            float distanceSquared =
                difference.sqrMagnitude;

            if (distanceSquared >= nearestDistanceSquared)
            {
                continue;
            }

            nearestDistanceSquared = distanceSquared;
            nearestEnemy = enemyHealth.transform;
        }

        return nearestEnemy;
    }
}