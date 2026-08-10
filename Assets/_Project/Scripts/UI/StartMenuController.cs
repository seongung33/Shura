using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartMenuController : MonoBehaviour
{
    private const string GameTitle = "무궁";
    private const string BackgroundResourcePath = "UI/MainMenu/main_menu_city";
    private const string LogoResourcePath = "UI/MainMenu/mugung_logo_pixel_transparent";

    private static readonly Color PrimaryTextColor = new Color(0.96f, 0.98f, 1f, 1f);
    private static readonly Color FocusTextColor = new Color(0.2f, 0.95f, 0.92f, 1f);
    private static readonly Color FocusBackgroundColor = new Color(0.04f, 0.72f, 0.72f, 0.16f);
    private static bool introShownThisSession;

    [SerializeField] private GameObject startMenuPanel;
    [SerializeField] private GameObject multiplayerPanel;
    [Header("Readability")]
    [SerializeField, Range(0f, 0.7f)] private float backgroundOverlayAlpha = 0.3f;
    [SerializeField, Range(0f, 0.6f)] private float centerShadeAlpha = 0.24f;
    private CanvasGroup menuCanvasGroup;
    private bool skipIntroRequested;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetIntroSession()
    {
        introShownThisSession = false;
    }

    private void Awake()
    {
        GameAudioController.EnsureExists();
        MainMenuSettingsPanel.ApplySavedDisplaySetting();
        ApplyVisualTheme();
        ShowLaunchIntro();
    }

    public void OpenMultiplayer()
    {
        startMenuPanel.SetActive(false);
        multiplayerPanel.SetActive(true);
    }
    public void BackToStartMenu()
    {
        multiplayerPanel.SetActive(false);
        startMenuPanel.SetActive(true);
    }
    public void OpenMultiplayerScene()
    {
        SceneManager.LoadScene("MultiPlayerEntry");
    }
    public void OpenSinglePlayerScene()
    {
        SceneManager.LoadScene("CharacterSelect");
    }
    public void OpenMainMenuScene()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ApplyVisualTheme()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = new Color(0.018f, 0.03f, 0.065f, 1f);
        }

        GameObject menuRoot = ResolveMenuRoot();
        if (menuRoot == null)
        {
            return;
        }

        Canvas canvas = menuRoot.GetComponentInParent<Canvas>();
        ConfigureCanvas(canvas);

        CreateBackground(menuRoot);

        bool compactLayout = (float)Screen.width / Mathf.Max(1f, Screen.height) < 1.45f;
        RectTransform panelRect = menuRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = compactLayout
            ? new Vector2(500f, 650f)
            : new Vector2(560f, 650f);

        Image panelImage = menuRoot.GetComponent<Image>();
        if (panelImage == null)
        {
            panelImage = menuRoot.AddComponent<Image>();
        }
        panelImage.color = Color.clear;
        panelImage.raycastTarget = false;

        Outline panelOutline = menuRoot.GetComponent<Outline>();
        if (panelOutline != null)
        {
            panelOutline.enabled = false;
        }

        CreateLogo(menuRoot.transform, compactLayout);
        Transform oldSubtitle = menuRoot.transform.Find("Subtitle");
        if (oldSubtitle != null)
        {
            oldSubtitle.gameObject.SetActive(false);
        }
        CreateHowToButton(menuRoot.transform);

        float firstButtonY = -40f;
        float buttonGap = 52f;

        Button[] buttons = menuRoot.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            TMP_Text tmpLabel = button.GetComponentInChildren<TMP_Text>(true);
            Text legacyLabel = button.GetComponentInChildren<Text>(true);
            string text = tmpLabel != null
                ? tmpLabel.text.Trim()
                : legacyLabel != null ? legacyLabel.text.Trim() : string.Empty;
            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            if (text == "싱글 플레이")
            {
                StyleButton(button, tmpLabel, legacyLabel, new Vector2(0f, firstButtonY), compactLayout);
            }
            else if (text == "멀티 플레이")
            {
                StyleButton(button, tmpLabel, legacyLabel, new Vector2(0f, firstButtonY - buttonGap), compactLayout);
            }
            else if (text == "설정")
            {
                StyleButton(button, tmpLabel, legacyLabel, new Vector2(0f, firstButtonY - buttonGap * 3f), compactLayout);
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OpenSettings);
            }
            else if (text == "게임 방법")
            {
                StyleButton(button, tmpLabel, legacyLabel, new Vector2(0f, firstButtonY - buttonGap * 2f), compactLayout);
            }
            else if (text == "게임 종료")
            {
                StyleButton(button, tmpLabel, legacyLabel, new Vector2(0f, firstButtonY - buttonGap * 4f), compactLayout);
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(QuitGame);
            }

            button.onClick.AddListener(GameAudioController.PlayButtonClick);
        }
    }

    private void CreateHowToButton(Transform parent)
    {
        if (parent.Find("HowToButtonRuntime") != null)
        {
            return;
        }

        GameObject buttonObject = new(
            "HowToButtonRuntime",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button)
        );
        buttonObject.transform.SetParent(parent, false);
        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(OpenHowTo);

        TMP_Text label = CreateHeading(
            buttonObject.transform,
            "Label",
            "게임 방법",
            Vector2.zero,
            28f,
            PrimaryTextColor
        );
        StretchLabel(label.rectTransform);
    }

    private static void ConfigureCanvas(Canvas canvas)
    {
        if (canvas == null)
        {
            return;
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;
    }

    private void CreateBackground(GameObject menuRoot)
    {
        Canvas canvas = menuRoot.GetComponentInParent<Canvas>();
        if (canvas == null || canvas.transform.Find("MainMenuArtworkRuntime") != null)
        {
            return;
        }

        GameObject artwork = new GameObject("MainMenuArtworkRuntime", typeof(RectTransform));
        artwork.transform.SetParent(canvas.transform, false);
        artwork.transform.SetAsFirstSibling();
        StretchLabel(artwork.GetComponent<RectTransform>());
        RectTransform artworkRect = artwork.GetComponent<RectTransform>();
        artworkRect.offsetMin = Vector2.zero;
        artworkRect.offsetMax = Vector2.zero;

        Image background = new GameObject("MainMenuCityBackground", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        background.transform.SetParent(artwork.transform, false);
        StretchLabel(background.rectTransform);
        background.rectTransform.offsetMin = Vector2.zero;
        background.rectTransform.offsetMax = Vector2.zero;
        background.sprite = Resources.Load<Sprite>(BackgroundResourcePath);
        background.color = Color.white;
        background.raycastTarget = false;
        if (background.sprite != null)
        {
            AspectRatioFitter fitter = background.gameObject.AddComponent<AspectRatioFitter>();
            background.rectTransform.localScale = new Vector3(1.1f, 1.1f, 1f);
            background.rectTransform.anchoredPosition = new Vector2(-40f, 0f);

            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = background.sprite.rect.width / background.sprite.rect.height;
        }

        // Keep the supplied artwork completely still on the main menu.
        MainMenuBackdropMotion motion = background.GetComponent<MainMenuBackdropMotion>();
        if (motion != null)
        {
            motion.enabled = false;
        }

        Image overlay = new GameObject(
            "BackgroundReadabilityOverlay",
            typeof(RectTransform),
            typeof(Image)
        ).GetComponent<Image>();
        overlay.transform.SetParent(artwork.transform, false);
        StretchLabel(overlay.rectTransform);
        overlay.rectTransform.offsetMin = Vector2.zero;
        overlay.rectTransform.offsetMax = Vector2.zero;
        overlay.color = new Color(0f, 0f, 0f, backgroundOverlayAlpha);
        overlay.raycastTarget = false;

        MainMenuCenterShade centerShade = new GameObject(
            "MenuCenterShade",
            typeof(RectTransform),
            typeof(MainMenuCenterShade)
        ).GetComponent<MainMenuCenterShade>();
        centerShade.transform.SetParent(artwork.transform, false);
        StretchLabel(centerShade.rectTransform);
        centerShade.rectTransform.offsetMin = Vector2.zero;
        centerShade.rectTransform.offsetMax = Vector2.zero;
        centerShade.Configure(centerShadeAlpha, new Vector2(0.58f, 0.96f));
    }

    private static void CreateLogo(Transform parent, bool compactLayout)
    {
        Transform oldTitle = parent.Find("Title");
        if (oldTitle != null)
        {
            oldTitle.gameObject.SetActive(false);
        }

        Sprite logoSprite = Resources.Load<Sprite>(LogoResourcePath);
        if (logoSprite == null)
        {
            CreateHeading(parent, "Title", GameTitle, new Vector2(0f, 110f), 68f, PrimaryTextColor);
            return;
        }

        GameObject logoObject = new GameObject("MugungLogo", typeof(RectTransform), typeof(Image));
        logoObject.transform.SetParent(parent, false);
        RectTransform rect = logoObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 110f);
        rect.sizeDelta = compactLayout
            ? new Vector2(380f, 170f)
            : new Vector2(400f, 175f);

        Image logo = logoObject.GetComponent<Image>();
        logo.sprite = logoSprite;
        logo.preserveAspect = true;
        logo.raycastTarget = false;
    }

    private GameObject ResolveMenuRoot()
    {
        if (startMenuPanel == null)
        {
            return null;
        }

        // 기존 씬에는 이 필드가 패널이 아니라 SinglePlayButton에 연결되어 있다.
        // 씬 직렬화를 깨뜨리지 않고 실제 공통 부모(StartMenuPanel)를 사용한다.
        if (startMenuPanel.GetComponent<Button>() != null && startMenuPanel.transform.parent != null)
        {
            return startMenuPanel.transform.parent.gameObject;
        }

        return startMenuPanel;
    }

    private void OpenSettings()
    {
        MainMenuSettingsPanel.Show(GetComponentInParent<Canvas>());
    }

    private void OpenHowTo()
    {
        MainMenuHowToPanel.Show(GetComponentInParent<Canvas>());
    }

    private static void StyleButton(Button button, TMP_Text tmpLabel, Text legacyLabel, Vector2 position, bool compactLayout)
    {
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = compactLayout
            ? new Vector2(380f, 56f)
            : new Vector2(400f, 64f);

        Image image = button.GetComponent<Image>();
        image.color = Color.clear;
        button.interactable = true;
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.clear;
        colors.highlightedColor = Color.clear;
        colors.pressedColor = Color.clear;
        colors.selectedColor = Color.clear;
        colors.disabledColor = Color.clear;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        MainMenuButtonMotion motion = button.GetComponent<MainMenuButtonMotion>();
        if (motion == null)
        {
            motion = button.gameObject.AddComponent<MainMenuButtonMotion>();
        }
        motion.ConfigureFocusColors(
            Color.clear,
            FocusBackgroundColor,
            false,
            tmpLabel,
            PrimaryTextColor,
            FocusTextColor
        );
        button.transition = Selectable.Transition.None;

        if (tmpLabel != null)
        {
            StretchLabel(tmpLabel.rectTransform);
            tmpLabel.alignment = TextAlignmentOptions.Center;
            tmpLabel.enableAutoSizing = true;
            tmpLabel.fontSizeMin = 22f;
            tmpLabel.fontSizeMax = 34f;
            tmpLabel.fontStyle = FontStyles.Bold;
            ApplyTextColor(tmpLabel, PrimaryTextColor, 0.1f);
        }

        if (legacyLabel != null)
        {
            StretchLabel(legacyLabel.rectTransform);
            legacyLabel.alignment = TextAnchor.MiddleCenter;
            legacyLabel.resizeTextForBestFit = true;
            legacyLabel.resizeTextMinSize = 22;
            legacyLabel.resizeTextMaxSize = 34;
            legacyLabel.fontStyle = FontStyle.Bold;
            legacyLabel.color = PrimaryTextColor;
        }
    }

    private static TMP_Text CreateHeading(Transform parent, string name, string value, Vector2 position, float size, Color color)
    {
        Transform existing = parent.Find(name);
        GameObject textObject = existing != null
            ? existing.gameObject
            : new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(560f, size + 28f);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = value;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.fontSize = size;
        ApplyTextColor(text, color, name == "Title" ? 0.2f : 0.1f);
        return text;
    }

    private void ShowLaunchIntro()
    {
        if (introShownThisSession)
        {
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        Sprite logoSprite = Resources.Load<Sprite>(LogoResourcePath);
        if (canvas == null || logoSprite == null)
        {
            return;
        }

        introShownThisSession = true;
        GameObject menuRoot = ResolveMenuRoot();
        if (menuRoot != null)
        {
            menuCanvasGroup = menuRoot.GetComponent<CanvasGroup>();
            if (menuCanvasGroup == null)
            {
                menuCanvasGroup = menuRoot.AddComponent<CanvasGroup>();
            }
            menuCanvasGroup.alpha = 0f;
            menuCanvasGroup.interactable = false;
        }

        GameObject overlay = new GameObject(
            "LaunchIntroRuntime",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(Image),
            typeof(Button)
        );
        overlay.transform.SetParent(canvas.transform, false);
        overlay.transform.SetAsLastSibling();
        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = overlay.GetComponent<Image>();
        overlayImage.color = Color.clear;
        Button skipButton = overlay.GetComponent<Button>();
        skipButton.transition = Selectable.Transition.None;
        skipButton.onClick.AddListener(() => skipIntroRequested = true);

        GameObject logoObject = new GameObject(
            "IntroLogo",
            typeof(RectTransform),
            typeof(Image)
        );
        logoObject.transform.SetParent(overlay.transform, false);
        RectTransform logoRect = logoObject.GetComponent<RectTransform>();
        logoRect.anchorMin = new Vector2(0.5f, 0.5f);
        logoRect.anchorMax = new Vector2(0.5f, 0.5f);
        logoRect.pivot = new Vector2(0.5f, 0.5f);
        logoRect.anchoredPosition = Vector2.zero;
        logoRect.sizeDelta = new Vector2(660f, 440f);
        Image logo = logoObject.GetComponent<Image>();
        logo.sprite = logoSprite;
        logo.color = new Color(1f, 1f, 1f, 0f);
        logo.preserveAspect = true;
        logo.raycastTarget = false;
        logoRect.localScale = Vector3.one * 0.86f;

        RectTransform targetLogoRect = menuRoot != null
            ? menuRoot.transform.Find("MugungLogo") as RectTransform
            : null;
        if (targetLogoRect != null)
        {
            targetLogoRect.gameObject.SetActive(false);
        }

        CanvasGroup group = overlay.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = true;
        skipIntroRequested = false;
        StartCoroutine(PlayLaunchIntro(
            group,
            overlay,
            overlayRect,
            logo,
            logoRect,
            targetLogoRect
        ));
    }

    private IEnumerator PlayLaunchIntro(
        CanvasGroup group,
        GameObject overlay,
        RectTransform overlayRect,
        Image logo,
        RectTransform logoRect,
        RectTransform targetLogoRect
    )
    {
        yield return Fade(group, 0f, 1f, 0.45f);

        GameAudioController.PlayLogoReveal();
        float logoElapsed = 0f;
        const float logoDuration = 0.42f;
        while (logoElapsed < logoDuration && !skipIntroRequested)
        {
            logoElapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, logoElapsed / logoDuration);
            logo.color = new Color(1f, 1f, 1f, progress);
            logoRect.localScale = Vector3.one * Mathf.Lerp(0.86f, 1f, progress);
            yield return null;
        }
        logo.color = Color.white;
        logoRect.localScale = Vector3.one;

        float holdElapsed = 0f;
        while (holdElapsed < 0.72f && !skipIntroRequested)
        {
            holdElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Vector2 startPosition = logoRect.anchoredPosition;
        Vector2 startSize = logoRect.sizeDelta;
        Vector2 targetPosition = startPosition;
        Vector2 targetSize = startSize * 0.5f;
        if (targetLogoRect != null)
        {
            GetRectInLocalSpace(targetLogoRect, overlayRect, out targetPosition, out targetSize);
        }

        float morphElapsed = 0f;
        const float morphDuration = 0.9f;
        while (morphElapsed < morphDuration && !skipIntroRequested)
        {
            morphElapsed += Time.unscaledDeltaTime;
            float linearProgress = Mathf.Clamp01(morphElapsed / morphDuration);
            float progress = linearProgress * linearProgress * (3f - 2f * linearProgress);
            logoRect.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, progress);
            logoRect.sizeDelta = Vector2.LerpUnclamped(startSize, targetSize, progress);
            logoRect.localScale = Vector3.one;

            if (menuCanvasGroup != null)
            {
                menuCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, linearProgress);
            }
            yield return null;
        }

        logoRect.anchoredPosition = targetPosition;
        logoRect.sizeDelta = targetSize;
        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.alpha = 1f;
        }

        Destroy(overlay);

        if (targetLogoRect != null)
        {
            targetLogoRect.gameObject.SetActive(true);
        }

        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.interactable = true;
            SelectFirstMenuButton();
        }
    }

    private static void GetRectInLocalSpace(
        RectTransform source,
        RectTransform destination,
        out Vector2 center,
        out Vector2 size
    )
    {
        Vector3[] worldCorners = new Vector3[4];
        source.GetWorldCorners(worldCorners);
        Vector3 bottomLeft = destination.InverseTransformPoint(worldCorners[0]);
        Vector3 topRight = destination.InverseTransformPoint(worldCorners[2]);
        center = (bottomLeft + topRight) * 0.5f;
        size = new Vector2(
            Mathf.Abs(topRight.x - bottomLeft.x),
            Mathf.Abs(topRight.y - bottomLeft.y)
        );
    }

    private void SelectFirstMenuButton()
    {
        EventSystem eventSystem = EventSystem.current;
        GameObject menuRoot = ResolveMenuRoot();
        Button firstButton = menuRoot != null
            ? menuRoot.GetComponentInChildren<Button>(true)
            : null;
        if (eventSystem != null && firstButton != null && firstButton.interactable)
        {
            eventSystem.SetSelectedGameObject(firstButton.gameObject);
        }
    }

    private static IEnumerator Fade(
        CanvasGroup group,
        float from,
        float to,
        float duration
    )
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        group.alpha = to;
    }

    private static void ApplyTextColor(TMP_Text text, Color color, float outlineWidth)
    {
        text.color = color;
        text.faceColor = color;
        text.enableVertexGradient = false;
        text.colorGradient = new VertexGradient(color);
        text.outlineColor = new Color32(0, 0, 0, 230);
        text.outlineWidth = Mathf.Clamp(outlineWidth, 0f, 0.15f);

        Shadow shadow = text.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = text.gameObject.AddComponent<Shadow>();
        }
        shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
        shadow.effectDistance = new Vector2(2f, -2f);
    }

    private static void StretchLabel(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(16f, 8f);
        rect.offsetMax = new Vector2(-16f, -8f);
    }
}
