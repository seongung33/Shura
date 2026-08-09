using Shura.Camera;
using Unity.Netcode;
using UnityEngine;

public class NetworkStageBootstrap : MonoBehaviour
{
    private static GameObject cachedPlayerPrefab;

    [Header("Stage Balance")]
    [SerializeField]
    private StageConfig stageConfig;

    [SerializeField]
    private LevelCurveData levelCurve;

    [Header("Level Up")]
    [SerializeField]
    private LevelUpSettings levelUpSettings = new();

    [Header("Players")]
    [SerializeField]
    private GameObject playerPrefab;

    [SerializeField]
    private GameObject offlinePlayerPrefab;

    [Header("Legacy Fallback Prefabs")]
    [SerializeField]
    private GameObject enemyPrefab;

    [SerializeField]
    private GameObject bossPrefab;

    [Header("Legacy Test Round")]
    [SerializeField, Min(1f)]
    private float roundDuration = 60f;

    [SerializeField, Min(0f)]
    private float wave2StartTime = 20f;

    [SerializeField, Min(0f)]
    private float wave3StartTime = 40f;

    private CameraFollow cameraFollow;
    private Transform offlinePlayer;
    private bool cameraBound;
    private bool missingPlayerPrefabLogged;

    public static void CachePlayerPrefab(GameObject prefab)
    {
        if (prefab != null)
        {
            cachedPlayerPrefab = prefab;
        }
    }

    private void Start()
    {
        cameraFollow = FindFirstObjectByType<CameraFollow>();

        if (IsNetworkSessionRunning())
        {
            EnsurePlayerObjects();
        }
        else
        {
            EnsureOfflinePlayer();
        }

        CreateStageRuntime();
        TryBindLocalCamera();
    }

    private void Update()
    {
        if (IsNetworkSessionRunning())
        {
            EnsurePlayerObjects();
        }

        if (!cameraBound)
        {
            TryBindLocalCamera();
        }
    }

    private void EnsureOfflinePlayer()
    {
        GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");

        if (existingPlayer != null)
        {
            offlinePlayer = existingPlayer.transform;
        }
        else
        {
            GameObject configuredPrefab = offlinePlayerPrefab;

            if (configuredPrefab == null)
            {
                Debug.LogError("NetworkStageBootstrap의 Offline Player Prefab이 비어 있습니다.");
                return;
            }

            offlinePlayer = Instantiate(
                configuredPrefab,
                Vector3.zero,
                Quaternion.identity
            ).transform;
        }

        if (offlinePlayer.GetComponent<PlayerExperience>() == null)
        {
            offlinePlayer.gameObject.AddComponent<PlayerExperience>();
        }

        ApplyOfflineCharacterSelection();
    }

    private void ApplyOfflineCharacterSelection()
    {
        CharacterData data = SinglePlayerSelection.Character;

        if (offlinePlayer == null || data == null)
        {
            return;
        }

        offlinePlayer.GetComponent<Shura.Player.PlayerController>()
            ?.ConfigureBaseSpeed(data.MoveSpeed);
        offlinePlayer.GetComponent<Shura.Player.PlayerHealth>()
            ?.ConfigureMaxHealth(data.MaxHealth);
        offlinePlayer.GetComponent<DirectionalAutoAttack>()
            ?.ConfigureBasicSkill(data.BasicSkill);
        offlinePlayer.GetComponent<PlayerRuntimeGrowth>()
            ?.ConfigureBasicSkill(data.BasicSkill);
        offlinePlayer.GetComponent<AutoSkillCaster>()
            ?.ConfigureSkills(data.StartingSkills);

        ApplyOfflineCharacterVisual(data);
        Debug.Log($"싱글 캐릭터 적용: {data.characterName}");
    }

    private void ApplyOfflineCharacterVisual(CharacterData data)
    {
        if (data.GameplayVisualPrefab == null)
        {
            return;
        }

        SpriteRenderer fallbackRenderer =
            offlinePlayer.GetComponent<SpriteRenderer>();

        if (fallbackRenderer != null)
        {
            fallbackRenderer.enabled = false;
        }

        GameObject visual = Instantiate(
            data.GameplayVisualPrefab,
            offlinePlayer
        );
        visual.name = $"{data.characterName}Visual";
        visual.transform.SetLocalPositionAndRotation(
            Vector3.zero,
            Quaternion.identity
        );
        visual.transform.localScale = Vector3.one;
    }

    private void EnsurePlayerObjects()
    {
        NetworkManager manager = NetworkManager.Singleton;

        if (manager == null || !manager.IsServer)
        {
            return;
        }

        GameObject configuredPlayerPrefab =
            playerPrefab != null ? playerPrefab : cachedPlayerPrefab;

        if (configuredPlayerPrefab == null)
        {
            if (!missingPlayerPrefabLogged)
            {
                Debug.LogError("NetworkStageBootstrap의 Player Prefab이 비어 있습니다.");
                missingPlayerPrefabLogged = true;
            }

            return;
        }

        foreach (NetworkClient client in manager.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                continue;
            }

            GameObject playerObject = Instantiate(configuredPlayerPrefab);
            NetworkObject networkObject = playerObject.GetComponent<NetworkObject>();

            if (networkObject == null)
            {
                Debug.LogError("Player Prefab에 NetworkObject가 없습니다.");
                Destroy(playerObject);
                continue;
            }

            networkObject.SpawnAsPlayerObject(client.ClientId, true);
            Debug.Log($"플레이어 재생성 완료: clientId={client.ClientId}");
        }
    }

    private void CreateStageRuntime()
    {
        GameObject runtimeObject = new GameObject("StageRuntime");

        TeamExperience teamExperience = runtimeObject.AddComponent<TeamExperience>();
        teamExperience.Configure(levelCurve);

        TeamLevelUpCoordinator levelUpCoordinator =
            runtimeObject.AddComponent<TeamLevelUpCoordinator>();
        levelUpCoordinator.Configure(teamExperience, levelUpSettings);

        EnemySpawner enemySpawner = runtimeObject.AddComponent<EnemySpawner>();
        enemySpawner.Configure(enemyPrefab);

        WaveManager waveManager = runtimeObject.AddComponent<WaveManager>();

        if (stageConfig != null)
        {
            enemySpawner.Configure(stageConfig);
            waveManager.Configure(enemySpawner, stageConfig);
        }
        else
        {
            waveManager.Configure(
                enemySpawner,
                roundDuration,
                wave2StartTime,
                wave3StartTime
            );
        }

        GameObject configuredBoss = stageConfig != null && stageConfig.BossPrefab != null
            ? stageConfig.BossPrefab
            : bossPrefab;
        float configuredBossHealth = stageConfig != null
            ? stageConfig.BossHealth
            : 500f;

        GameManager gameManager = runtimeObject.AddComponent<GameManager>();
        gameManager.Configure(waveManager, configuredBoss, configuredBossHealth);

        StageHudPresenter hud = runtimeObject.AddComponent<StageHudPresenter>();
        hud.Configure(waveManager, enemySpawner);

        runtimeObject.AddComponent<GameplayPauseMenu>();
    }

    private void TryBindLocalCamera()
    {
        if (cameraFollow == null)
        {
            cameraFollow = FindFirstObjectByType<CameraFollow>();
        }

        if (cameraFollow == null)
        {
            return;
        }

        if (!IsNetworkSessionRunning())
        {
            if (offlinePlayer != null)
            {
                cameraFollow.SetTarget(offlinePlayer);
                cameraBound = true;
            }

            return;
        }

        NetworkObject localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;

        if (localPlayer == null)
        {
            return;
        }

        cameraFollow.SetTarget(localPlayer.transform);
        cameraBound = true;
    }

    private static bool IsNetworkSessionRunning()
    {
        return NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening;
    }

    private void OnValidate()
    {
        wave2StartTime = Mathf.Clamp(wave2StartTime, 0f, roundDuration);
        wave3StartTime = Mathf.Clamp(wave3StartTime, wave2StartTime, roundDuration);
    }
}
