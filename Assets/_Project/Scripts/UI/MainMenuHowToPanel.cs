using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class MainMenuHowToPanel : MonoBehaviour
{
    private static readonly Color PanelColor = new(0.025f, 0.045f, 0.085f, 0.98f);
    private static readonly Color AccentColor = new(0.08f, 0.72f, 0.88f, 1f);
    private static readonly Color TextColor = new(0.92f, 0.97f, 1f, 1f);
    private static readonly Color MutedTextColor = new(0.66f, 0.78f, 0.88f, 1f);

    private static MainMenuHowToPanel instance;
    private CanvasGroup canvasGroup;
    private RectTransform panelRect;
    private Button closeButton;
    private bool closing;

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

        GameObject root = new(
            "MainMenuHowToRuntime",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(Image),
            typeof(MainMenuHowToPanel)
        );
        root.transform.SetParent(canvas.transform, false);
        root.transform.SetAsLastSibling();
        Stretch((RectTransform)root.transform);
        root.GetComponent<Image>().color = new Color(0f, 0.01f, 0.03f, 0.78f);

        instance = root.GetComponent<MainMenuHowToPanel>();
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

        GameObject panel = new(
            "HowToPanel",
            typeof(RectTransform),
            typeof(Image),
            typeof(Outline)
        );
        panel.transform.SetParent(transform, false);
        panelRect = (RectTransform)panel.transform;
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(780f, 580f);
        panel.GetComponent<Image>().color = PanelColor;
        Outline outline = panel.GetComponent<Outline>();
        outline.effectColor = new Color(0.08f, 0.72f, 0.88f, 0.88f);
        outline.effectDistance = new Vector2(2f, -2f);

        TMP_Text title = CreateText(panel.transform, "Title", "게임 방법", 44f, TextColor);
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -48f), new Vector2(-64f, 64f));
        title.alignment = TextAlignmentOptions.Center;
        title.fontStyle = FontStyles.Bold;

        TMP_Text objective = CreateText(
            panel.transform,
            "Objective",
            "15분 동안 살아남아 마지막에 등장하는 장산범을 처치하세요.",
            24f,
            new Color(1f, 0.8f, 0.3f, 1f)
        );
        SetRect(objective.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -105f), new Vector2(-70f, 48f));
        objective.alignment = TextAlignmentOptions.Center;
        objective.fontStyle = FontStyles.Bold;

        CreateSection(
            panel.transform,
            "조작",
            "이동  WASD / 방향키\n필살기  R     조작 안내  F1\n일시정지  ESC 또는 메뉴 버튼",
            new Vector2(-190f, 15f)
        );
        CreateSection(
            panel.transform,
            "성장",
            "적 처치 → 경험치 획득\n레벨업 → 능력 강화 선택\n화면 하단에서 레벨과 경험치 확인",
            new Vector2(190f, 15f)
        );

        TMP_Text tip = CreateText(
            panel.transform,
            "Tip",
            "공격은 자동으로 발동합니다. 적과 거리를 유지하면서 경험치를 모으세요.",
            22f,
            MutedTextColor
        );
        SetRect(tip.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 112f), new Vector2(680f, 50f));
        tip.alignment = TextAlignmentOptions.Center;

        closeButton = CreateButton(panel.transform, "CloseButton", "확인", new Vector2(0f, -232f), Close);
    }

    private static void CreateSection(Transform parent, string heading, string body, Vector2 position)
    {
        GameObject card = new("Section_" + heading, typeof(RectTransform), typeof(Image));
        card.transform.SetParent(parent, false);
        RectTransform cardRect = (RectTransform)card.transform;
        cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = position;
        cardRect.sizeDelta = new Vector2(340f, 220f);
        card.GetComponent<Image>().color = new Color(0.04f, 0.08f, 0.14f, 0.96f);

        TMP_Text headingText = CreateText(card.transform, "Heading", heading, 28f, AccentColor);
        SetRect(headingText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -36f), new Vector2(-32f, 44f));
        headingText.alignment = TextAlignmentOptions.Center;
        headingText.fontStyle = FontStyles.Bold;

        TMP_Text bodyText = CreateText(card.transform, "Body", body, 21f, TextColor);
        SetRect(bodyText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, -18f), new Vector2(-36f, -72f));
        bodyText.alignment = TextAlignmentOptions.Center;
        bodyText.lineSpacing = 12f;
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        string label,
        Vector2 position,
        UnityEngine.Events.UnityAction action
    )
    {
        GameObject buttonObject = new(
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
        rect.sizeDelta = new Vector2(240f, 62f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.08f, 0.34f, 0.48f, 1f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        button.onClick.AddListener(GameAudioController.PlayButtonClick);

        TMP_Text text = CreateText(buttonObject.transform, "Label", label, 27f, TextColor);
        Stretch(text.rectTransform, 14f, 8f);
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        return button;
    }

    private void Close()
    {
        if (!closing)
        {
            closing = true;
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
            panelRect.localScale = Vector3.Lerp(Vector3.one * 0.94f, Vector3.one, t);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        panelRect.localScale = Vector3.one;
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
        }
    }

    private IEnumerator CloseAnimation()
    {
        canvasGroup.blocksRaycasts = false;
        float elapsed = 0f;
        while (elapsed < 0.16f)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - elapsed / 0.16f;
            yield return null;
        }
        Destroy(gameObject);
    }

    private static TMP_Text CreateText(Transform parent, string name, string value, float size, Color color)
    {
        GameObject textObject = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = value;
        text.fontSize = size;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(15f, size - 6f);
        text.fontSizeMax = size;
        text.color = color;
        text.faceColor = color;
        text.outlineColor = new Color(0f, 0f, 0f, 0.9f);
        text.outlineWidth = 0.1f;
        text.raycastTarget = false;
        return text;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect, float horizontalInset = 0f, float verticalInset = 0f)
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
