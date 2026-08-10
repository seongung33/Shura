using System;
using UnityEngine;

public enum EnemyRole
{
    Swarm,
    Basic,
    Fast,
    Medium,
    Elite
}

[Serializable]
public sealed class EnemySpawnEntry
{
    [SerializeField]
    private EnemyRole role;

    [SerializeField]
    private GameObject prefab;

    [SerializeField, Min(0f)]
    private float weight = 1f;

    [SerializeField, Min(0.01f)]
    private float healthMultiplier = 1f;

    [SerializeField, Min(0.01f)]
    private float damageMultiplier = 1f;

    [SerializeField, Min(0.01f)]
    private float moveSpeedMultiplier = 1f;

    [SerializeField, Min(0.01f)]
    private float experienceWeight = 1f;

    [SerializeField, Min(0.01f)]
    private float scaleMultiplier = 1f;

    public EnemyRole Role => role;
    public GameObject Prefab => prefab;
    public float Weight => Mathf.Max(0f, weight);
    public float HealthMultiplier => Mathf.Max(0.01f, healthMultiplier);
    public float DamageMultiplier => Mathf.Max(0.01f, damageMultiplier);
    public float MoveSpeedMultiplier => Mathf.Max(0.01f, moveSpeedMultiplier);
    public float ExperienceWeight => Mathf.Max(0.01f, experienceWeight);
    public float ScaleMultiplier => scaleMultiplier > 0f
        ? scaleMultiplier
        : 1f;
}

[Serializable]
public sealed class WaveSegment
{
    [SerializeField, Min(0f)]
    private float startTime;

    [SerializeField, Min(0f)]
    private float endTime = 180f;

    [SerializeField, Min(0)]
    private int targetAliveStart = 35;

    [SerializeField, Min(0)]
    private int targetAliveEnd = 70;

    [SerializeField, Min(1)]
    private int maxAlive = 70;

    [SerializeField, Min(0.02f)]
    private float spawnInterval = 0.45f;

    [SerializeField, Range(1, 6)]
    private int spawnBatchSize = 2;

    [SerializeField, Range(1, 6)]
    private int catchUpBatchSize = 5;

    [SerializeField, Range(0.1f, 0.9f)]
    private float catchUpThreshold = 0.5f;

    [SerializeField, Range(0.1f, 1f)]
    private float catchUpIntervalMultiplier = 0.5f;

    [SerializeField, Min(0f)]
    private float spawnBudgetPerMinute = 77f;

    [SerializeField, Min(0f)]
    private float teamExperiencePerMinute = 63f;

    [SerializeField, Min(0.01f)]
    private float healthMultiplier = 1f;

    [SerializeField, Min(0.01f)]
    private float damageMultiplier = 1f;

    [SerializeField, Range(1, 20)]
    private int maxAttackersPerPlayer = 5;

    [SerializeField, Min(1)]
    private int experienceOrbBundleSize = 3;

    [SerializeField]
    private EnemySpawnEntry[] enemies = Array.Empty<EnemySpawnEntry>();

    public float StartTime => startTime;
    public float EndTime => Mathf.Max(startTime, endTime);
    public int TargetAliveStart => Mathf.Max(0, targetAliveStart);
    public int TargetAliveEnd => Mathf.Max(0, targetAliveEnd);
    public int MaxAlive => Mathf.Max(1, maxAlive);
    public float SpawnInterval => Mathf.Max(0.02f, spawnInterval);
    public int SpawnBatchSize => Mathf.Clamp(spawnBatchSize, 1, 6);
    public int CatchUpBatchSize => Mathf.Clamp(
        Mathf.Max(spawnBatchSize, catchUpBatchSize),
        1,
        6
    );
    public float CatchUpThreshold => Mathf.Clamp(catchUpThreshold, 0.1f, 0.9f);
    public float CatchUpIntervalMultiplier => Mathf.Clamp(
        catchUpIntervalMultiplier,
        0.1f,
        1f
    );
    public float SpawnBudgetPerMinute => Mathf.Max(0f, spawnBudgetPerMinute);
    public float TeamExperiencePerMinute => Mathf.Max(0f, teamExperiencePerMinute);
    public float HealthMultiplier => Mathf.Max(0.01f, healthMultiplier);
    public float DamageMultiplier => Mathf.Max(0.01f, damageMultiplier);
    public int MaxAttackersPerPlayer => Mathf.Max(1, maxAttackersPerPlayer);
    public int ExperienceOrbBundleSize => Mathf.Max(1, experienceOrbBundleSize);
    public EnemySpawnEntry[] Enemies => enemies ?? Array.Empty<EnemySpawnEntry>();

    public float GetProgress(float elapsedTime)
    {
        return Mathf.InverseLerp(StartTime, EndTime, elapsedTime);
    }

    public int GetTargetAlive(float elapsedTime)
    {
        return Mathf.Min(
            MaxAlive,
            Mathf.RoundToInt(
                Mathf.Lerp(TargetAliveStart, TargetAliveEnd, GetProgress(elapsedTime))
            )
        );
    }
}

[CreateAssetMenu(
    fileName = "StageConfig",
    menuName = "Shura/Balance/Stage Config"
)]
public sealed class StageConfig : ScriptableObject
{
    [SerializeField]
    private string stageId = "jangsanbeom_forest";

    [SerializeField, Min(1f)]
    private float duration = 900f;

    [SerializeField, Min(0f)]
    private float cleanupStart = 870f;

    [SerializeField, Min(0)]
    private int cleanupTargetAlive = 230;

    [SerializeField, Min(1f)]
    private float cleanupSpawnIntervalMultiplier = 1.5f;

    [SerializeField, Min(1)]
    private int maxAlive = 300;

    [SerializeField, Min(0f)]
    private float spawnRadiusMin = 8f;

    [SerializeField, Min(0f)]
    private float spawnRadiusMax = 12f;

    [SerializeField]
    private GameObject bossPrefab;

    [SerializeField, Min(1f)]
    private float bossHealth = 2100f;

    [Header("Elite Rewards")]

    [SerializeField]
    private EnemySpawnEntry eliteEnemy;

    [SerializeField]
    private float[] eliteSpawnTimes = { 240f, 480f, 720f };

    [Header("Field Supplies")]

    [SerializeField]
    private BreakableSupplyObject fieldSupplyPrefab;

    [SerializeField, Min(1f)]
    private float fieldSupplyCellSize = 24f;

    [SerializeField, Range(0f, 1f)]
    private float fieldSupplySpawnChance = 0.25f;

    [SerializeField, Min(0f)]
    private float fieldSupplyMinimumPlayerDistance = 4f;

    [SerializeField, Min(1)]
    private int maxExistingFieldSupplies = 6;

    [SerializeField]
    private WaveSegment[] waveSegments = Array.Empty<WaveSegment>();

    public string StageId => stageId;
    public float Duration => Mathf.Max(1f, duration);
    public float CleanupStart => Mathf.Clamp(cleanupStart, 0f, Duration);
    public int MaxAlive => Mathf.Max(1, maxAlive);
    public int CleanupTargetAlive => Mathf.Clamp(cleanupTargetAlive, 0, MaxAlive);
    public float CleanupSpawnIntervalMultiplier => Mathf.Max(
        1f,
        cleanupSpawnIntervalMultiplier
    );
    public float SpawnRadiusMin => Mathf.Max(0f, spawnRadiusMin);
    public float SpawnRadiusMax => Mathf.Max(SpawnRadiusMin, spawnRadiusMax);
    public GameObject BossPrefab => bossPrefab;
    public float BossHealth => Mathf.Max(1f, bossHealth);
    public EnemySpawnEntry EliteEnemy => eliteEnemy;
    public float[] EliteSpawnTimes => eliteSpawnTimes ?? Array.Empty<float>();
    public BreakableSupplyObject FieldSupplyPrefab => fieldSupplyPrefab;
    public float FieldSupplyCellSize => Mathf.Max(1f, fieldSupplyCellSize);
    public float FieldSupplySpawnChance =>
        Mathf.Clamp01(fieldSupplySpawnChance);
    public float FieldSupplyMinimumPlayerDistance =>
        Mathf.Max(0f, fieldSupplyMinimumPlayerDistance);
    public int MaxExistingFieldSupplies =>
        Mathf.Max(1, maxExistingFieldSupplies);
    public WaveSegment[] WaveSegments => waveSegments ?? Array.Empty<WaveSegment>();

    public WaveSegment GetSegment(float elapsedTime)
    {
        foreach (WaveSegment segment in WaveSegments)
        {
            if (segment != null &&
                elapsedTime >= segment.StartTime &&
                elapsedTime < segment.EndTime)
            {
                return segment;
            }
        }

        return null;
    }

    public int GetSegmentIndex(float elapsedTime)
    {
        for (int index = 0; index < WaveSegments.Length; index++)
        {
            WaveSegment segment = WaveSegments[index];

            if (segment != null &&
                elapsedTime >= segment.StartTime &&
                elapsedTime < segment.EndTime)
            {
                return index;
            }
        }

        return -1;
    }

    private void OnValidate()
    {
        cleanupStart = Mathf.Clamp(cleanupStart, 0f, duration);
        spawnRadiusMax = Mathf.Max(spawnRadiusMin, spawnRadiusMax);

        if (eliteSpawnTimes != null)
        {
            for (int index = 0; index < eliteSpawnTimes.Length; index++)
            {
                eliteSpawnTimes[index] = Mathf.Clamp(
                    eliteSpawnTimes[index],
                    0f,
                    duration
                );
            }

            Array.Sort(eliteSpawnTimes);
        }

        fieldSupplyCellSize = Mathf.Max(1f, fieldSupplyCellSize);
        fieldSupplySpawnChance = Mathf.Clamp01(fieldSupplySpawnChance);
        fieldSupplyMinimumPlayerDistance = Mathf.Max(
            0f,
            fieldSupplyMinimumPlayerDistance
        );
        maxExistingFieldSupplies = Mathf.Max(
            1,
            maxExistingFieldSupplies
        );
    }
}
