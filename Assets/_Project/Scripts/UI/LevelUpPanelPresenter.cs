using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class LevelUpPanelPresenter : MonoBehaviour
{
    private const int CardCount = 3;

    private readonly TMP_Text[] cardTexts = new TMP_Text[CardCount];
    private readonly Button[] cardButtons = new Button[CardCount];

    private ILevelUpChoiceSource progression;
    private GameObject canvasObject;
    private GameObject panelObject;
    private TMP_Text levelText;
    private TMP_Text timerText;
    private TMP_Text statusText;
    private int displayedSessionId = -1;
    private bool requestPending;

    public void Bind(ILevelUpChoiceSource configuredProgression)
    {
        progression = configuredProgression;
        CreateUi();
        Refresh();
    }

    public void Refresh()
    {
        if (progression == null || panelObject == null)
        {
            return;
        }

        bool visible = progression.ChoiceActive;
        panelObject.SetActive(visible);

        if (!visible)
        {
            requestPending = false;
            return;
        }

        if (displayedSessionId != progression.ChoiceSessionId)
        {
            displayedSessionId = progression.ChoiceSessionId;
            requestPending = false;
        }

        levelText.text = $"TEAM LEVEL {progression.ChoiceTeamLevel}";

        for (int index = 0; index < CardCount; index++)
        {
            bool hasCard = index < progression.CandidateCount;
            cardButtons[index].gameObject.SetActive(hasCard);

            if (!hasCard)
            {
                continue;
            }

            cardTexts[index].text = FormatCard(
                progression.GetCandidate(index)
            );
            cardButtons[index].interactable =
                !progression.HasSelected && !requestPending;
        }

        RefreshTimerAndStatus();
    }

    private void Update()
    {
        if (progression == null || !progression.ChoiceActive)
        {
            return;
        }

        RefreshTimerAndStatus();
    }

    private void OnDestroy()
    {
        if (canvasObject != null)
        {
            Destroy(canvasObject);
        }
    }

    private void CreateUi()
    {
        if (canvasObject != null)
        {
            return;
        }

        EnsureEventSystem();

        canvasObject = new GameObject(
            "LevelUpCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );
        DontDestroyOnLoad(canvasObject);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        panelObject = new GameObject(
            "LevelUpPanel",
            typeof(RectTransform),
            typeof(Image)
        );
        panelObject.transform.SetParent(canvasObject.transform, false);

        RectTransform overlayRect = panelObject.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        panelObject.GetComponent<Image>().color =
            new Color(0.015f, 0.025f, 0.055f, 0.9f);

        TMP_Text title = CreateText(
            "Title",
            panelObject.transform,
            new Vector2(0.25f, 0.86f),
            new Vector2(0.75f, 0.97f),
            64f,
            TextAlignmentOptions.Center,
            new Color(1f, 0.86f, 0.25f)
        );
        title.text = "LEVEL UP";
        title.fontStyle = FontStyles.Bold;

        levelText = CreateText(
            "TeamLevel",
            panelObject.transform,
            new Vector2(0.35f, 0.80f),
            new Vector2(0.65f, 0.87f),
            32f,
            TextAlignmentOptions.Center,
            Color.white
        );

        for (int index = 0; index < CardCount; index++)
        {
            CreateCard(index);
        }

        timerText = CreateText(
            "Timer",
            panelObject.transform,
            new Vector2(0.35f, 0.08f),
            new Vector2(0.65f, 0.15f),
            34f,
            TextAlignmentOptions.Center,
            new Color(1f, 0.82f, 0.3f)
        );
        timerText.fontStyle = FontStyles.Bold;

        statusText = CreateText(
            "SelectionStatus",
            panelObject.transform,
            new Vector2(0.2f, 0.015f),
            new Vector2(0.8f, 0.075f),
            26f,
            TextAlignmentOptions.Center,
            new Color(0.78f, 0.88f, 1f)
        );
    }

    private void CreateCard(int index)
    {
        GameObject card = new GameObject(
            $"ChoiceCard{index + 1}",
            typeof(RectTransform),
            typeof(Image),
            typeof(Outline),
            typeof(Button)
        );
        card.transform.SetParent(panelObject.transform, false);

        RectTransform rect = card.GetComponent<RectTransform>();
        float centerX = 0.26f + index * 0.24f;
        rect.anchorMin = new Vector2(centerX - 0.105f, 0.19f);
        rect.anchorMax = new Vector2(centerX + 0.105f, 0.76f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = card.GetComponent<Image>();
        image.color = new Color(0.07f, 0.105f, 0.17f, 0.98f);

        Outline outline = card.GetComponent<Outline>();
        outline.effectColor = new Color(0.45f, 0.65f, 0.95f, 0.9f);
        outline.effectDistance = new Vector2(3f, -3f);

        Button button = card.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.92f, 0.65f);
        colors.pressedColor = new Color(0.75f, 0.85f, 1f);
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.75f);
        button.colors = colors;

        int capturedIndex = index;
        button.onClick.AddListener(() => HandleCardClicked(capturedIndex));
        cardButtons[index] = button;

        TMP_Text cardText = CreateText(
            "CardText",
            card.transform,
            new Vector2(0.07f, 0.06f),
            new Vector2(0.93f, 0.94f),
            30f,
            TextAlignmentOptions.Center,
            Color.white
        );
        cardText.enableAutoSizing = true;
        cardText.fontSizeMin = 19f;
        cardText.fontSizeMax = 32f;
        cardTexts[index] = cardText;
    }

    private void HandleCardClicked(int index)
    {
        if (progression == null || requestPending || progression.HasSelected)
        {
            return;
        }

        requestPending = true;

        foreach (Button button in cardButtons)
        {
            if (button != null)
            {
                button.interactable = false;
            }
        }

        progression.RequestChoice(index);
    }

    private string FormatCard(LevelUpCandidateState candidate)
    {
        if (candidate.Kind == LevelUpCandidateKind.GeneralUpgrade)
        {
            if (!progression.Settings.TryGetGeneralUpgrade(
                    candidate.GeneralUpgrade,
                    out GeneralUpgradeDefinition definition
                ))
            {
                return "능력치 강화";
            }

            return $"[{definition.DisplayName}]\n\n능력치 강화\n\n{definition.Description}";
        }

        if (candidate.Kind != LevelUpCandidateKind.Skill)
        {
            return "선택 불가";
        }

        SkillData skill = progression.GetSkillData(candidate.SkillPoolIndex);

        if (skill == null)
        {
            return "스킬 데이터 없음";
        }

        int currentLevel = progression.GetCurrentSkillLevel(
            candidate.SkillPoolIndex
        );
        string levelLabel = currentLevel <= 0
            ? "신규 → Lv1"
            : $"Lv{currentLevel} → Lv{candidate.TargetSkillLevel}";
        string result = currentLevel <= 0
            ? $"신규 습득: {skill.Description}"
            : $"강화: {skill.GetUpgradeSummary(candidate.TargetSkillLevel)}";

        return
            $"[{skill.DisplayName}]\n\n" +
            $"{levelLabel}\n\n" +
            $"속성: {ElementUtil.GetKoreanName(candidate.Element)}\n\n" +
            $"설명: {skill.Description}\n\n" +
            result;
    }

    private void RefreshTimerAndStatus()
    {
        if (progression == null || timerText == null || statusText == null)
        {
            return;
        }

        double remaining = progression.ChoiceDeadline - GetServerTime();
        timerText.text = $"남은 시간  {Mathf.Max(0, Mathf.CeilToInt((float)remaining))}초";
        statusText.text = progression.GetSelectionStatusText();

        if (progression.HasSelected)
        {
            foreach (Button button in cardButtons)
            {
                if (button != null)
                {
                    button.interactable = false;
                }
            }
        }
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        float fontSize,
        TextAlignmentOptions alignment,
        Color color
    )
    {
        GameObject textObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TMP_Text text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject(
            "LevelUpEventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule)
        );
    }

    private static double GetServerTime()
    {
        NetworkManager manager = NetworkManager.Singleton;
        return manager != null && manager.IsListening
            ? manager.ServerTime.Time
            : Time.unscaledTimeAsDouble;
    }
}
