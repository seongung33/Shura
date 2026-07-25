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
        PlayerHealth playerHealth =
            collision.gameObject.GetComponent<PlayerHealth>();

        // 부딪힌 대상이 플레이어가 아니면 종료
        if (playerHealth == null)
        {
            return;
        }

        // 아직 다음 공격 시간이 아니면 종료
        if (Time.time < nextAttackTime)
        {
            return;
        }

        playerHealth.TakeDamage(damage);

        nextAttackTime = Time.time + attackInterval;
    }
}