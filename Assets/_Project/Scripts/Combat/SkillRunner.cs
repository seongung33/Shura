using UnityEngine;
using Shura.Player;

public class SkillRunner : MonoBehaviour
{
    [SerializeField]
    private Transform firePoint;

    public bool TryRun(
        SkillData skillData,
        Transform target
    )
    {
        PlayerHealth playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth != null && playerHealth.IsDead)
        {
            return false;
        }

        if (skillData == null)
        {
            Debug.LogWarning("SkillData가 연결되지 않았습니다.");
            return false;
        }

        if (target == null)
        {
            return false;
        }

        if (skillData.SkillPrefab == null)
        {
            Debug.LogWarning(
                $"{skillData.name}에 Skill Prefab이 연결되지 않았습니다."
            );

            return false;
        }

        Vector3 spawnPosition =
            firePoint != null
                ? firePoint.position
                : transform.position;

        GameObject skillObject = Instantiate(
            skillData.SkillPrefab,
            spawnPosition,
            Quaternion.identity
        );

        Projectile projectile =
            skillObject.GetComponent<Projectile>();

        if (projectile == null)
        {
            Debug.LogError(
                $"{skillData.SkillPrefab.name}에 Projectile 컴포넌트가 없습니다."
            );

            Destroy(skillObject);
            return false;
        }

        projectile.Initialize(
            target,
            skillData.ProjectileSpeed,
            skillData.Damage
        );

        return true;
    }
}
