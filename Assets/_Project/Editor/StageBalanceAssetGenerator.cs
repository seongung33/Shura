using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class StageBalanceAssetGenerator
{
    private const string BalanceFolder =
        "Assets/_Project/ScriptableObjects/Balance";
    private const string LevelCurvePath =
        BalanceFolder + "/LevelCurve_Default.asset";
    private const string StageConfigPath =
        BalanceFolder + "/StageConfig_JangsanForest.asset";

    [InitializeOnLoadMethod]
    private static void CreateMissingDefaultAssets()
    {
        if (AssetDatabase.LoadAssetAtPath<StageConfig>(StageConfigPath) != null)
        {
            return;
        }

        EditorApplication.delayCall += CreateDefaultAssets;
    }

    [MenuItem("Shura/Balance/Create And Apply Default Stage")]
    public static void CreateDefaultAssets()
    {
        EnsureFolder();

        LevelCurveData levelCurve = LoadOrCreate<LevelCurveData>(LevelCurvePath);
        StageConfig stageConfig = LoadOrCreate<StageConfig>(StageConfigPath);

        GameObject normal = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Project/Prefabs/Enemy/Enemy_normal.prefab"
        );
        GameObject fast = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Project/Prefabs/Enemy/Enemy_fast.prefab"
        );
        GameObject tank = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Project/Prefabs/Enemy/Enemy_tank.prefab"
        );
        GameObject boss = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Project/Prefabs/Enemy/JangsanTiger/BossJangsanTiger.prefab"
        );

        ConfigureStage(stageConfig, normal, fast, tank, boss);
        ApplyMainScene(stageConfig, levelCurve, normal, boss);
        ApplyStageTestScene(stageConfig, levelCurve);
        ApplyMainMenuScene();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("15분 장산범 스테이지 밸런스 생성 및 씬 연결 완료");
    }

    private static void ConfigureStage(
        StageConfig stage,
        GameObject normal,
        GameObject fast,
        GameObject tank,
        GameObject boss
    )
    {
        SerializedObject serialized = new SerializedObject(stage);
        serialized.FindProperty("stageId").stringValue = "jangsanbeom_forest";
        serialized.FindProperty("duration").floatValue = 900f;
        serialized.FindProperty("cleanupStart").floatValue = 870f;
        serialized.FindProperty("maxAlive").intValue = 300;
        serialized.FindProperty("spawnRadiusMin").floatValue = 8f;
        serialized.FindProperty("spawnRadiusMax").floatValue = 12f;
        serialized.FindProperty("bossPrefab").objectReferenceValue = boss;
        serialized.FindProperty("bossHealth").floatValue = 2100f;

        SerializedProperty segments = serialized.FindProperty("waveSegments");
        segments.arraySize = 5;

        ConfigureSegment(
            segments.GetArrayElementAtIndex(0),
            0f, 180f, 35, 70, 77f, 63f, 1f, 1f, 3,
            new[]
            {
                Entry(EnemyRole.Swarm, normal, 70f, 0.11f, 0.4f, 1f, 1f),
                Entry(EnemyRole.Basic, normal, 30f, 0.22f, 0.6f, 1f, 1f)
            }
        );
        ConfigureSegment(
            segments.GetArrayElementAtIndex(1),
            180f, 360f, 70, 130, 135f, 109f, 1.05f, 1.05f, 4,
            new[]
            {
                Entry(EnemyRole.Swarm, normal, 50f, 0.11f, 0.4f, 1f, 1f),
                Entry(EnemyRole.Basic, normal, 35f, 0.22f, 0.6f, 1f, 1f),
                Entry(EnemyRole.Fast, fast, 15f, 0.25f, 1.67f, 1f, 1f)
            }
        );
        ConfigureSegment(
            segments.GetArrayElementAtIndex(2),
            360f, 540f, 130, 190, 220f, 190f, 1.10f, 1.10f, 5,
            new[]
            {
                Entry(EnemyRole.Swarm, normal, 45f, 0.11f, 0.4f, 1f, 1f),
                Entry(EnemyRole.Basic, normal, 30f, 0.22f, 0.6f, 1f, 1f),
                Entry(EnemyRole.Fast, fast, 15f, 0.25f, 1.67f, 1f, 1f),
                Entry(EnemyRole.Medium, tank, 10f, 0.4f, 0.4f, 1f, 3f)
            }
        );
        ConfigureSegment(
            segments.GetArrayElementAtIndex(3),
            540f, 720f, 190, 250, 250f, 210f, 1.18f, 1.18f, 7,
            new[]
            {
                Entry(EnemyRole.Swarm, normal, 55f, 0.11f, 0.4f, 1f, 1f),
                Entry(EnemyRole.Basic, normal, 20f, 0.22f, 0.6f, 1f, 1f),
                Entry(EnemyRole.Fast, fast, 15f, 0.25f, 1.67f, 1f, 1f),
                Entry(EnemyRole.Medium, tank, 8f, 0.4f, 0.4f, 1f, 3f),
                Entry(EnemyRole.Elite, tank, 2f, 1.3f, 0.56f, 1f, 8f)
            }
        );
        ConfigureSegment(
            segments.GetArrayElementAtIndex(4),
            720f, 870f, 250, 300, 360f, 336f, 1.25f, 1.30f, 10,
            new[]
            {
                Entry(EnemyRole.Swarm, normal, 60f, 0.11f, 0.4f, 1f, 1f),
                Entry(EnemyRole.Basic, normal, 15f, 0.22f, 0.6f, 1f, 1f),
                Entry(EnemyRole.Fast, fast, 15f, 0.25f, 1.67f, 1f, 1f),
                Entry(EnemyRole.Medium, tank, 8f, 0.4f, 0.4f, 1f, 3f),
                Entry(EnemyRole.Elite, tank, 2f, 1.3f, 0.56f, 1f, 8f)
            }
        );

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(stage);
    }

    private static void ConfigureSegment(
        SerializedProperty segment,
        float start,
        float end,
        int aliveStart,
        int aliveEnd,
        float spawnBudget,
        float teamExperience,
        float healthMultiplier,
        float damageMultiplier,
        int orbBundleSize,
        EntryValues[] entries
    )
    {
        segment.FindPropertyRelative("startTime").floatValue = start;
        segment.FindPropertyRelative("endTime").floatValue = end;
        segment.FindPropertyRelative("targetAliveStart").intValue = aliveStart;
        segment.FindPropertyRelative("targetAliveEnd").intValue = aliveEnd;
        segment.FindPropertyRelative("spawnBudgetPerMinute").floatValue = spawnBudget;
        segment.FindPropertyRelative("teamExperiencePerMinute").floatValue = teamExperience;
        segment.FindPropertyRelative("healthMultiplier").floatValue = healthMultiplier;
        segment.FindPropertyRelative("damageMultiplier").floatValue = damageMultiplier;
        segment.FindPropertyRelative("maxAttackersPerPlayer").intValue = 5;
        segment.FindPropertyRelative("experienceOrbBundleSize").intValue = orbBundleSize;

        SerializedProperty enemyEntries = segment.FindPropertyRelative("enemies");
        enemyEntries.arraySize = entries.Length;

        for (int index = 0; index < entries.Length; index++)
        {
            EntryValues values = entries[index];
            SerializedProperty entry = enemyEntries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("role").enumValueIndex = (int)values.Role;
            entry.FindPropertyRelative("prefab").objectReferenceValue = values.Prefab;
            entry.FindPropertyRelative("weight").floatValue = values.Weight;
            entry.FindPropertyRelative("healthMultiplier").floatValue = values.Health;
            entry.FindPropertyRelative("damageMultiplier").floatValue = values.Damage;
            entry.FindPropertyRelative("moveSpeedMultiplier").floatValue = values.Speed;
            entry.FindPropertyRelative("experienceWeight").floatValue = values.Experience;
        }
    }

    private static void ApplyMainScene(
        StageConfig stage,
        LevelCurveData levelCurve,
        GameObject normal,
        GameObject boss
    )
    {
        Scene scene = EditorSceneManager.OpenScene(
            "Assets/_Project/Scenes/Main.unity",
            OpenSceneMode.Single
        );
        NetworkStageBootstrap bootstrap =
            Object.FindFirstObjectByType<NetworkStageBootstrap>();
        SerializedObject serialized = new SerializedObject(bootstrap);
        serialized.FindProperty("stageConfig").objectReferenceValue = stage;
        serialized.FindProperty("levelCurve").objectReferenceValue = levelCurve;
        serialized.FindProperty("offlinePlayerPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Players/Player.prefab"
            );
        serialized.FindProperty("enemyPrefab").objectReferenceValue = normal;
        serialized.FindProperty("bossPrefab").objectReferenceValue = boss;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(bootstrap);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ApplyStageTestScene(
        StageConfig stage,
        LevelCurveData levelCurve
    )
    {
        Scene scene = EditorSceneManager.OpenScene(
            "Assets/_Project/Scenes/Tests/StageTest.unity",
            OpenSceneMode.Single
        );
        WaveManager wave = Object.FindFirstObjectByType<WaveManager>();
        SerializedObject waveSerialized = new SerializedObject(wave);
        waveSerialized.FindProperty("stageConfig").objectReferenceValue = stage;
        waveSerialized.ApplyModifiedPropertiesWithoutUndo();

        GameObject runtime = GameObject.Find("StageBalanceRuntime");

        if (runtime == null)
        {
            runtime = new GameObject("StageBalanceRuntime");
        }

        TeamExperience team = runtime.GetComponent<TeamExperience>();

        if (team == null)
        {
            team = runtime.AddComponent<TeamExperience>();
        }

        SerializedObject teamSerialized = new SerializedObject(team);
        teamSerialized.FindProperty("levelCurve").objectReferenceValue = levelCurve;
        teamSerialized.ApplyModifiedPropertiesWithoutUndo();

        if (runtime.GetComponent<StageHudPresenter>() == null)
        {
            runtime.AddComponent<StageHudPresenter>();
        }

        EditorSceneManager.SaveScene(scene);
    }

    private static void ApplyMainMenuScene()
    {
        Scene scene = EditorSceneManager.OpenScene(
            "Assets/_Project/Scenes/MainMenu.unity",
            OpenSceneMode.Single
        );
        StartMenuController controller =
            Object.FindFirstObjectByType<StartMenuController>();
        GameObject buttonObject = GameObject.Find("SinglePlayButton");
        Button button = buttonObject != null ? buttonObject.GetComponent<Button>() : null;

        if (controller != null && button != null && button.onClick.GetPersistentEventCount() == 0)
        {
            UnityEventTools.AddPersistentListener(
                button.onClick,
                controller.OpenSinglePlayerScene
            );
            EditorUtility.SetDirty(button);
        }

        EditorSceneManager.SaveScene(scene);
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);

        if (asset != null)
        {
            return asset;
        }

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(BalanceFolder))
        {
            AssetDatabase.CreateFolder(
                "Assets/_Project/ScriptableObjects",
                "Balance"
            );
        }
    }

    private static EntryValues Entry(
        EnemyRole role,
        GameObject prefab,
        float weight,
        float health,
        float damage,
        float speed,
        float experience
    )
    {
        return new EntryValues
        {
            Role = role,
            Prefab = prefab,
            Weight = weight,
            Health = health,
            Damage = damage,
            Speed = speed,
            Experience = experience
        };
    }

    private struct EntryValues
    {
        public EnemyRole Role;
        public GameObject Prefab;
        public float Weight;
        public float Health;
        public float Damage;
        public float Speed;
        public float Experience;
    }
}
