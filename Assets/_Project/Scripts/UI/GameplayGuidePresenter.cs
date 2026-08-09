using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Main 씬 진입 직후 최소 조작법을 안내하고 F1로 다시 펼친다.
/// 씬 YAML을 수정하지 않도록 런타임에만 생성한다.
/// </summary>
public sealed class GameplayGuidePresenter : MonoBehaviour
{
    private const string GameplaySceneName = "Main";
    private const float InitialDisplaySeconds = 8f;

    private static bool sceneCallbackRegistered;

    private GameObject detailPanel;
    private GameObject compactPanel;
    private TMP_Text compactLabel;
    private float collapseAt;
    private bool detailsVisible;
    private Coroutine transition;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneCallback()
    {
        if (sceneCallbackRegistered)
        {
            return;
        }

        SceneManager.sceneLoaded += HandleSceneLoaded;
        sceneCallbackRegistered = true;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != GameplaySceneName ||
            FindFirstObjectByType<GameplayGuidePresenter>() != null)
        {
            return;
        }

        new GameObject("GameplayGuidePresenter")
            .AddComponent<GameplayGuidePresenter>();
    }

    private void Awake()
    {
        CreateView();
        SetDetailsVisible(true, true);
        collapseAt = Time.unscaledTime + InitialDisplaySeconds;
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.f1Key.wasPressedThisFrame)
        {
            SetDetailsVisible(!detailsVisible);
            return;
        }

        if (detailsVisible && Time.unscaledTime >= collapseAt)
        {
            SetDetailsVisible(false);
        }
    }

    private void CreateView()
    {
        GameObject canvasObject = new GameObject(
            "GameplayGuideCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        detailPanel = CreatePanel(canvasObject.transform, "GuidePanel");
        RectTransform panelRect = detailPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-28f, -112f);
        panelRect.sizeDelta = new Vector2(430f, 190f);

        TMP_Text title = CreateText(detailPanel.transform, "Title", 28f);
        title.text = "조작 안내";
        title.fontStyle = FontStyles.Bold;
        SetRect(title.rectTransform, 18f, -14f, -18f, -52f);

        TMP_Text controls = CreateText(detailPanel.transform, "Controls", 22f);
        controls.text =
            "이동   WASD / 방향키\n" +
            "전투   기본 공격·스킬 자동 발동\n" +
            "필살기   R     메뉴   ESC\n" +
            "도움말 열기·닫기   F1";
        controls.lineSpacing = 6f;
        SetRect(controls.rectTransform, 18f, -55f, -18f, -174f);

        compactPanel = CreatePanel(
            canvasObject.transform,
            "CompactGuide"
        );
        RectTransform compactRect = compactPanel.GetComponent<RectTransform>();
        compactRect.anchorMin = new Vector2(1f, 1f);
        compactRect.anchorMax = new Vector2(1f, 1f);
        compactRect.pivot = new Vector2(1f, 1f);
        compactRect.anchoredPosition = new Vector2(-28f, -112f);
        compactRect.sizeDelta = new Vector2(210f, 48f);

        compactLabel = CreateText(compactPanel.transform, "Label", 20f);
        compactLabel.text = "F1  조작 도움말";
        compactLabel.alignment = TextAlignmentOptions.Center;
        SetRect(compactLabel.rectTransform, 8f, -4f, -8f, -44f);
    }

    private void SetDetailsVisible(bool visible, bool instant = false)
    {
        detailsVisible = visible;

        if (transition != null)
        {
            StopCoroutine(transition);
        }

        CanvasGroup detailGroup = detailPanel.GetComponent<CanvasGroup>();
        CanvasGroup compactGroup = compactPanel.GetComponent<CanvasGroup>();
        detailPanel.SetActive(true);
        compactPanel.SetActive(true);

        if (instant)
        {
            detailGroup.alpha = visible ? 1f : 0f;
            compactGroup.alpha = visible ? 0f : 1f;
            detailPanel.SetActive(visible);
            compactPanel.SetActive(!visible);
            return;
        }

        transition = StartCoroutine(
            CrossFadeGuide(detailGroup, compactGroup, visible)
        );
    }

    private IEnumerator CrossFadeGuide(
        CanvasGroup detailGroup,
        CanvasGroup compactGroup,
        bool showDetails
    )
    {
        float detailStart = detailGroup.alpha;
        float compactStart = compactGroup.alpha;
        const float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            detailGroup.alpha = Mathf.Lerp(
                detailStart,
                showDetails ? 1f : 0f,
                t
            );
            compactGroup.alpha = Mathf.Lerp(
                compactStart,
                showDetails ? 0f : 1f,
                t
            );
            yield return null;
        }

        detailGroup.alpha = showDetails ? 1f : 0f;
        compactGroup.alpha = showDetails ? 0f : 1f;
        detailPanel.SetActive(showDetails);
        compactPanel.SetActive(!showDetails);
        transition = null;
    }

    private static GameObject CreatePanel(Transform parent, string name)
    {
        GameObject panel = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Image),
            typeof(Outline),
            typeof(CanvasGroup)
        );
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color =
            new Color(0.025f, 0.045f, 0.09f, 0.9f);

        Outline outline = panel.GetComponent<Outline>();
        outline.effectColor = new Color(0.1f, 0.72f, 0.9f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);
        return panel;
    }

    private static TMP_Text CreateText(
        Transform parent,
        string name,
        float size
    )
    {
        GameObject textObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );
        textObject.transform.SetParent(parent, false);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.fontSize = size;
        Color textColor = new Color(0.92f, 0.97f, 1f, 1f);
        text.color = textColor;
        text.faceColor = textColor;
        text.outlineColor = new Color(0f, 0f, 0f, 0.95f);
        text.outlineWidth = 0.16f;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.raycastTarget = false;
        return text;
    }

    private static void SetRect(
        RectTransform rect,
        float left,
        float top,
        float right,
        float bottom
    )
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, top);
    }
}
