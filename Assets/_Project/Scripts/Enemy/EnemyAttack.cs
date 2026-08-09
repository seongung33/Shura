using UnityEngine;
using System.Collections.Generic;

public class EnemyAttack : MonoBehaviour
{
    private static readonly Dictionary<GameObject, HashSet<EnemyAttack>>
        ActiveAttackers = new();

    [SerializeField]
    private float damage = 10f;

    [SerializeField]
    private float attackInterval = 1f;

    private float nextAttackTime;
    private int maxAttackersPerPlayer = 5;
    private GameObject slottedPlayer;

    public void ConfigureRuntime(
        float damageMultiplier,
        int configuredMaxAttackersPerPlayer
    )
    {
        damage *= Mathf.Max(0.01f, damageMultiplier);
        attackInterval = Mathf.Max(0.75f, attackInterval);
        maxAttackersPerPlayer = Mathf.Max(1, configuredMaxAttackersPerPlayer);
    }

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

        if (!TryAcquireAttackSlot(collision.gameObject))
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

    private bool TryAcquireAttackSlot(GameObject player)
    {
        if (slottedPlayer == player)
        {
            return true;
        }

        ReleaseAttackSlot();

        if (!ActiveAttackers.TryGetValue(player, out HashSet<EnemyAttack> attackers))
        {
            attackers = new HashSet<EnemyAttack>();
            ActiveAttackers[player] = attackers;
        }

        attackers.RemoveWhere(attacker => attacker == null);

        if (attackers.Count >= maxAttackersPerPlayer)
        {
            return false;
        }

        attackers.Add(this);
        slottedPlayer = player;
        return true;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject == slottedPlayer)
        {
            ReleaseAttackSlot();
        }
    }

    private void OnDisable()
    {
        ReleaseAttackSlot();
    }

    private void ReleaseAttackSlot()
    {
        if (slottedPlayer == null)
        {
            return;
        }

        if (ActiveAttackers.TryGetValue(
                slottedPlayer,
                out HashSet<EnemyAttack> attackers
            ))
        {
            attackers.Remove(this);

            if (attackers.Count == 0)
            {
                ActiveAttackers.Remove(slottedPlayer);
            }
        }

        slottedPlayer = null;
    }
}
