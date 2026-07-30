using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField]
    private EnemySpawner enemySpawner;

    [Header("Test Round")]
    [SerializeField, Min(1f)]
    private float roundDuration = 60f;

    [Header("Wave Start Time")]
    [SerializeField, Min(0f)]
    private float wave2StartTime = 20f;

    [SerializeField, Min(0f)]
    private float wave3StartTime = 40f;

    [Header("Wave 1")]
    [SerializeField, Min(0.1f)]
    private float wave1SpawnInterval = 2f;

    [SerializeField, Min(1)]
    private int wave1MaxAlive = 15;

    [Header("Wave 2")]
    [SerializeField, Min(0.1f)]
    private float wave2SpawnInterval = 1f;

    [SerializeField, Min(1)]
    private int wave2MaxAlive = 25;

    [Header("Wave 3")]
    [SerializeField, Min(0.1f)]
    private float wave3SpawnInterval = 0.5f;

    [SerializeField, Min(1)]
    private int wave3MaxAlive = 40;

    private float elapsedTime;
    private int currentWave;
    private bool roundFinished;

    public float ElapsedTime => elapsedTime;
    public int CurrentWave => currentWave;
    public bool RoundFinished => roundFinished;

    private void Start()
    {
        if (enemySpawner == null)
        {
            enemySpawner =
                GetComponent<EnemySpawner>();
        }

        if (enemySpawner == null)
        {
            Debug.LogError(
                "WaveManager에 EnemySpawner가 연결되지 않았습니다."
            );

            enabled = false;
            return;
        }

        ApplyWave(1);
    }

    private void Update()
    {
        if (roundFinished)
        {
            return;
        }

        elapsedTime += Time.deltaTime;

        if (elapsedTime >= roundDuration)
        {
            FinishRound();
            return;
        }

        int nextWave = GetWaveByTime();

        if (nextWave != currentWave)
        {
            ApplyWave(nextWave);
        }
    }

    private int GetWaveByTime()
    {
        if (elapsedTime >= wave3StartTime)
        {
            return 3;
        }

        if (elapsedTime >= wave2StartTime)
        {
            return 2;
        }

        return 1;
    }

    private void ApplyWave(int wave)
    {
        currentWave = wave;

        switch (wave)
        {
            case 1:
                enemySpawner.ApplyWaveSettings(
                    wave1SpawnInterval,
                    wave1MaxAlive
                );
                break;

            case 2:
                enemySpawner.ApplyWaveSettings(
                    wave2SpawnInterval,
                    wave2MaxAlive
                );
                break;

            case 3:
                enemySpawner.ApplyWaveSettings(
                    wave3SpawnInterval,
                    wave3MaxAlive
                );
                break;
        }

        Debug.Log($"Wave {currentWave} 시작");
    }

    private void FinishRound()
    {
        roundFinished = true;

        enemySpawner.SetSpawningEnabled(false);

        Debug.Log(
            "테스트 라운드 종료 - 다음 단계에서 보스를 생성합니다."
        );
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