using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class SubmissionPreflightValidator
{
    private static readonly string[] RequiredScenes =
    {
        "Assets/_Project/Scenes/MainMenu.unity",
        "Assets/_Project/Scenes/CharacterSelect.unity",
        "Assets/_Project/Scenes/MultiPlayerEntry.unity",
        "Assets/_Project/Scenes/MultiPlayerLobby.unity",
        "Assets/_Project/Scenes/Main.unity"
    };

    private static readonly string[] RequiredAssets =
    {
        "Assets/Resources/UI/MainMenu/main_menu_city.png",
        "Assets/Resources/UI/MainMenu/mugung_logo_pixel.png",
        "Assets/_Project/Prefabs/Players/Player.prefab",
        "Assets/_Project/Prefabs/Enemy/Enemy_normal.prefab",
        "Assets/_Project/Prefabs/Enemy/JangsanTiger/BossJangsanTiger.prefab"
    };

    [MenuItem("Shura/검증/제출 전 전체 점검", priority = 1)]
    public static void RunFromMenu()
    {
        bool passed = Run(out List<string> failures, out List<string> notices);

        foreach (string notice in notices)
            Debug.Log($"[제출 점검] {notice}");
        foreach (string failure in failures)
            Debug.LogError($"[제출 점검] {failure}");

        string message = passed
            ? $"필수 자동 점검을 통과했습니다.\n확인 항목: {notices.Count}개\n\n이제 Play Mode 수동 확인을 진행하세요."
            : $"자동 점검에서 {failures.Count}개 문제가 발견됐습니다.\n\nConsole의 빨간 [제출 점검] 항목을 확인하세요.";

        EditorUtility.DisplayDialog(
            passed ? "제출 전 점검 통과" : "제출 전 점검 실패",
            message,
            "확인"
        );
    }

    public static bool Run(out List<string> failures, out List<string> notices)
    {
        failures = new List<string>();
        notices = new List<string>();
        ValidateBuildScenes(failures, notices);
        ValidateRequiredAssets(failures, notices);
        ValidatePrefabScripts(failures, notices);
        return failures.Count == 0;
    }

    private static void ValidateBuildScenes(
        ICollection<string> failures,
        ICollection<string> notices
    )
    {
        HashSet<string> enabledScenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToHashSet();

        foreach (string path in RequiredScenes)
        {
            if (!enabledScenes.Contains(path))
                failures.Add($"필수 씬이 Build Scene List에서 비활성화되었거나 없습니다: {path}");
        }

        if (RequiredScenes.All(enabledScenes.Contains))
            notices.Add($"필수 실행 씬 {RequiredScenes.Length}개가 모두 활성화되어 있습니다.");

        string firstScene = EditorBuildSettings.scenes
            .FirstOrDefault(scene => scene.enabled)?.path;
        if (firstScene != RequiredScenes[0])
            failures.Add($"첫 실행 씬이 MainMenu가 아닙니다: {firstScene ?? "없음"}");
        else
            notices.Add("첫 실행 씬이 MainMenu로 설정되어 있습니다.");
    }

    private static void ValidateRequiredAssets(
        ICollection<string> failures,
        ICollection<string> notices
    )
    {
        int found = 0;
        foreach (string path in RequiredAssets)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) == null)
            {
                failures.Add($"필수 에셋을 찾을 수 없습니다: {path}");
                continue;
            }
            found++;
        }

        if (found == RequiredAssets.Length)
            notices.Add($"필수 UI·플레이어·적 에셋 {found}개를 확인했습니다.");
    }

    private static void ValidatePrefabScripts(
        ICollection<string> failures,
        ICollection<string> notices
    )
    {
        string[] prefabGuids = AssetDatabase.FindAssets(
            "t:Prefab",
            new[] { "Assets/_Project/Prefabs" }
        );
        int missingCount = 0;

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                failures.Add($"프리팹을 불러올 수 없습니다: {path}");
                continue;
            }

            foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
            {
                int childMissing = GameObjectUtility
                    .GetMonoBehavioursWithMissingScriptCount(child.gameObject);
                if (childMissing <= 0)
                    continue;

                missingCount += childMissing;
                failures.Add($"Missing Script {childMissing}개: {path} / {GetHierarchyPath(child)}");
            }
        }

        if (missingCount == 0)
            notices.Add($"프로젝트 프리팹 {prefabGuids.Length}개의 Missing Script가 없습니다.");
    }

    private static string GetHierarchyPath(Transform target)
    {
        string path = target.name;
        Transform parent = target.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }
}
