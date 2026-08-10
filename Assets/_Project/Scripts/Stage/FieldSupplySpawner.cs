using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public sealed class FieldSupplySpawner : MonoBehaviour
{
    [SerializeField]
    private BreakableSupplyObject supplyPrefab;

    [SerializeField, Min(1f)]
    private float cellSize = 24f;

    [SerializeField, Range(0f, 1f)]
    private float objectSpawnChance = 0.25f;

    [SerializeField, Min(0f)]
    private float minimumPlayerSpawnDistance = 4f;

    [SerializeField, Min(1)]
    private int maxExistingSupplies = 6;

    [SerializeField, Min(0.05f)]
    private float playerCheckInterval = 0.25f;

    [SerializeField, Min(1f)]
    private float repeatAttemptIntervalMin = 8f;

    [SerializeField, Min(1f)]
    private float repeatAttemptIntervalMax = 14f;

    [SerializeField, Min(1f)]
    private float offscreenSpawnDistance = 10.5f;

    [SerializeField, Min(1f)]
    private float supplyCleanupDistance = 48f;

    private readonly Dictionary<Vector2Int, float> nextCellAttemptTimes = new();
    private readonly List<Transform> activePlayers = new();
    private float nextPlayerCheckTime;
    private float nextSupplyCleanupTime;

    public int VisitedCellCount => nextCellAttemptTimes.Count;

    public void Configure(StageConfig config)
    {
        if (config == null)
        {
            return;
        }

        supplyPrefab = config.FieldSupplyPrefab;
        cellSize = config.FieldSupplyCellSize;
        objectSpawnChance = config.FieldSupplySpawnChance;
        minimumPlayerSpawnDistance =
            config.FieldSupplyMinimumPlayerDistance;
        maxExistingSupplies = config.MaxExistingFieldSupplies;
    }

    private void Update()
    {
        if (Time.time < nextPlayerCheckTime ||
            !HasSpawnAuthority() ||
            supplyPrefab == null)
        {
            return;
        }

        nextPlayerCheckTime = Time.time + playerCheckInterval;
        CollectActivePlayers();

        if (Time.time >= nextSupplyCleanupTime)
        {
            nextSupplyCleanupTime = Time.time + 2f;
            CleanupDistantSupplies();
        }

        foreach (Transform player in activePlayers)
        {
            TrySpawnForPlayer(player.position);
        }
    }

    private void CollectActivePlayers()
    {
        activePlayers.Clear();
        NetworkManager manager = NetworkManager.Singleton;

        if (manager != null && manager.IsListening)
        {
            foreach (NetworkClient client in manager.ConnectedClientsList)
            {
                if (client.PlayerObject != null)
                {
                    activePlayers.Add(client.PlayerObject.transform);
                }
            }

            return;
        }

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject player in players)
        {
            activePlayers.Add(player.transform);
        }
    }

    private void TrySpawnForPlayer(Vector2 playerPosition)
    {
        Vector2Int cell = Vector2Int.FloorToInt(
            playerPosition / cellSize
        );

        if (nextCellAttemptTimes.TryGetValue(cell, out float nextAttemptTime) &&
            Time.time < nextAttemptTime)
        {
            return;
        }

        nextCellAttemptTimes[cell] = Time.time + Random.Range(
            repeatAttemptIntervalMin,
            repeatAttemptIntervalMax
        );

        bool spawnRollSucceeded = objectSpawnChance >= 1f ||
            (objectSpawnChance > 0f &&
                Random.value < objectSpawnChance);

        if (!spawnRollSucceeded ||
            CountExistingSupplies() >= maxExistingSupplies ||
            !TryFindSpawnPosition(playerPosition, out Vector2 spawnPosition))
        {
            return;
        }

        SpawnSupply(spawnPosition);
    }

    private bool TryFindSpawnPosition(
        Vector2 playerPosition,
        out Vector2 spawnPosition
    )
    {
        float maximumDistance = Mathf.Max(
            offscreenSpawnDistance + 2f,
            cellSize * 0.7f
        );
        float minimumDistance = Mathf.Max(
            minimumPlayerSpawnDistance,
            offscreenSpawnDistance
        );

        for (int attempt = 0; attempt < 12; attempt++)
        {
            Vector2 direction = Random.insideUnitCircle;

            if (direction.sqrMagnitude < 0.001f)
            {
                direction = Vector2.right;
            }

            Vector2 candidate = playerPosition + direction.normalized *
                Random.Range(
                    minimumDistance,
                    maximumDistance
                );

            if (IsFarEnoughFromPlayers(candidate, minimumDistance) &&
                !IsVisibleToAnyCamera(candidate))
            {
                spawnPosition = candidate;
                return true;
            }
        }

        spawnPosition = default;
        return false;
    }

    private bool IsFarEnoughFromPlayers(
        Vector2 candidate,
        float requiredDistance
    )
    {
        float minimumDistanceSquared =
            requiredDistance * requiredDistance;

        foreach (Transform player in activePlayers)
        {
            if (player != null &&
                ((Vector2)player.position - candidate).sqrMagnitude <
                    minimumDistanceSquared)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsVisibleToAnyCamera(Vector2 candidate)
    {
        foreach (Camera camera in Camera.allCameras)
        {
            if (camera == null || !camera.isActiveAndEnabled)
            {
                continue;
            }

            Vector3 viewport = camera.WorldToViewportPoint(candidate);
            if (viewport.z > 0f &&
                viewport.x >= -0.05f && viewport.x <= 1.05f &&
                viewport.y >= -0.05f && viewport.y <= 1.05f)
            {
                return true;
            }
        }

        return false;
    }

    private void SpawnSupply(Vector2 position)
    {
        BreakableSupplyObject supply = Instantiate(
            supplyPrefab,
            position,
            Quaternion.identity
        );
        NetworkManager manager = NetworkManager.Singleton;

        if (manager == null || !manager.IsListening)
        {
            return;
        }

        NetworkObject supplyNetworkObject =
            supply.GetComponent<NetworkObject>();

        if (supplyNetworkObject == null)
        {
            Destroy(supply.gameObject);
            return;
        }

        supplyNetworkObject.Spawn();
    }

    private static int CountExistingSupplies()
    {
        return FindObjectsByType<BreakableSupplyObject>(
            FindObjectsSortMode.None
        ).Length;
    }

    private void CleanupDistantSupplies()
    {
        float cleanupDistanceSquared =
            supplyCleanupDistance * supplyCleanupDistance;
        BreakableSupplyObject[] supplies =
            FindObjectsByType<BreakableSupplyObject>(
                FindObjectsSortMode.None
            );

        foreach (BreakableSupplyObject supply in supplies)
        {
            if (supply == null ||
                IsNearAnyPlayer(supply.transform.position, cleanupDistanceSquared))
            {
                continue;
            }

            NetworkObject networkObject = supply.GetComponent<NetworkObject>();

            if (networkObject != null && networkObject.IsSpawned)
            {
                networkObject.Despawn(true);
            }
            else
            {
                Destroy(supply.gameObject);
            }
        }
    }

    private bool IsNearAnyPlayer(
        Vector2 position,
        float maximumDistanceSquared
    )
    {
        foreach (Transform player in activePlayers)
        {
            if (player != null &&
                ((Vector2)player.position - position).sqrMagnitude <=
                    maximumDistanceSquared)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasSpawnAuthority()
    {
        NetworkManager manager = NetworkManager.Singleton;
        return manager == null ||
            !manager.IsListening ||
            manager.IsServer;
    }

    private void OnValidate()
    {
        cellSize = Mathf.Max(1f, cellSize);
        objectSpawnChance = Mathf.Clamp01(objectSpawnChance);
        minimumPlayerSpawnDistance = Mathf.Max(
            0f,
            minimumPlayerSpawnDistance
        );
        maxExistingSupplies = Mathf.Max(1, maxExistingSupplies);
        playerCheckInterval = Mathf.Max(0.05f, playerCheckInterval);
        repeatAttemptIntervalMin = Mathf.Max(1f, repeatAttemptIntervalMin);
        repeatAttemptIntervalMax = Mathf.Max(
            repeatAttemptIntervalMin,
            repeatAttemptIntervalMax
        );
        offscreenSpawnDistance = Mathf.Max(
            minimumPlayerSpawnDistance,
            offscreenSpawnDistance
        );
        supplyCleanupDistance = Mathf.Max(
            offscreenSpawnDistance + 1f,
            supplyCleanupDistance
        );
    }
}
