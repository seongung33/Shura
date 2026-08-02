using Shura.Camera;
using Unity.Netcode;
using UnityEngine;

public class NetworkStageBootstrap : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField]
    private GameObject enemyPrefab;

    [SerializeField]
    private GameObject bossPrefab;

    [Header("Test Round")]
    [SerializeField, Min(1f)]
    private float roundDuration = 60f;

    [SerializeField, Min(0f)]
    private float wave2StartTime = 20f;

    [SerializeField, Min(0f)]
    private float wave3StartTime = 40f;

    private CameraFollow cameraFollow;
    private bool cameraBound;

    private void Start()
    {
        if (NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsListening)
        {
            Debug.LogError(
                "NetworkStageBootstrap은 실행 중인 네트워크 세션이 필요합니다."
            );
            enabled = false;
            return;
        }

        if (enemyPrefab == null || bossPrefab == null)
        {
            Debug.LogError(
                "NetworkStageBootstrap의 적 또는 보스 프리팹이 비어 있습니다."
            );
            enabled = false;
            return;
        }

        cameraFollow = FindFirstObjectByType<CameraFollow>();
        CreateStageRuntime();
        TryBindLocalCamera();
    }

    private void Update()
    {
        if (!cameraBound)
        {
            TryBindLocalCamera();
        }
    }

    private void CreateStageRuntime()
    {
        GameObject runtimeObject = new GameObject("NetworkStageRuntime");

        EnemySpawner enemySpawner =
            runtimeObject.AddComponent<EnemySpawner>();
        enemySpawner.Configure(enemyPrefab);

        WaveManager waveManager =
            runtimeObject.AddComponent<WaveManager>();
        waveManager.Configure(
            enemySpawner,
            roundDuration,
            wave2StartTime,
            wave3StartTime
        );

        GameManager gameManager =
            runtimeObject.AddComponent<GameManager>();
        gameManager.Configure(waveManager, bossPrefab);
    }

    private void TryBindLocalCamera()
    {
        if (cameraFollow == null)
        {
            cameraFollow = FindFirstObjectByType<CameraFollow>();
        }

        if (cameraFollow == null || NetworkManager.Singleton == null)
        {
            return;
        }

        NetworkObject localPlayer =
            NetworkManager.Singleton.LocalClient?.PlayerObject;

        if (localPlayer == null)
        {
            return;
        }

        cameraFollow.SetTarget(localPlayer.transform);
        cameraBound = true;
    }

    private void OnValidate()
    {
        wave2StartTime = Mathf.Clamp(
            wave2StartTime,
            0f,
            roundDuration
        );
        wave3StartTime = Mathf.Clamp(
            wave3StartTime,
            wave2StartTime,
            roundDuration
        );
    }
}
