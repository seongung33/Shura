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

    public EnemyRole Role => role;
    public GameObject Prefab => prefab;
    public float Weight => Mathf.Max(0f, weight);
    public float HealthMultiplier => Mathf.Max(0.01f, healthMultiplier);
    public float DamageMultiplier => Mathf.Max(0.01f, damageMultiplier);
    public float MoveSpeedMultiplier => Mathf.Max(0.01f, moveSpeedMultiplier);
    public float ExperienceWeight => Mathf.Max(0.01f, experienceWeight);
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
        return Mathf.RoundToInt(
            Mathf.Lerp(TargetAliveStart, TargetAliveEnd, GetProgress(elapsedTime))
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

    [SerializeField]
    private WaveSegment[] waveSegments = Array.Empty<WaveSegment>();

    public string StageId => stageId;
    public float Duration => Mathf.Max(1f, duration);
    public float CleanupStart => Mathf.Clamp(cleanupStart, 0f, Duration);
    public int MaxAlive => Mathf.Max(1, maxAlive);
    public float SpawnRadiusMin => Mathf.Max(0f, spawnRadiusMin);
    public float SpawnRadiusMax => Mathf.Max(SpawnRadiusMin, spawnRadiusMax);
    public GameObject BossPrefab => bossPrefab;
    public float BossHealth => Mathf.Max(1f, bossHealth);
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
    }
}
