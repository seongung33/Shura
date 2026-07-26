using UnityEngine;

public class PlayerAutoAttack : MonoBehaviour
{
    [SerializeField]
    private SkillData basicSkill;

    [SerializeField]
    private SkillRunner skillRunner;

    [SerializeField]
    private LayerMask enemyLayer;

    private float nextAttackTime;

    private void Awake()
    {
        if (skillRunner == null)
        {
            skillRunner = GetComponent<SkillRunner>();
        }
    }

    private void Update()
    {
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