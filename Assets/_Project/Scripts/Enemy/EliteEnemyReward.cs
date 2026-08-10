using UnityEngine;

public sealed class EliteEnemyReward : MonoBehaviour
{
    private EnemyHealth health;
    private bool rewardGranted;

    public void Configure(EnemyHealth configuredHealth)
    {
        if (health != null)
        {
            health.Died -= HandleDied;
        }

        health = configuredHealth;

        if (health != null)
        {
            health.Died += HandleDied;
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.Died -= HandleDied;
        }
    }

    private void HandleDied(EnemyHealth defeatedEnemy)
    {
        if (rewardGranted)
        {
            return;
        }

        rewardGranted = true;
        RelicRewardCoordinator.Active?.BeginEliteReward();
    }
}
