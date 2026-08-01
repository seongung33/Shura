using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class NetworkEnemyAuthoritySetup : NetworkBehaviour
{
    private EnemyController enemyController;
    private EnemyAttack enemyAttack;
    private Rigidbody2D rigidBody;
    private Collider2D[] colliders;

    private void Awake()
    {
        enemyController = GetComponent<EnemyController>();
        enemyAttack = GetComponent<EnemyAttack>();
        rigidBody = GetComponent<Rigidbody2D>();
        colliders = GetComponents<Collider2D>();
    }

    public override void OnNetworkSpawn()
    {
        SetSimulationEnabled(IsServer);
    }

    public override void OnNetworkDespawn()
    {
        SetSimulationEnabled(false);
    }

    private void SetSimulationEnabled(bool isEnabled)
    {
        if (enemyController != null)
        {
            enemyController.enabled = isEnabled;
        }

        if (enemyAttack != null)
        {
            enemyAttack.enabled = isEnabled;
        }

        if (rigidBody != null)
        {
            rigidBody.simulated = isEnabled;
        }

        foreach (Collider2D enemyCollider in colliders)
        {
            enemyCollider.enabled = isEnabled;
        }
    }
}
