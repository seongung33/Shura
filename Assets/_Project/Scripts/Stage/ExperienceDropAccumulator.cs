using Unity.Netcode;
using UnityEngine;

public sealed class ExperienceDropAccumulator : MonoBehaviour
{
    private ExperienceOrb orbPrefab;

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
        int amount = float.IsNaN(experience) || float.IsInfinity(experience)
            ? 1
            : Mathf.Max(1, Mathf.RoundToInt(experience));
        SpawnOrb(position, amount);
    }

    public void Flush()
    {
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
