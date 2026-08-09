using System;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class StageHudBossValidator
{
    [MenuItem("Shura/Validate/Stage HUD Boss Bar")]
    public static void Run()
    {
        GameObject presenterObject = null;
        GameObject bossObject = null;

        try
        {
            presenterObject = new GameObject("StageHudBossValidation");
            StageHudPresenter presenter = presenterObject.AddComponent<StageHudPresenter>();
            Invoke(presenter, "CreateView");

            GameObject bossPanel = GetField<GameObject>(presenter, "bossPanel");
            Require(!bossPanel.activeSelf, "보스가 없을 때 체력바가 숨겨져야 합니다.");

            bossObject = new GameObject("ValidationBoss");
            EnemyHealth boss = bossObject.AddComponent<EnemyHealth>();
            SetField(boss, "isBoss", true);
            Invoke(boss, "Awake");

            Invoke(presenter, "RefreshBoss");
            Invoke(presenter, "UpdateBoss");
            Require(bossPanel.activeSelf, "보스 등장 시 체력바가 표시되어야 합니다.");

            TMP_Text healthText = GetField<TMP_Text>(presenter, "bossHealthText");
            Image healthFill = GetField<Image>(presenter, "bossHealthFill");
            Require(healthText.text.Contains("장산범"), "보스 이름이 표시되어야 합니다.");
            Require(Mathf.Approximately(healthFill.rectTransform.anchorMax.x, 1f), "최초 보스 체력이 100%여야 합니다.");

            SetField(boss, "localHealth", 40f);
            Invoke(presenter, "UpdateBoss");
            Require(healthText.text.Contains("40 / 100"), "보스 체력 수치가 갱신되어야 합니다.");
            Require(Mathf.Approximately(healthFill.rectTransform.anchorMax.x, 0.4f), "보스 체력 막대 비율이 갱신되어야 합니다.");

            SetField(boss, "localHealth", 0f);
            Invoke(presenter, "RefreshBoss");
            Invoke(presenter, "UpdateBoss");
            Require(!bossPanel.activeSelf, "보스 처치 후 체력바가 숨겨져야 합니다.");

            Debug.Log("[StageHudBossValidator] PASS: hidden, shown, health update, hidden on defeat");
        }
        finally
        {
            if (bossObject != null)
            {
                UnityEngine.Object.DestroyImmediate(bossObject);
            }

            if (presenterObject != null)
            {
                UnityEngine.Object.DestroyImmediate(presenterObject);
            }
        }
    }

    public static void RunBatch()
    {
        Run();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static T GetField<T>(object target, string fieldName)
    {
        FieldInfo field = FindField(target.GetType(), fieldName);
        return (T)field.GetValue(target);
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = FindField(target.GetType(), fieldName);
        field.SetValue(target, value);
    }

    private static FieldInfo FindField(Type type, string fieldName)
    {
        FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new MissingFieldException(type.Name, fieldName);
        }
        return field;
    }

    private static void Invoke(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
        {
            throw new MissingMethodException(target.GetType().Name, methodName);
        }
        method.Invoke(target, null);
    }
}
