using Shura.Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Ready,
    Playing,
    Result
}

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private WaveManager waveManager;

    [SerializeField]
    private PlayerHealth playerHealth;

    [SerializeField]
    private Transform player;

    [Header("Boss")]
    [SerializeField]
    private GameObject bossPrefab;

    [SerializeField]
    private Vector3 bossSpawnOffset =
        new Vector3(6f, 0f, 0f);

    [SerializeField, Min(1f)]
    private float bossReturnDistance = 30f;

    [SerializeField, Min(0f)]
    private float bossReturnRadiusMin = 8f;

    [SerializeField, Min(0f)]
    private float bossReturnRadiusMax = 13f;

    [SerializeField, Min(0.1f)]
    private float bossDistanceCheckInterval = 0.75f;

    private GameObject spawnedBoss;

    private bool bossSpawnAttempted;
    private bool bossSpawned;
    private float bossDistanceCheckTimer;
    [SerializeField, Min(1f)]
    private float bossHealth = 500f;

    public GameState CurrentState { get; private set; }
        = GameState.Ready;

    public bool IsVictory { get; private set; }

    public void Configure(
        WaveManager configuredWaveManager,
        GameObject configuredBossPrefab,
        float configuredBossHealth = 500f
    )
    {
        waveManager = configuredWaveManager;
        bossPrefab = configuredBossPrefab;
        bossHealth = Mathf.Max(1f, configuredBossHealth);
    }

    private void Start()
    {
        Time.timeScale = 1f;

        FindReferences();
        StartGame();
    }

    private void Update()
    {
        if (CurrentState != GameState.Playing)
        {
            return;
        }

        CheckPlayerDeath();
        CheckBossStart();
        CheckBossDeath();
        CheckBossDistance();
    }

    public void StartGame()
    {
        CurrentState = GameState.Playing;
        IsVictory = false;

        Debug.Log("게임 시작");
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }

    private void CheckPlayerDeath()
    {
        if (IsNetworkSessionRunning())
        {
            return;
        }

        if (playerHealth == null)
        {
            return;
        }

        if (playerHealth.IsDead)
        {
            FinishGame(false);
        }
    }

    private void CheckBossStart()
    {
        if (CurrentState != GameState.Playing)
        {
            return;
        }

        if (bossSpawnAttempted)
        {
            return;
        }

        if (IsNetworkSessionRunning())
        {
            NetworkGameResultState resultState =
                FindFirstObjectByType<NetworkGameResultState>();

            if (resultState != null && resultState.HasFinished)
            {
                return;
            }
        }

        if (waveManager == null ||
            !waveManager.RoundFinished)
        {
            return;
        }

        SpawnBoss();
    }

    private void SpawnBoss()
    {
        if (IsNetworkSessionRunning() &&
            !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        if (bossPrefab == null)
        {
            Debug.LogError(
                "GameManager에 Boss Prefab이 연결되지 않았습니다."
            );
            return;
        }

        Transform spawnTarget = FindLivingPlayer();

        if (spawnTarget == null)
        {
            Debug.LogError(
                "GameManager가 살아 있는 Player를 찾지 못했습니다."
            );
            return;
        }

        bossSpawnAttempted = true;

        Vector3 spawnPosition =
            spawnTarget.position + bossSpawnOffset;

        spawnPosition.z = 0f;

        spawnedBoss = Instantiate(
            bossPrefab,
            spawnPosition,
            Quaternion.identity
        );

        EnemyHealth bossEnemyHealth = spawnedBoss.GetComponent<EnemyHealth>();
        bossEnemyHealth?.ConfigureRuntime(
            bossHealth / 500f,
            0f,
            null,
            1
        );

        if (IsNetworkSessionRunning())
        {
            NetworkObject bossNetworkObject =
                spawnedBoss.GetComponent<NetworkObject>();

            if (bossNetworkObject == null)
            {
                Debug.LogError(
                    "보스 프리팹에 NetworkObject가 없어 네트워크 스폰할 수 없습니다."
                );
                Destroy(spawnedBoss);
                spawnedBoss = null;
                return;
            }

            bossNetworkObject.Spawn();
        }

        bossSpawned = true;

        CombatFeedbackPresenter.ShowBossWarning("장산범");

        Debug.Log("보스 등장");
    }

    private Transform FindLivingPlayer()
    {
        if (IsNetworkSessionRunning())
        {
            foreach (NetworkClient client in
                     NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject == null)
                {
                    continue;
                }

                NetworkPlayerHealth health =
                    client.PlayerObject.GetComponent<NetworkPlayerHealth>();

                if (health == null || !health.IsDead)
                {
                    player = client.PlayerObject.transform;
                    playerHealth = player.GetComponent<PlayerHealth>();
                    return player;
                }
            }

            return null;
        }

        if (playerHealth != null && !playerHealth.IsDead)
        {
            player = playerHealth.transform;
            return player;
        }

        PlayerHealth[] candidates =
            FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);

        foreach (PlayerHealth candidate in candidates)
        {
            if (candidate != null && !candidate.IsDead)
            {
                playerHealth = candidate;
                player = candidate.transform;
                return player;
            }
        }

        return null;
    }

    private void CheckBossDeath()
    {
        if (IsNetworkSessionRunning() &&
            !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        if (!bossSpawned)
        {
            return;
        }

        if (spawnedBoss == null)
        {
            FinishGame(true);
        }
    }

    private void CheckBossDistance()
    {
        if (!bossSpawned || spawnedBoss == null ||
            (IsNetworkSessionRunning() && !NetworkManager.Singleton.IsServer))
        {
            return;
        }

        bossDistanceCheckTimer -= Time.deltaTime;

        if (bossDistanceCheckTimer > 0f)
        {
            return;
        }

        bossDistanceCheckTimer = bossDistanceCheckInterval;
        Transform nearestPlayer = FindNearestLivingPlayer(
            spawnedBoss.transform.position
        );

        if (nearestPlayer == null ||
            (nearestPlayer.position - spawnedBoss.transform.position)
                .sqrMagnitude <= bossReturnDistance * bossReturnDistance)
        {
            return;
        }

        float angle = Random.Range(0f, Mathf.PI * 2f);
        float distance = Random.Range(
            bossReturnRadiusMin,
            bossReturnRadiusMax
        );
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) *
            distance;
        Vector3 position = nearestPlayer.position + (Vector3)offset;
        position.z = 0f;

        Rigidbody2D rigidBody = spawnedBoss.GetComponent<Rigidbody2D>();

        if (rigidBody != null)
        {
            rigidBody.linearVelocity = Vector2.zero;
            rigidBody.position = position;
        }

        spawnedBoss.transform.position = position;
        spawnedBoss.GetComponent<EnemyController>()?.Retarget(
            nearestPlayer,
            GetClientId(nearestPlayer)
        );
        spawnedBoss.GetComponent<JangsanbeomBossPattern>()?
            .RestartAfterReposition();
    }

    private Transform FindNearestLivingPlayer(Vector3 origin)
    {
        Transform nearest = null;
        float nearestDistance = float.MaxValue;
        GameObject[] candidates = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject candidate in candidates)
        {
            if (candidate == null || !IsLivingPlayer(candidate.transform))
            {
                continue;
            }

            float distance = (candidate.transform.position - origin).sqrMagnitude;

            if (distance < nearestDistance)
            {
                nearest = candidate.transform;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private static bool IsLivingPlayer(Transform candidate)
    {
        NetworkPlayerHealth networkHealth =
            candidate.GetComponent<NetworkPlayerHealth>();

        if (networkHealth != null && networkHealth.IsDead)
        {
            return false;
        }

        PlayerHealth localHealth = candidate.GetComponent<PlayerHealth>();
        return localHealth == null || !localHealth.IsDead;
    }

    private static ulong GetClientId(Transform target)
    {
        NetworkObject networkObject = target.GetComponent<NetworkObject>();
        return networkObject != null ? networkObject.OwnerClientId : 0;
    }

    private void FinishGame(bool victory)
    {
        if (CurrentState == GameState.Result)
        {
            return;
        }

        CurrentState = GameState.Result;
        IsVictory = victory;

        if (IsNetworkSessionRunning())
        {
            if (victory && NetworkManager.Singleton.IsServer)
            {
                NetworkGameResultState resultState =
                    FindFirstObjectByType<NetworkGameResultState>();

                if (resultState != null)
                {
                    resultState.SetVictoryServer();
                }
                else
                {
                    Debug.LogError(
                        "NetworkGameResultState를 찾지 못해 승리를 동기화할 수 없습니다."
                    );
                }
            }

            Debug.Log(victory ? "네트워크 승리!" : "네트워크 패배!");
            return;
        }

        Time.timeScale = 0f;

        Debug.Log(
            victory ? "승리!" : "패배!"
        );
    }

    private static bool IsNetworkSessionRunning()
    {
        return NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening;
    }

    private void FindReferences()
    {
        if (waveManager == null)
        {
            waveManager =
                FindFirstObjectByType<WaveManager>();
        }

        if (playerHealth == null)
        {
            playerHealth =
                FindFirstObjectByType<PlayerHealth>();
        }

        if (player == null &&
            playerHealth != null)
        {
            player = playerHealth.transform;
        }

        if (waveManager == null)
        {
            Debug.LogError(
                "GameManager가 WaveManager를 찾지 못했습니다."
            );
        }

        if (playerHealth == null)
        {
            Debug.LogError(
                "GameManager가 PlayerHealth를 찾지 못했습니다."
            );
        }
    }

    private void OnValidate()
    {
        bossReturnDistance = Mathf.Max(1f, bossReturnDistance);
        bossReturnRadiusMin = Mathf.Max(0f, bossReturnRadiusMin);
        bossReturnRadiusMax = Mathf.Max(
            bossReturnRadiusMin,
            bossReturnRadiusMax
        );
        bossDistanceCheckInterval = Mathf.Max(
            0.1f,
            bossDistanceCheckInterval
        );
    }
}
