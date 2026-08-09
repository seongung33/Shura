using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public sealed class TeamLevelUpCoordinator : MonoBehaviour
{
    private readonly List<NetworkPlayerProgression> players = new();

    private TeamExperience teamExperience;
    private LevelUpSettings settings = new();
    private bool sessionActive;
    private int activeTeamLevel;
    private int activeSessionId;
    private double activeDeadline;

    public static TeamLevelUpCoordinator Active { get; private set; }

    private void Awake()
    {
        Active = this;
    }

    public void Configure(
        TeamExperience configuredTeamExperience,
        LevelUpSettings configuredSettings
    )
    {
        Unsubscribe();

        teamExperience = configuredTeamExperience;
        settings = configuredSettings ?? new LevelUpSettings();

        if (teamExperience != null)
        {
            teamExperience.LevelUpChoiceRequested += HandleLevelUpRequested;
            teamExperience.ProgressReset += HandleProgressReset;
        }

        RegisterExistingPlayers();
    }

    private void OnDestroy()
    {
        Unsubscribe();

        if (Active == this)
        {
            Active = null;
        }
    }

    private void Update()
    {
        if (!sessionActive || !IsServerSession())
        {
            return;
        }

        RemoveMissingPlayers();

        if (GetServerTime() >= activeDeadline)
        {
            int expiredSessionId = activeSessionId;
            NetworkPlayerProgression[] snapshot = players.ToArray();

            foreach (NetworkPlayerProgression player in snapshot)
            {
                if (sessionActive &&
                    activeSessionId == expiredSessionId &&
                    player != null &&
                    !player.HasSelected)
                {
                    player.AutoSelectServer();
                }
            }
        }

        TryCompleteSessionServer();
    }

    public void Register(NetworkPlayerProgression player)
    {
        if (player == null || players.Contains(player))
        {
            return;
        }

        players.Add(player);
        player.ConfigureSettings(settings);

        if (!IsServerSession())
        {
            return;
        }

        if (sessionActive && player.CanParticipateInLevelUp)
        {
            player.BeginChoiceServer(
                activeTeamLevel,
                activeSessionId,
                activeDeadline,
                settings.IsSkillChoiceLevel(activeTeamLevel)
            );
        }
        else if (!sessionActive)
        {
            player.ResetProgressionServer();
        }
    }

    public void Unregister(NetworkPlayerProgression player)
    {
        players.Remove(player);

        if (sessionActive && IsServerSession())
        {
            TryCompleteSessionServer();
        }
    }

    public void NotifyChoiceAppliedServer(NetworkPlayerProgression player)
    {
        if (!sessionActive || !IsServerSession() || !players.Contains(player))
        {
            return;
        }

        TryCompleteSessionServer();
    }

    private bool HandleLevelUpRequested(int teamLevel)
    {
        if (!IsServerSession() || sessionActive)
        {
            return false;
        }

        RemoveMissingPlayers();

        if (players.Count == 0)
        {
            RegisterExistingPlayers();
        }

        if (players.Count == 0)
        {
            return false;
        }

        foreach (NetworkPlayerProgression player in players)
        {
            if (!player.CanParticipateInLevelUp)
            {
                Debug.LogWarning(
                    "레벨업 후보를 만들 수 없는 네트워크 플레이어가 있어 선택 단계를 건너뜁니다."
                );
                return false;
            }
        }

        sessionActive = true;
        activeTeamLevel = teamLevel;
        activeSessionId++;
        activeDeadline = GetServerTime() + settings.ChoiceDuration;
        bool isSkillChoice = settings.IsSkillChoiceLevel(teamLevel);
        List<NetworkPlayerProgression> startedPlayers = new();

        foreach (NetworkPlayerProgression player in players)
        {
            if (!player.BeginChoiceServer(
                    teamLevel,
                    activeSessionId,
                    activeDeadline,
                    isSkillChoice
                ))
            {
                foreach (NetworkPlayerProgression started in startedPlayers)
                {
                    started.EndChoiceServer();
                }

                sessionActive = false;
                return false;
            }

            startedPlayers.Add(player);
        }

        return true;
    }

    private void TryCompleteSessionServer()
    {
        if (!sessionActive)
        {
            return;
        }

        foreach (NetworkPlayerProgression player in players)
        {
            if (player != null && !player.HasSelected)
            {
                return;
            }
        }

        int completedLevel = activeTeamLevel;

        foreach (NetworkPlayerProgression player in players)
        {
            player?.CompletePendingChoiceServer();
        }

        sessionActive = false;
        teamExperience?.CompleteLevelUpChoice(completedLevel);

        // CompleteLevelUpChoice가 누적 EXP의 다음 레벨 선택을 즉시 시작할 수 있다.
        if (sessionActive)
        {
            return;
        }

        foreach (NetworkPlayerProgression player in players)
        {
            player?.EndChoiceServer();
        }
    }

    private void HandleProgressReset()
    {
        if (!IsServerSession())
        {
            return;
        }

        sessionActive = false;

        foreach (NetworkPlayerProgression player in players)
        {
            if (player == null)
            {
                continue;
            }

            player.ResetProgressionServer();
            player.EndChoiceServer();
        }
    }

    private void RegisterExistingPlayers()
    {
        NetworkPlayerProgression[] existing =
            FindObjectsByType<NetworkPlayerProgression>(
                FindObjectsSortMode.None
            );

        foreach (NetworkPlayerProgression player in existing)
        {
            Register(player);
        }
    }

    private void RemoveMissingPlayers()
    {
        players.RemoveAll(player => player == null || !player.IsSpawned);
    }

    private void Unsubscribe()
    {
        if (teamExperience == null)
        {
            return;
        }

        teamExperience.LevelUpChoiceRequested -= HandleLevelUpRequested;
        teamExperience.ProgressReset -= HandleProgressReset;
    }

    private static bool IsServerSession()
    {
        NetworkManager manager = NetworkManager.Singleton;
        return manager != null && manager.IsListening && manager.IsServer;
    }

    private static double GetServerTime()
    {
        NetworkManager manager = NetworkManager.Singleton;
        return manager != null && manager.IsListening
            ? manager.ServerTime.Time
            : Time.unscaledTimeAsDouble;
    }
}
