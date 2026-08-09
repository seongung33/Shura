using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [SerializeField]
    private EnemySpawner enemySpawner;

    [SerializeField]
    private StageConfig stageConfig;

    [Header("Legacy Test Fallback")]
    [SerializeField, Min(1f)]
    private float roundDuration = 60f;

    [SerializeField, Min(0f)]
    private float wave2StartTime = 20f;

    [SerializeField, Min(0f)]
    private float wave3StartTime = 40f;

    private float elapsedTime;
    private int currentWave;
    private bool cleanupStarted;
    private bool roundFinished;

    public float ElapsedTime => elapsedTime;
    public float Duration => stageConfig != null ? stageConfig.Duration : roundDuration;
    public int CurrentWave => currentWave;
    public bool CleanupStarted => cleanupStarted;
    public bool RoundFinished => roundFinished;

    public void Configure(EnemySpawner configuredSpawner, StageConfig configuredStage)
    {
        enemySpawner = configuredSpawner;
        stageConfig = configuredStage;
    }

    public void Configure(
        EnemySpawner configuredSpawner,
        float configuredRoundDuration,
        float configuredWave2StartTime,
        float configuredWave3StartTime
    )
    {
        enemySpawner = configuredSpawner;
        roundDuration = Mathf.Max(1f, configuredRoundDuration);
        wave2StartTime = Mathf.Clamp(configuredWave2StartTime, 0f, roundDuration);
        wave3StartTime = Mathf.Clamp(configuredWave3StartTime, wave2StartTime, roundDuration);
    }

    private void Start()
    {
        if (enemySpawner == null)
        {
            enemySpawner = GetComponent<EnemySpawner>();
        }

        if (enemySpawner == null)
        {
            Debug.LogError("WaveManager에 EnemySpawner가 연결되지 않았습니다.");
            enabled = false;
            return;
        }

        if (stageConfig != null)
        {
            enemySpawner.Configure(stageConfig);
            ApplyConfiguredSegment();
        }
        else
        {
            ApplyLegacyWave(1);
        }
    }

    private void Update()
    {
        if (roundFinished)
        {
            return;
        }

        elapsedTime += Time.deltaTime;

        if (stageConfig != null)
        {
            UpdateConfiguredStage();
        }
        else
        {
            UpdateLegacyStage();
        }
    }

    private void UpdateConfiguredStage()
    {
        if (!cleanupStarted && elapsedTime >= stageConfig.CleanupStart)
        {
            cleanupStarted = true;
            enemySpawner.SetSpawningEnabled(false);
            Debug.Log("보스 전 정리 구간 시작");
        }

        if (elapsedTime >= stageConfig.Duration)
        {
            FinishRound();
            return;
        }

        ApplyConfiguredSegment();
    }

    private void ApplyConfiguredSegment()
    {
        WaveSegment segment = stageConfig.GetSegment(elapsedTime);
        int segmentIndex = stageConfig.GetSegmentIndex(elapsedTime);
        enemySpawner.ApplyStageState(segment, elapsedTime);

        int nextWave = segmentIndex >= 0 ? segmentIndex + 1 : 0;

        if (nextWave != currentWave)
        {
            currentWave = nextWave;
            Debug.Log(currentWave > 0
                ? $"Wave {currentWave} 시작"
                : "정리 구간 진행 중");
        }
    }

    private void UpdateLegacyStage()
    {
        if (elapsedTime >= roundDuration)
        {
            FinishRound();
            return;
        }

        int nextWave = elapsedTime >= wave3StartTime
            ? 3
            : elapsedTime >= wave2StartTime ? 2 : 1;

        if (nextWave != currentWave)
        {
            ApplyLegacyWave(nextWave);
        }
    }

    private void ApplyLegacyWave(int wave)
    {
        currentWave = wave;
        float interval = wave == 1 ? 2f : wave == 2 ? 1f : 0.5f;
        int alive = wave == 1 ? 15 : wave == 2 ? 25 : 40;
        enemySpawner.ApplyWaveSettings(interval, alive);
        Debug.Log($"Wave {currentWave} 시작");
    }

    private void FinishRound()
    {
        roundFinished = true;
        enemySpawner.SetSpawningEnabled(false);
        enemySpawner.DespawnAllEnemies();
        Debug.Log("일반 웨이브 종료 - 보스 전환");
    }

    private void OnValidate()
    {
        wave2StartTime = Mathf.Clamp(wave2StartTime, 0f, roundDuration);
        wave3StartTime = Mathf.Clamp(wave3StartTime, wave2StartTime, roundDuration);
    }
}
