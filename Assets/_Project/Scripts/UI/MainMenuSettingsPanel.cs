using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class MainMenuSettingsPanel : MonoBehaviour
{
    private const string FullScreenKey = "settings.fullScreen";
    private static readonly Color PanelColor =
        new(0.025f, 0.045f, 0.085f, 0.97f);
    private static readonly Color AccentColor =
        new(0.08f, 0.72f, 0.88f, 1f);
    private static readonly Color TextColor =
        new(0.92f, 0.97f, 1f, 1f);

    private static MainMenuSettingsPanel instance;
    private CanvasGroup canvasGroup;
    private RectTransform panelRect;
    private TMP_Text fullScreenLabel;
    private bool closing;

    public static void ApplySavedDisplaySetting()
    {
        bool defaultValue = Screen.fullScreen;
        bool fullScreen = PlayerPrefs.GetInt(
            FullScreenKey,
            defaultValue ? 1 : 0
        ) == 1;
        Screen.fullScreen = fullScreen;
    }

    public static void Show(Canvas canvas)
    {
        if (canvas == null)
        {
            return;
        }

        if (instance != null)
        {
            instance.gameObject.SetActive(true);
            return;
        }

        GameObject root = new GameObject(
            "MainMenuSettingsRuntime",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(Image),
            typeof(MainMenuSettingsPanel)
        );
        root.transform.SetParent(canvas.transform, false);
        root.transform.SetAsLastSibling();
        Stretch((RectTransform)root.transform);
        root.GetComponent<Image>().color = new Color(0f, 0.01f, 0.03f, 0.72f);

        instance = root.GetComponent<MainMenuSettingsPanel>();
        instance.BuildView();
        instance.StartCoroutine(instance.OpenAnimation());
    }

    private void Update()
    {
        if (!closing && Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Close();
        }
    }

    private void BuildView()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = true;

        GameObject panel = new GameObject(
            "SettingsPanel",
            typeof(RectTransform),
            typeof(Image),
            typeof(Outline)
        );
        panel.transform.SetParent(transform, false);
        panelRect = (RectTransform)panel.transform;
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(600f, 500f);
        panel.GetComponent<Image>().color = PanelColor;
        Outline outline = panel.GetComponent<Outline>();
        outline.effectColor = new Color(0.08f, 0.72f, 0.88f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);

        TMP_Text title = CreateText(panel.transform, "Title", "설정", 48f);
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -56f), new Vector2(-56f, 72f));
        title.alignment = TextAlignmentOptions.Center;
        title.fontStyle = FontStyles.Bold;

        CreateVolumeRow(
            panel.transform,
            "배경 음악",
            120f,
            GameAudioController.MusicVolume,
            GameAudioController.SetMusicVolume
        );
        CreateVolumeRow(
            panel.transform,
            "효과음",
            220f,
            GameAudioController.EffectsVolume,
            GameAudioController.SetEffectsVolume
        );

        Button fullScreenButton = CreateButton(
            panel.transform,
            "FullScreenButton",
            string.Empty,
            new Vector2(0f, -85f),
            new Vector2(440f, 64f),
            ToggleFullScreen
        );
        fullScreenLabel = fullScreenButton.GetComponentInChildren<TMP_Text>();
        RefreshFullScreenLabel();

        CreateButton(
            panel.transform,
            "CloseButton",
            "닫기",
            new Vector2(0f, -185f),
            new Vector2(220f, 62f),
            Close
        );
    }

    private void CreateVolumeRow(
        Transform parent,
        string label,
        float top,
        float initialValue,
        UnityEngine.Events.UnityAction<float> onChanged
    )
    {
        TMP_Text labelText = CreateText(
            parent,
            label + "Label",
            label,
            26f
        );
        SetRect(labelText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(132f, -top), new Vector2(190f, 46f));

        TMP_Text valueText = CreateText(
            parent,
            label + "Value",
            Mathf.RoundToInt(initialValue * 100f) + "%",
            22f
        );
        SetRect(valueText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-88f, -top), new Vector2(92f, 42f));
        valueText.alignment = TextAlignmentOptions.Center;

        Slider slider = CreateSlider(parent, label + "Slider");
        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        SetRect(sliderRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(40f, -top), new Vector2(300f, 42f));
        slider.value = initialValue;
        slider.onValueChanged.AddListener(value =>
        {
            onChanged(value);
            valueText.text = Mathf.RoundToInt(value * 100f) + "%";
        });
    }

    private static Slider CreateSlider(Transform parent, string name)
    {
        GameObject root = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Slider)
        );
        root.transform.SetParent(parent, false);

        Image background = CreateImage(root.transform, "Background", new Color(0.08f, 0.12f, 0.19f, 1f));
        Stretch(background.rectTransform, 0f, 12f);

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(root.transform, false);
        Stretch((RectTransform)fillArea.transform, 8f, 15f);
        Image fill = CreateImage(fillArea.transform, "Fill", AccentColor);
        Stretch(fill.rectTransform);

        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(root.transform, false);
        Stretch((RectTransform)handleArea.transform, 10f, 8f);
        Image handle = CreateImage(handleArea.transform, "Handle", TextColor);
        handle.rectTransform.sizeDelta = new Vector2(26f, 38f);

        Slider slider = root.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        return slider;
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        string label,
        Vector2 position,
        Vector2 size,
        UnityEngine.Events.UnityAction action
    )
    {
        GameObject buttonObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(MainMenuButtonMotion)
        );
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)buttonObject.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.08f, 0.34f, 0.48f, 1f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        button.onClick.AddListener(GameAudioController.PlayButtonClick);

        TMP_Text text = CreateText(buttonObject.transform, "Label", label, 26f);
        Stretch(text.rectTransform, 14f, 8f);
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        return button;
    }

    private void ToggleFullScreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
        PlayerPrefs.SetInt(FullScreenKey, Screen.fullScreen ? 1 : 0);
        PlayerPrefs.Save();
        RefreshFullScreenLabel();
    }

    private void RefreshFullScreenLabel()
    {
        if (fullScreenLabel != null)
        {
            fullScreenLabel.text = Screen.fullScreen
                ? "화면 모드   전체 화면"
                : "화면 모드   창 모드";
        }
    }

    private void Close()
    {
        if (!closing)
        {
            closing = true;
            GameAudioController.SaveSettings();
            StartCoroutine(CloseAnimation());
        }
    }

    private IEnumerator OpenAnimation()
    {
        panelRect.localScale = Vector3.one * 0.94f;
        float elapsed = 0f;
        while (elapsed < 0.22f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / 0.22f);
            canvasGroup.alpha = t;
            panelRect.localScale = Vector3.Lerp(
                Vector3.one * 0.94f,
                Vector3.one,
                t
            );
            yield return null;
        }
        canvasGroup.alpha = 1f;
        panelRect.localScale = Vector3.one;
    }

    private IEnumerator CloseAnimation()
    {
        canvasGroup.blocksRaycasts = false;
        float elapsed = 0f;
        while (elapsed < 0.16f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / 0.16f;
            canvasGroup.alpha = 1f - t;
            yield return null;
        }
        Destroy(gameObject);
    }

    private static TMP_Text CreateText(
        Transform parent,
        string name,
        string value,
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
        text.text = value;
        text.fontSize = size;
        text.color = TextColor;
        text.faceColor = TextColor;
        text.outlineColor = new Color(0f, 0f, 0f, 0.9f);
        text.outlineWidth = 0.12f;
        text.raycastTarget = false;
        return text;
    }

    private static Image CreateImage(
        Transform parent,
        string name,
        Color color
    )
    {
        GameObject imageObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Image)
        );
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 position,
        Vector2 size
    )
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(
        RectTransform rect,
        float horizontalInset = 0f,
        float verticalInset = 0f
    )
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(horizontalInset, verticalInset);
        rect.offsetMax = new Vector2(-horizontalInset, -verticalInset);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
