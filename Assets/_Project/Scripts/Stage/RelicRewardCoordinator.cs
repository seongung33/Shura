using Unity.Netcode;
using UnityEngine;

public sealed class RelicRewardCoordinator : MonoBehaviour
{
    [SerializeField, Min(1f)]
    private float offlineChoiceDuration = 15f;

    public static RelicRewardCoordinator Active { get; private set; }

    private void Awake()
    {
        Active = this;
    }

    private void OnDestroy()
    {
        if (Active == this)
        {
            Active = null;
        }
    }

    public void BeginEliteReward()
    {
        NetworkManager manager = NetworkManager.Singleton;
        bool networked = manager != null && manager.IsListening;

        if (networked && !manager.IsServer)
        {
            return;
        }

        foreach (PlayerRelicInventory inventory in
            PlayerRelicInventory.GetActiveSnapshot())
        {
            if (inventory == null)
            {
                continue;
            }

            if (networked)
            {
                NetworkPlayerRelics networkRelics =
                    inventory.GetComponent<NetworkPlayerRelics>();

                if (networkRelics != null && networkRelics.IsSpawned)
                {
                    networkRelics.BeginChoiceServer();
                }

                continue;
            }

            inventory.BeginLocalChoice(offlineChoiceDuration);
        }
    }
}
