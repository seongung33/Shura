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
    private float attractionRadius = 2f;

    [SerializeField, Min(0.1f)]
    private float automaticCollectionDistance = 0.9f;

    [SerializeField, Min(0.1f)]
    private float collectionValidationDistance = 2f;

    private readonly NetworkVariable<int> networkExperienceAmount =
        new NetworkVariable<int>(1);

    private readonly NetworkVariable<NetworkObjectReference>
        networkAttractionTarget = new();

    private Transform attractionTarget;
    private bool collectionRequested;
    private float nextTargetSearchTime;

    private static readonly System.Collections.Generic.List<Transform>
        PlayerTargets = new();
    private static float nextPlayerCacheRefreshTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPlayerCache()
    {
        PlayerTargets.Clear();
        nextPlayerCacheRefreshTime = 0f;
    }

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
        bool hasCollectionAuthority = !IsSpawned || IsServer;

        if (hasCollectionAuthority && attractionTarget == null &&
            Time.time >= nextTargetSearchTime)
        {
            nextTargetSearchTime = Time.time + 0.2f;
            attractionTarget = FindNearestPlayer();

            if (IsSpawned && attractionTarget != null)
            {
                NetworkObject targetNetworkObject =
                    attractionTarget.GetComponent<NetworkObject>();

                if (targetNetworkObject != null && targetNetworkObject.IsSpawned)
                {
                    networkAttractionTarget.Value = targetNetworkObject;
                }
            }
        }

        if (attractionTarget == null)
        {
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            attractionTarget.position,
            attractionSpeed * Time.deltaTime
        );

        if (hasCollectionAuthority &&
            Vector2.Distance(transform.position, attractionTarget.position) <=
            automaticCollectionDistance)
        {
            CollectAutomatically();
        }
    }

    private Transform FindNearestPlayer()
    {
        RefreshPlayerTargets();
        Transform nearest = null;
        float nearestDistanceSquared = attractionRadius * attractionRadius;
        Vector2 position = transform.position;

        foreach (Transform candidate in PlayerTargets)
        {
            if (candidate == null)
            {
                continue;
            }

            float distanceSquared = ((Vector2)candidate.position - position)
                .sqrMagnitude;

            if (distanceSquared <= nearestDistanceSquared)
            {
                nearest = candidate;
                nearestDistanceSquared = distanceSquared;
            }
        }

        return nearest;
    }

    private static void RefreshPlayerTargets()
    {
        if (Time.time < nextPlayerCacheRefreshTime)
        {
            return;
        }

        nextPlayerCacheRefreshTime = Time.time + 0.5f;
        PlayerTargets.Clear();

        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (player.GetComponent<PlayerExperience>() != null)
            {
                PlayerTargets.Add(player.transform);
            }
        }
    }

    private void CollectAutomatically()
    {
        if (collectionRequested || attractionTarget == null)
        {
            return;
        }

        collectionRequested = true;

        if (IsSpawned)
        {
            NetworkPlayerExperience networkExperience =
                attractionTarget.GetComponent<NetworkPlayerExperience>();

            if (networkExperience == null)
            {
                collectionRequested = false;
                attractionTarget = null;
                return;
            }

            networkExperience.GrantExperienceServer(networkExperienceAmount.Value);
            NetworkObject.Despawn(true);
            return;
        }

        PlayerExperience playerExperience =
            attractionTarget.GetComponent<PlayerExperience>();

        if (playerExperience == null)
        {
            collectionRequested = false;
            attractionTarget = null;
            return;
        }

        playerExperience.AddExperience(experienceAmount);
        Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        networkAttractionTarget.OnValueChanged +=
            OnAttractionTargetChanged;

        if (IsServer)
        {
            networkExperienceAmount.Value = experienceAmount;
        }

        ApplyNetworkAttractionTarget(networkAttractionTarget.Value);
    }

    public override void OnNetworkDespawn()
    {
        networkAttractionTarget.OnValueChanged -=
            OnAttractionTargetChanged;
        attractionTarget = null;
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

        if (!IsSpawned)
        {
            attractionTarget = target;
            return;
        }

        if (!IsServer)
        {
            attractionTarget = target;
            return;
        }

        NetworkObject targetNetworkObject =
            target.GetComponent<NetworkObject>();

        if (targetNetworkObject == null ||
            !targetNetworkObject.IsSpawned)
        {
            return;
        }

        networkAttractionTarget.Value = targetNetworkObject;
        attractionTarget = target;
    }

    private void OnAttractionTargetChanged(
        NetworkObjectReference previousValue,
        NetworkObjectReference newValue
    )
    {
        ApplyNetworkAttractionTarget(newValue);
    }

    private void ApplyNetworkAttractionTarget(
        NetworkObjectReference targetReference
    )
    {
        attractionTarget = targetReference.TryGet(
            out NetworkObject targetNetworkObject
        )
            ? targetNetworkObject.transform
            : null;
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
