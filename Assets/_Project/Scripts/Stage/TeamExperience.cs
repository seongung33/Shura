using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public sealed class TeamExperience : MonoBehaviour
{
    private readonly List<PlayerExperience> players = new();

    private LevelCurveData levelCurve;
    private int currentLevel = 1;
    private int currentExperience;
    private int experienceToNextLevel = 10;

    public static TeamExperience Active { get; private set; }

    public int CurrentLevel => currentLevel;
    public int CurrentExperience => currentExperience;
    public int ExperienceToNextLevel => experienceToNextLevel;

    public void Configure(LevelCurveData configuredLevelCurve)
    {
        levelCurve = configuredLevelCurve;
        experienceToNextLevel = GetNextLevelExperience(currentLevel);
        RegisterExistingPlayers();
        PublishState(0);
    }

    private void Awake()
    {
        Active = this;
    }

    private void Start()
    {
        if (levelCurve != null)
        {
            Configure(levelCurve);
        }
    }

    private void OnDestroy()
    {
        if (Active == this)
        {
            Active = null;
        }
    }

    public void Register(PlayerExperience player)
    {
        if (player == null || players.Contains(player))
        {
            return;
        }

        players.Add(player);
        ApplyState(player, 0);
    }

    public void Unregister(PlayerExperience player)
    {
        players.Remove(player);
    }

    public void GrantExperience(int amount)
    {
        if (amount <= 0 || !HasServerAuthority())
        {
            return;
        }

        int previousLevel = currentLevel;
        currentExperience += amount;

        while (currentExperience >= experienceToNextLevel)
        {
            currentExperience -= experienceToNextLevel;
            currentLevel++;
            experienceToNextLevel = GetNextLevelExperience(currentLevel);
        }

        PublishState(currentLevel - previousLevel);
    }

    public void ResetProgress()
    {
        if (!HasServerAuthority())
        {
            return;
        }

        currentLevel = 1;
        currentExperience = 0;
        experienceToNextLevel = GetNextLevelExperience(currentLevel);
        PublishState(0, true);
    }

    private int GetNextLevelExperience(int level)
    {
        if (levelCurve != null)
        {
            return levelCurve.GetTeamNextExperience(level);
        }

        return Mathf.Max(1, 10 + (level - 1) * 5);
    }

    private void RegisterExistingPlayers()
    {
        PlayerExperience[] existingPlayers =
            FindObjectsByType<PlayerExperience>(FindObjectsSortMode.None);

        foreach (PlayerExperience player in existingPlayers)
        {
            Register(player);
        }
    }

    private void PublishState(int gainedLevels, bool resetChoices = false)
    {
        players.RemoveAll(player => player == null);

        if (players.Count == 0)
        {
            RegisterExistingPlayers();
        }

        foreach (PlayerExperience player in players)
        {
            ApplyState(player, gainedLevels, resetChoices);
        }
    }

    private void ApplyState(
        PlayerExperience player,
        int gainedLevels,
        bool resetChoices = false
    )
    {
        NetworkPlayerExperience networkExperience =
            player.GetComponent<NetworkPlayerExperience>();

        if (networkExperience != null && networkExperience.IsSpawned)
        {
            networkExperience.ApplyTeamStateServer(
                currentLevel,
                currentExperience,
                experienceToNextLevel,
                gainedLevels,
                resetChoices
            );
            return;
        }

        player.ApplyNetworkState(
            currentLevel,
            currentExperience,
            experienceToNextLevel
        );
    }

    private static bool HasServerAuthority()
    {
        return NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsListening ||
            NetworkManager.Singleton.IsServer;
    }
}
