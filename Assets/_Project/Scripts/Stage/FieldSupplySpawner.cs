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

    private readonly HashSet<Vector2Int> visitedCells = new();
    private readonly List<Transform> activePlayers = new();
    private float nextPlayerCheckTime;

    public int VisitedCellCount => visitedCells.Count;

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

        foreach (Transform player in activePlayers)
        {
            VisitPlayerCell(player.position);
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

    private void VisitPlayerCell(Vector2 playerPosition)
    {
        Vector2Int cell = Vector2Int.FloorToInt(
            playerPosition / cellSize
        );

        if (!visitedCells.Add(cell))
        {
            return;
        }

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
            minimumPlayerSpawnDistance + 1f,
            cellSize * 0.45f
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
                    minimumPlayerSpawnDistance,
                    maximumDistance
                );

            if (IsFarEnoughFromPlayers(candidate))
            {
                spawnPosition = candidate;
                return true;
            }
        }

        spawnPosition = default;
        return false;
    }

    private bool IsFarEnoughFromPlayers(Vector2 candidate)
    {
        float minimumDistanceSquared =
            minimumPlayerSpawnDistance * minimumPlayerSpawnDistance;

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
    }
}
