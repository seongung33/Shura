using UnityEngine;
using Shura.Player;

public class PlayerAutoAttack : MonoBehaviour
{
    [SerializeField]
    private SkillData basicSkill;

    [SerializeField]
    private SkillRunner skillRunner;

    [SerializeField]
    private LayerMask enemyLayer;

    private float nextAttackTime;
    private PlayerHealth playerHealth;

    private void Awake()
    {
        if (skillRunner == null)
        {
            skillRunner = GetComponent<SkillRunner>();
        }

        playerHealth = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        if (playerHealth != null && playerHealth.IsDead)
        {
            return;
        }

        if (basicSkill == null)
        {
            return;
        }

        if (skillRunner == null)
        {
            return;
        }

        if (Time.time < nextAttackTime)
        {
            return;
        }

        Transform target = EnemyTargetFinder.FindNearestEnemy(
            transform.position,
            basicSkill.Range,
            enemyLayer
            );
        


        if (target == null)
        {
            return;
        }

        bool wasUsed = skillRunner.TryRun(
            basicSkill,
            target
        );

        if (!wasUsed)
        {
            return;
        }

        nextAttackTime =
            Time.time + basicSkill.Cooldown;
    }

}
