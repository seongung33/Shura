using UnityEngine;

public class PlayerAutoAttack : MonoBehaviour
{
    [SerializeField]
    private float attackRange = 5f;

    [SerializeField]
    private float attackDamage = 25f;

    [SerializeField]
    private float attackInterval = 1f;

    [SerializeField]
    private Projectile projectilePrefab;
    
    private float nextAttackTime;

    private void Update()
    {
        // 다음 공격 시간이 아직 되지 않았다면 종료
        if (Time.time < nextAttackTime)
        {
            return;
        }

        EnemyHealth nearestEnemy = FindNearestEnemy();

        // 범위 안에 적이 없으면 공격하지 않음
        if (nearestEnemy == null)
        {
            return;
        }

        FireProjectile(nearestEnemy);

        // 다음 공격 가능 시간 설정
        nextAttackTime = Time.time + attackInterval;
    }

    private void FireProjectile(EnemyHealth target)
    {
        Projectile newProjectile = Instantiate(
            projectilePrefab,
            transform.position,
            Quaternion.identity
        );
        newProjectile.Initialize(target, attackDamage);
    } 
        
    // 가까운 적 1개 찾는 코드
    private EnemyHealth FindNearestEnemy()
    {
        EnemyHealth[] enemies =
            FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);

        EnemyHealth nearestEnemy = null;
        float nearestDistance = attackRange;

        foreach (EnemyHealth enemy in enemies)
        {
            float distance = Vector2.Distance(
                transform.position,
                enemy.transform.position
            );

            if (distance <= nearestDistance)
            {
                nearestDistance = distance;
                nearestEnemy = enemy;
            }
        }

        return nearestEnemy;
    }
}