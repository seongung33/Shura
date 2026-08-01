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

    public NetworkGameResult Result => result.Value;
    public bool HasFinished => result.Value != NetworkGameResult.Playing;

    public event Action<NetworkGameResult> ResultChanged;

    private void Awake()
    {
        playerHealth = GetComponent<NetworkPlayerHealth>();
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
        if (!IsServer || HasFinished || !playerHealth.IsDead)
        {
            return;
        }

        SetResultForAllPlayers(NetworkGameResult.Defeat);
    }

    public void SetVictoryServer()
    {
        if (!IsServer)
        {
            return;
        }

        SetResultForAllPlayers(NetworkGameResult.Victory);
    }

    private static void SetResultForAllPlayers(NetworkGameResult newResult)
    {
        foreach (NetworkGameResultState state in SpawnedStates)
        {
            if (state != null && state.IsServer && !state.HasFinished)
            {
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
