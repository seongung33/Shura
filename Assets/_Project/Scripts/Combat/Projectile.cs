using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField]
    private float moveSpeed = 8f;

    private EnemyHealth target;
    private float damage;
    private Vector2 moveDirection;


    public void Initialize(EnemyHealth newTarget, float newDamage)
    {
        target = newTarget;
        damage = newDamage;

    }

    private void Update()
    {
        // 공격 대상이 이미 죽거나 사라졌다면 투사체도 제거
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 currentPosition = transform.position;
        Vector2 targetPosition = target.transform.position;

        transform.position = Vector2.MoveTowards(
            currentPosition,
            targetPosition,
            moveSpeed * Time.deltaTime
        );
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();

        // 부딪힌 물체가 적이 아니면 무시
        if (enemyHealth == null)
        {
            return;
        }

        // 원래 목표로 정한 적이 아니면 무시
        if (enemyHealth != target)
        {
            return;
        }

        enemyHealth.TakeDamage(damage);
        Destroy(gameObject);
    }
}