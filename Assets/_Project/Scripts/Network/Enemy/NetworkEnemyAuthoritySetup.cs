using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class NetworkEnemyAuthoritySetup : NetworkBehaviour
{
    private EnemyController enemyController;
    private EnemyAttack enemyAttack;
    private Rigidbody2D rigidBody;
    private Collider2D[] colliders;
    private RigidbodyType2D originalBodyType;

    private void Awake()
    {
        enemyController = GetComponent<EnemyController>();
        enemyAttack = GetComponent<EnemyAttack>();
        rigidBody = GetComponent<Rigidbody2D>();
        colliders = GetComponents<Collider2D>();

        if (rigidBody != null)
        {
            originalBodyType = rigidBody.bodyType;
        }
    }

    public override void OnNetworkSpawn()
    {
        SetAuthorityState(IsServer);
    }

    public override void OnNetworkDespawn()
    {
        SetAuthorityState(false);
    }

    private void SetAuthorityState(bool hasServerAuthority)
    {
        if (enemyController != null)
        {
            enemyController.enabled = hasServerAuthority;
        }

        if (enemyAttack != null)
        {
            enemyAttack.enabled = hasServerAuthority;
        }

        if (rigidBody != null)
        {
            rigidBody.bodyType = hasServerAuthority
                ? originalBodyType
                : RigidbodyType2D.Kinematic;
            rigidBody.simulated = true;
        }

        foreach (Collider2D enemyCollider in colliders)
        {
            enemyCollider.enabled = true;
        }
    }
}
