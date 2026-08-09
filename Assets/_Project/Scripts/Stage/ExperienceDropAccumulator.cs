using Unity.Netcode;
using UnityEngine;

public sealed class ExperienceDropAccumulator : MonoBehaviour
{
    private ExperienceOrb orbPrefab;
    private float pendingExperience;
    private Vector3 latestPosition;

    public void SetOrbPrefab(ExperienceOrb configuredOrbPrefab)
    {
        if (configuredOrbPrefab != null)
        {
            orbPrefab = configuredOrbPrefab;
        }
    }

    public void Add(
        Vector3 position,
        float experience,
        int bundleSize
    )
    {
        if (experience <= 0f)
        {
            return;
        }

        latestPosition = position;
        pendingExperience += experience;

        int safeBundleSize = Mathf.Max(1, bundleSize);

        while (pendingExperience >= safeBundleSize)
        {
            pendingExperience -= safeBundleSize;
            SpawnOrb(latestPosition, safeBundleSize);
        }
    }

    public void Flush()
    {
        int amount = Mathf.FloorToInt(pendingExperience);

        if (amount <= 0)
        {
            return;
        }

        pendingExperience -= amount;
        SpawnOrb(latestPosition, amount);
    }

    private void SpawnOrb(Vector3 position, int amount)
    {
        if (orbPrefab == null)
        {
            TeamExperience.Active?.GrantExperience(amount);
            return;
        }

        ExperienceOrb orb = Instantiate(orbPrefab, position, Quaternion.identity);
        orb.Initialize(amount);

        if (!IsNetworkSessionRunning())
        {
            return;
        }

        NetworkObject networkObject = orb.GetComponent<NetworkObject>();

        if (networkObject == null)
        {
            Debug.LogError("ExperienceOrb에 NetworkObject가 없어 네트워크 생성할 수 없습니다.");
            Destroy(orb.gameObject);
            return;
        }

        networkObject.Spawn();
    }

    private static bool IsNetworkSessionRunning()
    {
        return NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening;
    }
}
