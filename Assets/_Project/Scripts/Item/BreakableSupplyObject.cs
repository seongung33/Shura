using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public sealed class BreakableSupplyObject : NetworkBehaviour, IDamageable
{
    [SerializeField, Min(1f)]
    private float maxHealth = 60f;

    [SerializeField]
    private FieldItemPickup pickupPrefab;

    private readonly NetworkVariable<float> networkHealth = new();

    private float localHealth;
    private bool destroyed;

    public float CurrentHealth =>
        IsSpawned ? networkHealth.Value : localHealth;

    private void Awake()
    {
        localHealth = maxHealth;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            networkHealth.Value = maxHealth;
        }
    }

    public void TakeDamage(float damage)
    {
        if (!IsValidDamage(damage))
        {
            return;
        }

        if (!IsSpawned)
        {
            ApplyDamage(damage);
            return;
        }

        if (IsServer)
        {
            ApplyDamage(damage);
        }
        else
        {
            RequestDamageRpc(damage);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestDamageRpc(float damage)
    {
        if (IsValidDamage(damage))
        {
            ApplyDamage(damage);
        }
    }

    private void ApplyDamage(float damage)
    {
        if (destroyed || (IsSpawned && !IsServer))
        {
            return;
        }

        float nextHealth = Mathf.Max(0f, CurrentHealth - damage);

        if (IsSpawned)
        {
            networkHealth.Value = nextHealth;
        }
        else
        {
            localHealth = nextHealth;
        }

        if (nextHealth <= 0f)
        {
            Break();
        }
    }

    private void Break()
    {
        if (destroyed)
        {
            return;
        }

        destroyed = true;
        TryDropSingleItem();

        if (IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void TryDropSingleItem()
    {
        if (pickupPrefab == null ||
            !FieldSupplyLootTable.TryRoll(out FieldItemType itemType))
        {
            return;
        }

        FieldItemPickup pickup = Instantiate(
            pickupPrefab,
            transform.position,
            Quaternion.identity
        );
        pickup.Initialize(itemType);

        if (!IsSpawned)
        {
            return;
        }

        NetworkObject pickupNetworkObject =
            pickup.GetComponent<NetworkObject>();

        if (pickupNetworkObject == null)
        {
            Destroy(pickup.gameObject);
            return;
        }

        pickupNetworkObject.Spawn();
    }

    private static bool IsValidDamage(float damage)
    {
        return damage > 0f &&
            !float.IsNaN(damage) &&
            !float.IsInfinity(damage);
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
    }
}
