using Shura.Player;
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

        bossSpawned = true;

        Debug.Log("보스 등장");
    }

    private void CheckBossDeath()
    {
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

        Time.timeScale = 0f;

        Debug.Log(
            victory ? "승리!" : "패배!"
        );
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