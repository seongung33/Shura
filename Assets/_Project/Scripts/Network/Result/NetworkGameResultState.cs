using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum NetworkGameResult
{
    Playing,
    Victory,
    Defeat
}

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkPlayerHealth))]
public class NetworkGameResultState : NetworkBehaviour
{
    private static readonly HashSet<NetworkGameResultState> SpawnedStates =
        new HashSet<NetworkGameResultState>();

    private readonly NetworkVariable<NetworkGameResult> result =
        new NetworkVariable<NetworkGameResult>();

    private NetworkPlayerHealth playerHealth;
    private NetworkPlayerExperience playerExperience;

    public NetworkGameResult Result => result.Value;
    public bool HasFinished => result.Value != NetworkGameResult.Playing;

    public event Action<NetworkGameResult> ResultChanged;

    private void Awake()
    {
        playerHealth = GetComponent<NetworkPlayerHealth>();
        playerExperience = GetComponent<NetworkPlayerExperience>();
    }

    public override void OnNetworkSpawn()
    {
        SpawnedStates.Add(this);
        result.OnValueChanged += HandleResultChanged;

        if (IsServer)
        {
            result.Value = NetworkGameResult.Playing;
        }

        ResultChanged?.Invoke(result.Value);
    }

    public override void OnNetworkDespawn()
    {
        result.OnValueChanged -= HandleResultChanged;
        SpawnedStates.Remove(this);
    }

    private void Update()
    {
        if (!IsServer || HasFinished || !playerHealth.IsDead ||
            !AreAllSpawnedPlayersDead())
        {
            return;
        }

        SetResultForAllPlayers(NetworkGameResult.Defeat);
    }

    private static bool AreAllSpawnedPlayersDead()
    {
        bool foundPlayer = false;

        foreach (NetworkGameResultState state in SpawnedStates)
        {
            if (state == null || !state.IsSpawned || !state.IsServer)
            {
                continue;
            }

            foundPlayer = true;

            if (state.playerHealth == null || !state.playerHealth.IsDead)
            {
                return false;
            }
        }

        return foundPlayer;
    }

    public void SetVictoryServer()
    {
        if (!IsServer)
        {
            return;
        }

        SetResultForAllPlayers(NetworkGameResult.Victory);
    }

    public void ResetResultServer()
    {
        if (!IsServer)
        {
            return;
        }

        SetResultForAllPlayers(NetworkGameResult.Playing, true);
    }

    private static void SetResultForAllPlayers(
        NetworkGameResult newResult,
        bool overwriteFinished = false
    )
    {
        foreach (NetworkGameResultState state in SpawnedStates)
        {
            if (state != null &&
                state.IsServer &&
                (overwriteFinished || !state.HasFinished))
            {
                if (newResult == NetworkGameResult.Playing && overwriteFinished)
                {
                    state.playerHealth.ResetHealthServer();
                    state.playerExperience?.ResetProgressServer();
                }

                state.result.Value = newResult;
            }
        }
    }

    private void HandleResultChanged(
        NetworkGameResult previousResult,
        NetworkGameResult newResult
    )
    {
        ResultChanged?.Invoke(newResult);
    }
}
