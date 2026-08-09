using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PickupFeedbackPresenter : MonoBehaviour
{
    private static PickupFeedbackPresenter instance;
    private Canvas canvas;

    public static void ShowExperience(Vector3 worldPosition, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        EnsureInstance();
        instance.CreateFeedback(
            worldPosition,
            $"EXP +{amount}",
            new Color(1f, 0.82f, 0.18f),
            false
        );
        GameAudioController.PlayExperiencePickup();
    }

    public static void ShowItem(Vector3 worldPosition, FieldItemType itemType)
    {
        EnsureInstance();
        string label;
        Color color;

        switch (itemType)
        {
            case FieldItemType.Health:
                label = "체력 회복";
                color = new Color(1f, 0.28f, 0.32f);
                break;
            case FieldItemType.Magnet:
                label = "경험치 흡수";
                color = new Color(1f, 0.82f, 0.18f);
                break;
            case FieldItemType.EnemyFreeze:
                label = "적 시간 정지";
                color = new Color(0.28f, 0.82f, 1f);
                break;
            case FieldItemType.SkillCooldownReset:
                label = "필살기 충전";
                color = new Color(0.84f, 0.4f, 1f);
                break;
            default:
                label = "아이템 획득";
                color = Color.white;
                break;
        }

        instance.CreateFeedback(worldPosition, label, color, true);
        GameAudioController.PlayItemPickup(itemType);
    }

    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        instance = new GameObject("PickupFeedbackRuntime")
            .AddComponent<PickupFeedbackPresenter>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildCanvas();
    }

    private void CreateFeedback(Vector3 worldPosition, string label, Color color, bool important)
    {
        Vector2 screenPosition = Camera.main != null
            ? Camera.main.WorldToScreenPoint(worldPosition + Vector3.up * 0.65f)
            : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        GameObject root = new GameObject("PickupFeedback", typeof(RectTransform), typeof(CanvasGroup));
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.position = screenPosition;
        rootRect.sizeDelta = new Vector2(important ? 360f : 220f, important ? 100f : 70f);

        TMP_Text text = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI))
            .GetComponent<TMP_Text>();
        text.transform.SetParent(root.transform, false);
        Stretch(text.rectTransform);
        text.text = label;
        text.fontStyle = FontStyles.Bold;
        text.fontSize = important ? 38f : 27f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.faceColor = color;
        text.outlineWidth = 0.18f;
        text.outlineColor = Color.black;
        text.raycastTarget = false;

        int sparkCount = important ? 8 : 4;
        for (int index = 0; index < sparkCount; index++)
        {
            Image spark = new GameObject("PixelSpark", typeof(RectTransform), typeof(Image))
                .GetComponent<Image>();
            spark.transform.SetParent(root.transform, false);
            RectTransform sparkRect = spark.rectTransform;
            float angle = Mathf.PI * 2f * index / sparkCount;
            sparkRect.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) *
                (important ? 80f : 52f);
            float size = index % 2 == 0 ? 10f : 6f;
            sparkRect.sizeDelta = new Vector2(size, size);
            spark.color = color;
            spark.raycastTarget = false;
        }

        StartCoroutine(Animate(rootRect, root.GetComponent<CanvasGroup>(), important));
    }

    private static IEnumerator Animate(RectTransform rect, CanvasGroup group, bool important)
    {
        float elapsed = 0f;
        float duration = important ? 0.85f : 0.55f;
        Vector2 start = rect.anchoredPosition;
        Vector3 smallScale = Vector3.one * 0.78f;

        while (elapsed < duration && rect != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float enter = Mathf.Clamp01(progress * 5f);
            rect.localScale = Vector3.Lerp(smallScale, Vector3.one, 1f - Mathf.Pow(1f - enter, 3f));
            rect.anchoredPosition = start + Vector2.up * (important ? 82f : 55f) * progress;
            group.alpha = progress < 0.62f
                ? 1f
                : 1f - Mathf.InverseLerp(0.62f, 1f, progress);
            yield return null;
        }

        if (rect != null)
        {
            Destroy(rect.gameObject);
        }
    }

    private void BuildCanvas()
    {
        GameObject canvasObject = new GameObject(
            "PickupFeedbackCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler)
        );
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 65;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
