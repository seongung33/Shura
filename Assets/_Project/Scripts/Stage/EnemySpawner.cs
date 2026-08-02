using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private GameObject enemyPrefab;

    [SerializeField]
    private Transform player;

    [Header("Spawn Settings")]
    [SerializeField, Min(0.1f)]
    private float spawnInterval = 2f;

    [SerializeField, Min(0f)]
    private float minSpawnDistance = 7f;

    [SerializeField, Min(0f)]
    private float maxSpawnDistance = 10f;

    [SerializeField, Min(1)]
    private int maxAlive = 15;

    private readonly List<GameObject> spawnedEnemies = new();

    private float spawnTimer;
    private bool spawningEnabled = true;

    private void Start()
    {
        TryFindPlayer();
    }

    public void Configure(GameObject configuredEnemyPrefab)
    {
        enemyPrefab = configuredEnemyPrefab;
    }

    private void Update()
    {
        RemoveDestroyedEnemies();

        if (!HasSpawnAuthority())
        {
            return;
        }

        if (!spawningEnabled)
        {
            return;
        }

        if (player == null)
        {
            TryFindPlayer();
            return;
        }

        if (enemyPrefab == null ||
            spawnedEnemies.Count >= maxAlive)
        {
            return;
        }

        spawnTimer += Time.deltaTime;

        if (spawnTimer < spawnInterval)
        {
            return;
        }

        spawnTimer -= spawnInterval;
        SpawnEnemy();
    }

    public void ApplyWaveSettings(
        float newSpawnInterval,
        int newMaxAlive
    )
    {
        spawnInterval = Mathf.Max(
            0.1f,
            newSpawnInterval
        );

        maxAlive = Mathf.Max(
            1,
            newMaxAlive
        );
    }

    public void SetSpawningEnabled(bool enabled)
    {
        spawningEnabled = enabled;

        if (!enabled)
        {
            spawnTimer = 0f;
        }
    }

    private void TryFindPlayer()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private void SpawnEnemy()
    {
        float angle =
            Random.Range(0f, Mathf.PI * 2f);

        float distance =
            Random.Range(
                minSpawnDistance,
                maxSpawnDistance
            );

        Vector2 direction = new Vector2(
            Mathf.Cos(angle),
            Mathf.Sin(angle)
        );

        Vector3 spawnPosition =
            player.position +
            (Vector3)(direction * distance);

        GameObject enemy = Instantiate(
            enemyPrefab,
            spawnPosition,
            Quaternion.identity
        );

        if (IsNetworkSessionRunning())
        {
            NetworkObject networkObject =
                enemy.GetComponent<NetworkObject>();

            if (networkObject == null)
            {
                Debug.LogError(
                    $"{enemyPrefab.name}에 NetworkObject가 없어 네트워크 스폰할 수 없습니다."
                );

                Destroy(enemy);
                return;
            }

            networkObject.Spawn();
        }

        spawnedEnemies.Add(enemy);
    }

    private static bool HasSpawnAuthority()
    {
        return !IsNetworkSessionRunning() ||
            NetworkManager.Singleton.IsServer;
    }

    private static bool IsNetworkSessionRunning()
    {
        return NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening;
    }

    private void RemoveDestroyedEnemies()
    {
        spawnedEnemies.RemoveAll(
            enemy => enemy == null
        );
    }

    private void OnValidate()
    {
        if (maxSpawnDistance < minSpawnDistance)
        {
            maxSpawnDistance =
                minSpawnDistance;
        }
    }
}
