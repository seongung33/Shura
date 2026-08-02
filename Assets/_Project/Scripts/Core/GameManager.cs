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

    private GameObject spawnedBoss;

    private bool bossSpawnAttempted;
    private bool bossSpawned;

    public GameState CurrentState { get; private set; }
        = GameState.Ready;

    public bool IsVictory { get; private set; }

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

        if (waveManager == null ||
            !waveManager.RoundFinished)
        {
            return;
        }

        SpawnBoss();
    }

    private void SpawnBoss()
    {
        bossSpawnAttempted = true;

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

        if (player == null)
        {
            Debug.LogError(
                "GameManager가 Player를 찾지 못했습니다."
            );
            return;
        }

        Vector3 spawnPosition =
            player.position + bossSpawnOffset;

        spawnPosition.z = 0f;

        spawnedBoss = Instantiate(
            bossPrefab,
            spawnPosition,
            Quaternion.identity
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

        Debug.Log("보스 등장");
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
}
