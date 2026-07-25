using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [SerializeField]
    private float damage = 10f;

    [SerializeField]
    private float attackInterval = 1f;

    private float nextAttackTime;

    private void OnCollisionStay2D(Collision2D collision)
    {
        
        // 부딪힌 대상이 플레이어가 아니면 종료
        if (!collision.gameObject.CompareTag("Player"))
        {
            return;
        }
        IDamageable damageable =
            collision.gameObject.GetComponent<IDamageable>();

        // 위에서 검사했지만 Player에 IDamageable 이 없는 것을 알 수 있다.
        if (damageable == null)
        {
            return;
        }

        // 아직 다음 공격 시간이 아니면 종료
        if (Time.time < nextAttackTime)
        {
            return;
        }

        damageable.TakeDamage(damage);

        nextAttackTime = Time.time + attackInterval;
    }
}