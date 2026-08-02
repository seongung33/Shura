using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(PlayerExperience))]
public class NetworkPlayerExperience : NetworkBehaviour
{
    [SerializeField]
    private int experienceGrowthPerLevel = 5;

    private readonly NetworkVariable<int> currentLevel =
        new NetworkVariable<int>(1);

    private readonly NetworkVariable<int> currentExperience =
        new NetworkVariable<int>();

    private readonly NetworkVariable<int> experienceToNextLevel =
        new NetworkVariable<int>(10);

    private PlayerExperience playerExperience;

    private void Awake()
    {
        playerExperience = GetComponent<PlayerExperience>();
    }

    public override void OnNetworkSpawn()
    {
        currentLevel.OnValueChanged += OnProgressChanged;
        currentExperience.OnValueChanged += OnProgressChanged;
        experienceToNextLevel.OnValueChanged += OnProgressChanged;

        if (IsServer)
        {
            currentLevel.Value = playerExperience.CurrentLevel;
            currentExperience.Value = playerExperience.CurrentExperience;
            experienceToNextLevel.Value =
                playerExperience.ExperienceToNextLevel;
        }

        ApplyStateToPlayer();
    }

    public override void OnNetworkDespawn()
    {
        currentLevel.OnValueChanged -= OnProgressChanged;
        currentExperience.OnValueChanged -= OnProgressChanged;
        experienceToNextLevel.OnValueChanged -= OnProgressChanged;
    }

    public void GrantExperienceServer(int amount)
    {
        if (!IsServer || amount <= 0)
        {
            return;
        }

        int nextExperience = currentExperience.Value + amount;
        int nextLevel = currentLevel.Value;
        int nextThreshold = experienceToNextLevel.Value;

        while (nextExperience >= nextThreshold)
        {
            nextExperience -= nextThreshold;
            nextLevel++;
            nextThreshold += experienceGrowthPerLevel;
        }

        currentExperience.Value = nextExperience;
        currentLevel.Value = nextLevel;
        experienceToNextLevel.Value = nextThreshold;

        ApplyStateToPlayer();
    }

    public void ResetProgressServer()
    {
        if (!IsServer)
        {
            return;
        }

        currentLevel.Value = 1;
        currentExperience.Value = 0;
        experienceToNextLevel.Value = 10;
        ApplyStateToPlayer();
    }

    private void OnProgressChanged(int previousValue, int newValue)
    {
        ApplyStateToPlayer();
    }

    private void ApplyStateToPlayer()
    {
        playerExperience.ApplyNetworkState(
            currentLevel.Value,
            currentExperience.Value,
            experienceToNextLevel.Value
        );
    }
}
