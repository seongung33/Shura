using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class ExperienceOrb : NetworkBehaviour
{
    [SerializeField]
    private int experienceAmount = 1;

    [SerializeField, Min(0.1f)]
    private float attractionSpeed = 12f;

    [SerializeField, Min(0.1f)]
    private float collectionValidationDistance = 2f;

    private readonly NetworkVariable<int> networkExperienceAmount =
        new NetworkVariable<int>(1);

    private Transform attractionTarget;
    private bool collectionRequested;

    public int ExperienceAmount
    {
        get
        {
            return IsSpawned
                ? networkExperienceAmount.Value
                : experienceAmount;
        }
    }

    private void Update()
    {
        if (attractionTarget == null)
        {
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            attractionTarget.position,
            attractionSpeed * Time.deltaTime
        );
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            networkExperienceAmount.Value = experienceAmount;
        }
    }

    public void Initialize(int amount)
    {
        experienceAmount = Mathf.Max(1, amount);

        if (IsSpawned && IsServer)
        {
            networkExperienceAmount.Value = experienceAmount;
        }
    }

    public void AttractTo(Transform target)
    {
        if (target == null)
        {
            return;
        }

        attractionTarget = target;
    }

    public bool TryCollect(PlayerExperience collector)
    {
        if (!IsSpawned)
        {
            return false;
        }

        if (collectionRequested || collector == null)
        {
            return true;
        }

        NetworkObject collectorNetworkObject =
            collector.GetComponent<NetworkObject>();

        if (collectorNetworkObject == null ||
            !collectorNetworkObject.IsSpawned ||
            !collectorNetworkObject.IsOwner)
        {
            return true;
        }

        collectionRequested = true;

        if (IsServer)
        {
            CollectServer(
                collectorNetworkObject,
                collectorNetworkObject.OwnerClientId
            );
        }
        else
        {
            RequestCollectRpc(collectorNetworkObject);
        }

        return true;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestCollectRpc(
        NetworkObjectReference collectorReference,
        RpcParams rpcParams = default
    )
    {
        if (!collectorReference.TryGet(out NetworkObject collector))
        {
            return;
        }

        CollectServer(collector, rpcParams.Receive.SenderClientId);
    }

    private void CollectServer(NetworkObject collector, ulong senderClientId)
    {
        if (!IsServer || !IsSpawned || collector == null)
        {
            return;
        }

        if (collector.OwnerClientId != senderClientId)
        {
            return;
        }

        float distance = Vector2.Distance(
            transform.position,
            collector.transform.position
        );

        if (distance > collectionValidationDistance)
        {
            return;
        }

        NetworkPlayerExperience playerExperience =
            collector.GetComponent<NetworkPlayerExperience>();

        if (playerExperience == null)
        {
            return;
        }

        playerExperience.GrantExperienceServer(
            networkExperienceAmount.Value
        );
        NetworkObject.Despawn(true);
    }
}
