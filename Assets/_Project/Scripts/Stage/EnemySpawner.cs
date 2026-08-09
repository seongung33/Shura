using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    private sealed class PlayerTarget
    {
        public ulong ClientId;
        public Transform Transform;
        public int AssignedCount;
    }

    private sealed class SpawnedEnemy
    {
        public GameObject GameObject;
        public ulong AssignedClientId;
    }

    [Header("Legacy Fallback")]
    [SerializeField]
    private GameObject enemyPrefab;

    [SerializeField]
    private Transform player;

    [SerializeField, Min(0.1f)]
    private float spawnInterval = 2f;

    [SerializeField, Min(0f)]
    private float minSpawnDistance = 8f;

    [SerializeField, Min(0f)]
    private float maxSpawnDistance = 12f;

    [SerializeField, Min(1)]
    private int maxAlive = 300;

    private readonly List<PlayerTarget> playerTargets = new();
    private readonly List<SpawnedEnemy> spawnedEnemies = new();

    private StageConfig stageConfig;
    private WaveSegment currentSegment;
    private ExperienceDropAccumulator dropAccumulator;
    private float elapsedTime;
    private float spawnBudget;
    private float playerRefreshTimer;
    private int targetAlive;
    private bool spawningEnabled = true;

    public int AliveCount => spawnedEnemies.Count;
    public int TargetAlive => targetAlive;

    private void Start()
    {
        dropAccumulator = GetComponent<ExperienceDropAccumulator>();

        if (dropAccumulator == null)
        {
            dropAccumulator = gameObject.AddComponent<ExperienceDropAccumulator>();
        }

        RefreshPlayers();
    }

    public void Configure(GameObject configuredEnemyPrefab)
    {
        enemyPrefab = configuredEnemyPrefab;
    }

    public void Configure(StageConfig configuredStage)
    {
        stageConfig = configuredStage;

        if (stageConfig == null)
        {
            return;
        }

        minSpawnDistance = stageConfig.SpawnRadiusMin;
        maxSpawnDistance = stageConfig.SpawnRadiusMax;
        maxAlive = stageConfig.MaxAlive;
    }

    private void Update()
    {
        RemoveDestroyedEnemies();

        if (GameplayPauseState.IsLevelUpActive)
        {
            return;
        }

        if (!HasSpawnAuthority())
        {
            return;
        }

        playerRefreshTimer -= Time.deltaTime;

        if (playerRefreshTimer <= 0f)
        {
            playerRefreshTimer = 0.5f;
            RefreshPlayers();
        }

        if (!spawningEnabled || playerTargets.Count == 0)
        {
            return;
        }

        float budgetPerSecond = currentSegment != null
            ? currentSegment.SpawnBudgetPerMinute / 60f
            : 1f / Mathf.Max(0.1f, spawnInterval);

        spawnBudget = Mathf.Min(spawnBudget + budgetPerSecond * Time.deltaTime, 12f);

        int spawnedThisFrame = 0;

        while (spawnBudget >= 1f &&
               spawnedEnemies.Count < targetAlive &&
               spawnedEnemies.Count < maxAlive &&
               spawnedThisFrame < 8)
        {
            if (!SpawnEnemy())
            {
                break;
            }

            spawnBudget -= 1f;
            spawnedThisFrame++;
        }
    }

    public void ApplyStageState(
        WaveSegment segment,
        float stageElapsedTime
    )
    {
        currentSegment = segment;
        elapsedTime = stageElapsedTime;
        targetAlive = segment != null
            ? Mathf.Min(maxAlive, segment.GetTargetAlive(stageElapsedTime))
            : 0;
    }

    public void ApplyWaveSettings(float newSpawnInterval, int newMaxAlive)
    {
        currentSegment = null;
        spawnInterval = Mathf.Max(0.1f, newSpawnInterval);
        maxAlive = Mathf.Max(1, newMaxAlive);
        targetAlive = maxAlive;
    }

    public void SetSpawningEnabled(bool enabled)
    {
        spawningEnabled = enabled;

        if (!enabled)
        {
            spawnBudget = 0f;
            dropAccumulator?.Flush();
        }
    }

    public void DespawnAllEnemies()
    {
        if (!HasSpawnAuthority())
        {
            return;
        }

        for (int index = spawnedEnemies.Count - 1; index >= 0; index--)
        {
            GameObject enemy = spawnedEnemies[index].GameObject;

            if (enemy == null)
            {
                continue;
            }

            NetworkObject networkObject = enemy.GetComponent<NetworkObject>();

            if (networkObject != null && networkObject.IsSpawned)
            {
                networkObject.Despawn(true);
            }
            else
            {
                Destroy(enemy);
            }
        }

        spawnedEnemies.Clear();
        ResetAssignedCounts();
    }

    private bool SpawnEnemy()
    {
        PlayerTarget assignedTarget = SelectTarget();
        EnemySpawnEntry entry = SelectEntry();
        GameObject prefab = entry != null ? entry.Prefab : enemyPrefab;

        if (assignedTarget == null || assignedTarget.Transform == null || prefab == null)
        {
            return false;
        }

        float angle = Random.Range(0f, Mathf.PI * 2f);
        float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        Vector3 spawnPosition = assignedTarget.Transform.position +
            (Vector3)(direction * distance);

        GameObject enemy = Instantiate(prefab, spawnPosition, Quaternion.identity);
        ConfigureEnemy(enemy, entry, assignedTarget);

        if (IsNetworkSessionRunning())
        {
            NetworkObject networkObject = enemy.GetComponent<NetworkObject>();

            if (networkObject == null)
            {
                Debug.LogError($"{prefab.name}에 NetworkObject가 없어 네트워크 스폰할 수 없습니다.");
                Destroy(enemy);
                return false;
            }

            networkObject.Spawn();
        }

        assignedTarget.AssignedCount++;
        spawnedEnemies.Add(new SpawnedEnemy
        {
            GameObject = enemy,
            AssignedClientId = assignedTarget.ClientId
        });
        return true;
    }

    private void ConfigureEnemy(
        GameObject enemy,
        EnemySpawnEntry entry,
        PlayerTarget target
    )
    {
        float healthScale = currentSegment?.HealthMultiplier ?? 1f;
        float damageScale = currentSegment?.DamageMultiplier ?? 1f;
        float moveScale = 1f;
        float experienceReward = 1f;
        int maxAttackers = currentSegment?.MaxAttackersPerPlayer ?? 5;

        if (entry != null)
        {
            healthScale *= entry.HealthMultiplier;
            damageScale *= entry.DamageMultiplier;
            moveScale = entry.MoveSpeedMultiplier;
            experienceReward = CalculateExperienceReward(entry);
        }

        EnemyController controller = enemy.GetComponent<EnemyController>();
        controller?.ConfigureRuntime(target.Transform, target.ClientId, moveScale);

        EnemyAttack attack = enemy.GetComponent<EnemyAttack>();
        attack?.ConfigureRuntime(damageScale, maxAttackers);

        EnemyHealth health = enemy.GetComponent<EnemyHealth>();

        if (health != null)
        {
            dropAccumulator.SetOrbPrefab(health.ExperienceOrbPrefab);
            health.ConfigureRuntime(
                healthScale,
                experienceReward,
                dropAccumulator,
                currentSegment?.ExperienceOrbBundleSize ?? 3
            );
        }
    }

    private float CalculateExperienceReward(EnemySpawnEntry entry)
    {
        if (currentSegment == null || currentSegment.SpawnBudgetPerMinute <= 0f)
        {
            return entry.ExperienceWeight;
        }

        float weightedExperience = 0f;
        float totalWeight = 0f;

        foreach (EnemySpawnEntry candidate in currentSegment.Enemies)
        {
            if (candidate == null || candidate.Prefab == null)
            {
                continue;
            }

            weightedExperience += candidate.Weight * candidate.ExperienceWeight;
            totalWeight += candidate.Weight;
        }

        float averageExperience = totalWeight > 0f
            ? weightedExperience / totalWeight
            : 1f;
        float scale = currentSegment.TeamExperiencePerMinute /
            (currentSegment.SpawnBudgetPerMinute * Mathf.Max(0.01f, averageExperience));
        return entry.ExperienceWeight * scale;
    }

    private EnemySpawnEntry SelectEntry()
    {
        if (currentSegment == null)
        {
            return null;
        }

        float totalWeight = 0f;

        foreach (EnemySpawnEntry entry in currentSegment.Enemies)
        {
            if (entry != null && entry.Prefab != null)
            {
                totalWeight += entry.Weight;
            }
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float roll = Random.value * totalWeight;

        foreach (EnemySpawnEntry entry in currentSegment.Enemies)
        {
            if (entry == null || entry.Prefab == null)
            {
                continue;
            }

            roll -= entry.Weight;

            if (roll <= 0f)
            {
                return entry;
            }
        }

        return currentSegment.Enemies[currentSegment.Enemies.Length - 1];
    }

    private PlayerTarget SelectTarget()
    {
        PlayerTarget selected = null;
        float lowestLoad = float.MaxValue;

        foreach (PlayerTarget candidate in playerTargets)
        {
            if (candidate.Transform == null)
            {
                continue;
            }

            float load = candidate.AssignedCount + Random.value * 0.1f;

            if (load < lowestLoad)
            {
                selected = candidate;
                lowestLoad = load;
            }
        }

        return selected;
    }

    private void RefreshPlayers()
    {
        Dictionary<ulong, Transform> found = new();

        if (IsNetworkSessionRunning())
        {
            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject != null)
                {
                    found[client.ClientId] = client.PlayerObject.transform;
                }
            }
        }
        else
        {
            if (player != null)
            {
                found[0] = player;
            }
            else
            {
                GameObject[] localPlayers = GameObject.FindGameObjectsWithTag("Player");

                for (int index = 0; index < localPlayers.Length; index++)
                {
                    found[(ulong)index] = localPlayers[index].transform;
                }
            }
        }

        foreach (KeyValuePair<ulong, Transform> pair in found)
        {
            PlayerTarget existing = playerTargets.Find(target => target.ClientId == pair.Key);

            if (existing == null)
            {
                playerTargets.Add(new PlayerTarget
                {
                    ClientId = pair.Key,
                    Transform = pair.Value
                });
            }
            else
            {
                existing.Transform = pair.Value;
            }
        }

        playerTargets.RemoveAll(target => !found.ContainsKey(target.ClientId));
        RecountAssignments();
    }

    private void RemoveDestroyedEnemies()
    {
        bool removedAny = spawnedEnemies.RemoveAll(enemy => enemy.GameObject == null) > 0;

        if (removedAny)
        {
            RecountAssignments();
        }
    }

    private void RecountAssignments()
    {
        ResetAssignedCounts();

        foreach (SpawnedEnemy enemy in spawnedEnemies)
        {
            PlayerTarget target = playerTargets.Find(
                candidate => candidate.ClientId == enemy.AssignedClientId
            );

            if (target != null)
            {
                target.AssignedCount++;
            }
        }
    }

    private void ResetAssignedCounts()
    {
        foreach (PlayerTarget target in playerTargets)
        {
            target.AssignedCount = 0;
        }
    }

    private static bool HasSpawnAuthority()
    {
        return !IsNetworkSessionRunning() || NetworkManager.Singleton.IsServer;
    }

    private static bool IsNetworkSessionRunning()
    {
        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
    }

    private void OnValidate()
    {
        maxSpawnDistance = Mathf.Max(minSpawnDistance, maxSpawnDistance);
    }
}
