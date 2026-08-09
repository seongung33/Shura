using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(PlayerExperience))]
public class NetworkPlayerExperience : NetworkBehaviour
{
    private readonly NetworkVariable<int> currentLevel =
        new NetworkVariable<int>(1);

    private readonly NetworkVariable<int> currentExperience =
        new NetworkVariable<int>();

    private readonly NetworkVariable<int> experienceToNextLevel =
        new NetworkVariable<int>(10);

    private readonly NetworkVariable<int> pendingChoiceCount =
        new NetworkVariable<int>();

    private PlayerExperience playerExperience;

    public int PendingChoiceCount => pendingChoiceCount.Value;

    private void Awake()
    {
        playerExperience = GetComponent<PlayerExperience>();
    }

    public override void OnNetworkSpawn()
    {
        currentLevel.OnValueChanged += OnProgressChanged;
        currentExperience.OnValueChanged += OnProgressChanged;
        experienceToNextLevel.OnValueChanged += OnProgressChanged;
        pendingChoiceCount.OnValueChanged += OnProgressChanged;

        if (IsServer)
        {
            if (TeamExperience.Active != null)
            {
                TeamExperience.Active.Register(playerExperience);
            }
            else
            {
                currentLevel.Value = playerExperience.CurrentLevel;
                currentExperience.Value = playerExperience.CurrentExperience;
                experienceToNextLevel.Value =
                    playerExperience.ExperienceToNextLevel;
            }
        }

        ApplyStateToPlayer();
    }

    public override void OnNetworkDespawn()
    {
        currentLevel.OnValueChanged -= OnProgressChanged;
        currentExperience.OnValueChanged -= OnProgressChanged;
        experienceToNextLevel.OnValueChanged -= OnProgressChanged;
        pendingChoiceCount.OnValueChanged -= OnProgressChanged;
    }

    public void GrantExperienceServer(int amount)
    {
        if (!IsServer || amount <= 0)
        {
            return;
        }

        if (TeamExperience.Active != null)
        {
            TeamExperience.Active.GrantExperience(amount);
            return;
        }

        playerExperience.AddExperience(amount);
    }

    public void ResetProgressServer()
    {
        if (!IsServer)
        {
            return;
        }

        if (TeamExperience.Active != null)
        {
            TeamExperience.Active.ResetProgress();
            return;
        }

        ApplyTeamStateServer(1, 0, 10, 0, true);
    }

    public void ApplyTeamStateServer(
        int level,
        int experience,
        int nextLevelExperience,
        int gainedLevels,
        bool resetChoices = false
    )
    {
        if (!IsServer)
        {
            return;
        }

        currentLevel.Value = Mathf.Max(1, level);
        currentExperience.Value = Mathf.Max(0, experience);
        experienceToNextLevel.Value = Mathf.Max(1, nextLevelExperience);

        if (resetChoices)
        {
            pendingChoiceCount.Value = 0;
        }
        else if (gainedLevels > 0)
        {
            pendingChoiceCount.Value += gainedLevels;
        }

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
