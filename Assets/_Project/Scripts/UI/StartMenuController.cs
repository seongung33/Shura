using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartMenuController : MonoBehaviour
{
    private const string GameTitle = "MUGUNG";
    private const string BackgroundResourcePath = "UI/MainMenu/main_menu_city";
    private const string LogoResourcePath = "UI/MainMenu/mugung_logo_transparent";

    private static readonly Color PanelColor = new Color(0.015f, 0.025f, 0.05f, 0.52f);
    private static readonly Color AccentColor = new Color(0.08f, 0.72f, 0.88f, 1f);
    private static readonly Color ButtonColor = new Color(0.08f, 0.34f, 0.48f, 1f);
    private static readonly Color DisabledColor = new Color(0.12f, 0.17f, 0.24f, 1f);
    private static readonly Color PrimaryTextColor = new Color(0.82f, 0.93f, 1f, 1f);
    private static bool introShownThisSession;

    [SerializeField] private GameObject startMenuPanel;
    [SerializeField] private GameObject multiplayerPanel;
    private TMP_Text subtitleText;
    private CanvasGroup menuCanvasGroup;
    private bool skipIntroRequested;

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
        panelImage.color = PanelColor;

        Outline panelOutline = menuRoot.GetComponent<Outline>();
        if (panelOutline == null)
        {
            panelOutline = menuRoot.AddComponent<Outline>();
        }
        panelOutline.effectColor = new Color(0.08f, 0.7f, 0.88f, 0.46f);
        panelOutline.effectDistance = new Vector2(2f, -2f);

        CreateLogo(menuRoot.transform, compactLayout);
        float subtitleY = compactLayout ? 75f : 82f;
        subtitleText = CreateHeading(menuRoot.transform, "Subtitle", "무궁의 밤, 끝까지 살아남아라", new Vector2(0f, subtitleY), compactLayout ? 23f : 27f, new Color(0.75f, 0.86f, 0.95f, 1f));
        CreateAccentLine(menuRoot.transform, compactLayout ? 48f : 54f);

        float firstButtonY = -4f;
        float buttonGap = 72f;

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
                StyleButton(button, tmpLabel, legacyLabel, new Vector2(0f, firstButtonY), ButtonColor, compactLayout);
            }
            else if (text == "멀티 플레이")
            {
                StyleButton(button, tmpLabel, legacyLabel, new Vector2(0f, firstButtonY - buttonGap), ButtonColor, compactLayout);
            }
            else if (text == "설정")
            {
                StyleButton(button, tmpLabel, legacyLabel, new Vector2(0f, firstButtonY - buttonGap * 2f), ButtonColor, compactLayout);
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OpenSettings);
            }
            else if (text == "게임 종료")
            {
                StyleButton(button, tmpLabel, legacyLabel, new Vector2(0f, firstButtonY - buttonGap * 3f), new Color(0.42f, 0.12f, 0.16f, 1f), compactLayout);
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(QuitGame);
            }

            button.onClick.AddListener(GameAudioController.PlayButtonClick);
        }
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

    private static void CreateBackground(GameObject menuRoot)
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

        Image background = new GameObject("CityBackground", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
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
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = background.sprite.rect.width / background.sprite.rect.height;
        }
        background.gameObject.AddComponent<MainMenuBackdropMotion>();

        Image shade = new GameObject("ReadabilityShade", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        shade.transform.SetParent(artwork.transform, false);
        StretchLabel(shade.rectTransform);
        shade.rectTransform.offsetMin = Vector2.zero;
        shade.rectTransform.offsetMax = Vector2.zero;
        shade.color = new Color(0f, 0.015f, 0.035f, 0.3f);
        shade.raycastTarget = false;
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
            CreateHeading(parent, "Title", GameTitle, new Vector2(0f, 260f), 68f, PrimaryTextColor);
            return;
        }

        GameObject logoObject = new GameObject("MugungLogo", typeof(RectTransform), typeof(Image));
        logoObject.transform.SetParent(parent, false);
        RectTransform rect = logoObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, compactLayout ? 205f : 210f);
        rect.sizeDelta = compactLayout
            ? new Vector2(420f, 190f)
            : new Vector2(460f, 200f);

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

    private static void StyleButton(Button button, TMP_Text tmpLabel, Text legacyLabel, Vector2 position, Color normalColor, bool compactLayout)
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
        image.color = normalColor;
        button.interactable = true;
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = normalColor * 1.22f;
        colors.pressedColor = normalColor * 0.78f;
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = DisabledColor;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        if (button.GetComponent<MainMenuButtonMotion>() == null)
        {
            button.gameObject.AddComponent<MainMenuButtonMotion>();
        }

        if (tmpLabel != null)
        {
            StretchLabel(tmpLabel.rectTransform);
            tmpLabel.alignment = TextAlignmentOptions.Center;
            tmpLabel.enableAutoSizing = true;
            tmpLabel.fontSizeMin = 22f;
            tmpLabel.fontSizeMax = 34f;
            tmpLabel.fontStyle = FontStyles.Bold;
            ApplyTextColor(tmpLabel, PrimaryTextColor, 0.14f);
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

    private static void CreateAccentLine(Transform parent, float positionY)
    {
        GameObject line = new GameObject("AccentLine", typeof(RectTransform), typeof(Image));
        line.transform.SetParent(parent, false);
        RectTransform rect = line.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, positionY);
        rect.sizeDelta = new Vector2(380f, 4f);
        line.GetComponent<Image>().color = AccentColor;
    }

    private void ShowLaunchIntro()
    {
        if (introShownThisSession)
        {
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        Sprite logoSprite = Resources.Load<Sprite>(LogoResourcePath);
        Sprite backgroundSprite = Resources.Load<Sprite>(BackgroundResourcePath);
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
        overlayImage.sprite = backgroundSprite;
        overlayImage.color = new Color(0.22f, 0.26f, 0.34f, 1f);
        Button skipButton = overlay.GetComponent<Button>();
        skipButton.transition = Selectable.Transition.None;
        skipButton.onClick.AddListener(() => skipIntroRequested = true);

        GameObject shadeObject = new GameObject(
            "IntroShade",
            typeof(RectTransform),
            typeof(Image)
        );
        shadeObject.transform.SetParent(overlay.transform, false);
        RectTransform shadeRect = shadeObject.GetComponent<RectTransform>();
        shadeRect.anchorMin = Vector2.zero;
        shadeRect.anchorMax = Vector2.one;
        shadeRect.offsetMin = Vector2.zero;
        shadeRect.offsetMax = Vector2.zero;
        shadeObject.GetComponent<Image>().color = new Color(0f, 0.01f, 0.035f, 0.62f);

        GameObject logoObject = new GameObject(
            "IntroLogo",
            typeof(RectTransform),
            typeof(Image)
        );
        logoObject.transform.SetParent(overlay.transform, false);
        RectTransform logoRect = logoObject.GetComponent<RectTransform>();
        logoRect.anchorMin = new Vector2(0.12f, 0.22f);
        logoRect.anchorMax = new Vector2(0.88f, 0.78f);
        logoRect.pivot = new Vector2(0.5f, 0.5f);
        logoRect.offsetMin = Vector2.zero;
        logoRect.offsetMax = Vector2.zero;
        Image logo = logoObject.GetComponent<Image>();
        logo.sprite = logoSprite;
        logo.preserveAspect = true;
        logo.raycastTarget = false;
        AspectRatioFitter logoFitter = logoObject.AddComponent<AspectRatioFitter>();
        logoFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        logoFitter.aspectRatio = logoSprite.rect.width / logoSprite.rect.height;

        CanvasGroup group = overlay.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = true;
        skipIntroRequested = false;
        StartCoroutine(PlayLaunchIntro(group, overlay));
    }

    private IEnumerator PlayLaunchIntro(
        CanvasGroup group,
        GameObject overlay
    )
    {
        yield return Fade(group, 0f, 1f, 0.45f);
        float holdElapsed = 0f;
        while (holdElapsed < 0.9f && !skipIntroRequested)
        {
            holdElapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        yield return Fade(group, 1f, 0f, 0.55f);
        Destroy(overlay);

        if (menuCanvasGroup != null)
        {
            yield return Fade(menuCanvasGroup, 0f, 1f, 0.42f);
            menuCanvasGroup.interactable = true;
            SelectFirstMenuButton();
        }
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
        text.outlineWidth = 0f;

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
