using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class CharacterSelectVisualTheme
{
    private static readonly Color Background = new(0.035f, 0.055f, 0.10f, 1f);
    private static readonly Color Panel = new(0.055f, 0.09f, 0.15f, 0.96f);
    private static readonly Color PanelRaised = new(0.075f, 0.12f, 0.20f, 1f);
    private static readonly Color Accent = new(0.12f, 0.78f, 0.92f, 1f);
    private static readonly Color AccentSoft = new(0.12f, 0.78f, 0.92f, 0.24f);
    private static readonly Color PrimaryText = new(0.94f, 0.97f, 1f, 1f);
    private static readonly Color SecondaryText = new(0.63f, 0.72f, 0.82f, 1f);

    public static void Apply(Transform source)
    {
        Canvas canvas = source.GetComponentInParent<Canvas>();
        if (canvas == null || canvas.transform.Find("CharacterSelectThemeRuntime") != null)
            return;

        if (Camera.main != null)
            Camera.main.backgroundColor = Background;

        ConfigureCanvas(canvas);

        TMP_FontAsset font = canvas.GetComponentInChildren<TMP_Text>(true)?.font;
        RectTransform themeRoot = CreateRect("CharacterSelectThemeRuntime", canvas.transform);
        Stretch(themeRoot, Vector2.zero, Vector2.zero);
        themeRoot.SetAsFirstSibling();

        Image backdrop = themeRoot.gameObject.AddComponent<Image>();
        backdrop.color = Background;
        backdrop.raycastTarget = false;

        bool multiplayerLobby = FindRect(canvas.transform, "StartGameButton") != null;
        string heading = multiplayerLobby ? "협동 영웅 선택" : "영웅 선택";
        string subtitle = multiplayerLobby
            ? "동료와 영웅을 정하고 전투 준비를 완료하세요"
            : "전장에 함께할 영웅을 선택하세요";

        CreateText(themeRoot, heading, font, 42, FontStyles.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(298f, -30f), new Vector2(500f, 60f),
            TextAlignmentOptions.Left, PrimaryText);
        CreateText(themeRoot, subtitle, font, 20, FontStyles.Normal,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(308f, -88f), new Vector2(520f, 36f),
            TextAlignmentOptions.Left, SecondaryText);

        RectTransform players = FindRect(canvas.transform, "PlayersPanel");
        RectTransform list = FindRect(canvas.transform, "CharacterListPanel");
        RectTransform info = FindRect(canvas.transform, "CharacterInfoPanel");

        SetPanel(players, new Vector2(0f, 0f), new Vector2(0.22f, 1f),
            new Vector2(28f, 32f), new Vector2(-12f, -128f), Panel);
        SetPanel(list, new Vector2(0.22f, 0f), new Vector2(0.66f, 1f),
            new Vector2(12f, 32f), new Vector2(-12f, -128f), PanelRaised);
        SetPanel(info, new Vector2(0.66f, 0f), new Vector2(1f, 1f),
            new Vector2(12f, 32f), new Vector2(-28f, -128f), Panel);

        StylePlayersPanel(players, font);
        StyleCharacterList(list, font);
        StyleInfoPanel(info, font);
        if (multiplayerLobby)
            StyleLobbyChrome(canvas.transform, font);
    }

    public static void StyleCharacterCard(Transform card)
    {
        if (card == null)
            return;

        Image background = card.GetComponent<Image>();
        if (background != null)
            background.color = new Color(0.08f, 0.14f, 0.22f, 1f);

        Outline outline = card.GetComponent<Outline>() ?? card.gameObject.AddComponent<Outline>();
        outline.effectColor = Accent;
        outline.effectDistance = new Vector2(2f, -2f);

        RectTransform portrait = FindRect(card, "PortraitImage");
        SetCentered(portrait, new Vector2(0f, 36f), new Vector2(220f, 220f));

        TMP_Text name = FindText(card, "CharacterNameText");
        if (name != null)
        {
            SetCentered(name.rectTransform, new Vector2(0f, -108f), new Vector2(280f, 48f));
            StyleText(name, 30, FontStyles.Bold, PrimaryText, TextAlignmentOptions.Center);
        }

        TMP_Text legacyButtonText = FindText(card, "Text (TMP)");
        if (legacyButtonText != null)
            legacyButtonText.gameObject.SetActive(false);
    }

    private static void ConfigureCanvas(Canvas canvas)
    {
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            return;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    private static void StylePlayersPanel(RectTransform panel, TMP_FontAsset font)
    {
        if (panel == null)
            return;

        TMP_Text title = FindText(panel, "PlayerTitleText");
        if (title != null)
        {
            title.text = "파티 구성";
            SetTopStretch(title.rectTransform, 24f, -24f, 48f);
            StyleText(title, 27, FontStyles.Bold, PrimaryText, TextAlignmentOptions.Left);
        }

        RectTransform mine = FindRect(panel, "MyPlayerSlot");
        if (mine != null)
        {
            mine.anchorMin = new Vector2(0f, 1f);
            mine.anchorMax = new Vector2(1f, 1f);
            mine.pivot = new Vector2(0.5f, 1f);
            mine.anchoredPosition = new Vector2(0f, -92f);
            mine.sizeDelta = new Vector2(-40f, 126f);
            StyleImage(mine, PanelRaised);

            RectTransform portrait = FindRect(mine, "PortraitImage");
            SetLeftCenter(portrait, 66f, new Vector2(94f, 94f));

            TMP_Text playerName = FindText(mine, "PlayerNameText");
            if (playerName != null)
            {
                SetCentered(playerName.rectTransform, new Vector2(66f, 0f), new Vector2(190f, 56f));
                playerName.text = string.IsNullOrWhiteSpace(playerName.text) ? "플레이어 1" : playerName.text;
                StyleText(playerName, 22, FontStyles.Bold, PrimaryText, TextAlignmentOptions.Left);
            }
        }

        RectTransform others = FindRect(panel, "OtherPlayersList");
        if (others != null)
        {
            others.anchorMin = new Vector2(0f, 0f);
            others.anchorMax = new Vector2(1f, 1f);
            others.offsetMin = new Vector2(20f, 24f);
            others.offsetMax = new Vector2(-20f, -244f);
            StyleImage(others, new Color(0.04f, 0.07f, 0.12f, 0.72f));
        }

        CreateText(panel, "대기 슬롯", font, 18, FontStyles.Normal,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -230f), new Vector2(-48f, 32f),
            TextAlignmentOptions.Left, SecondaryText);
    }

    private static void StyleCharacterList(RectTransform panel, TMP_FontAsset font)
    {
        if (panel == null)
            return;

        CreateText(panel, "영웅 목록", font, 28, FontStyles.Bold,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -26f), new Vector2(-48f, 44f),
            TextAlignmentOptions.Left, PrimaryText);
        CreateText(panel, "현재 선택 가능한 영웅", font, 17, FontStyles.Normal,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -70f), new Vector2(-48f, 30f),
            TextAlignmentOptions.Left, SecondaryText);

        RectTransform grid = FindRect(panel, "CharacterGrid");
        if (grid == null)
            return;

        grid.anchorMin = Vector2.zero;
        grid.anchorMax = Vector2.one;
        grid.offsetMin = new Vector2(28f, 84f);
        grid.offsetMax = new Vector2(-28f, -126f);
        StyleImage(grid, new Color(0.035f, 0.06f, 0.105f, 0.86f));

        GridLayoutGroup layout = grid.GetComponent<GridLayoutGroup>();
        if (layout != null)
        {
            layout.cellSize = new Vector2(320f, 390f);
            layout.spacing = new Vector2(24f, 24f);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 2;
        }

        CreateText(panel, "새로운 영웅은 추후 공개됩니다", font, 16, FontStyles.Normal,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 32f), new Vector2(-48f, 34f),
            TextAlignmentOptions.Center, SecondaryText);

        foreach (CharacterSlotUI slot in grid.GetComponentsInChildren<CharacterSlotUI>(true))
            StyleCharacterCard(slot.transform);
    }

    private static void StyleInfoPanel(RectTransform panel, TMP_FontAsset font)
    {
        if (panel == null)
            return;

        CreateText(panel, "영웅 정보", font, 24, FontStyles.Bold,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -26f), new Vector2(-48f, 40f),
            TextAlignmentOptions.Left, SecondaryText);

        TMP_Text name = FindText(panel, "CharecterNameText");
        if (name != null)
        {
            SetTopStretch(name.rectTransform, 24f, -88f, 64f);
            StyleText(name, 40, FontStyles.Bold, PrimaryText, TextAlignmentOptions.Left);
        }

        TMP_Text role = FindText(panel, "CharecterRoleText");
        if (role != null)
        {
            SetTopStretch(role.rectTransform, 24f, -166f, 42f);
            StyleText(role, 20, FontStyles.Bold, Accent, TextAlignmentOptions.Left);
        }

        TMP_Text description = FindText(panel, "CharacterDescriptionText");
        if (description != null)
        {
            description.rectTransform.anchorMin = new Vector2(0f, 0f);
            description.rectTransform.anchorMax = new Vector2(1f, 1f);
            description.rectTransform.offsetMin = new Vector2(24f, 210f);
            description.rectTransform.offsetMax = new Vector2(-24f, -238f);
            StyleText(description, 22, FontStyles.Normal, PrimaryText, TextAlignmentOptions.TopLeft);
            description.textWrappingMode = TextWrappingModes.Normal;
            description.lineSpacing = 8f;
        }

        StyleButton(FindRect(panel, "BackButton"), "뒤로", false,
            new Vector2(0f, 0f), new Vector2(0.48f, 0f), new Vector2(24f, 28f), new Vector2(-8f, 92f));
        RectTransform startButton = FindRect(panel, "StartButton") ??
            FindRect(panel, "StartGameButton");
        StyleButton(startButton, "게임 시작", true,
            new Vector2(0.48f, 0f), new Vector2(1f, 0f), new Vector2(8f, 28f), new Vector2(-24f, 92f));
    }

    private static void StyleLobbyChrome(Transform canvas, TMP_FontAsset font)
    {
        TMP_Text joinCode = FindText(canvas, "JoinCodeText");
        if (joinCode != null)
        {
            SetAnchored(joinCode.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-280f, -34f), new Vector2(500f, 42f));
            StyleText(joinCode, 24f, FontStyles.Bold, PrimaryText, TextAlignmentOptions.Right);
            if (font != null)
                joinCode.font = font;
        }

        TMP_Text playerCount = FindText(canvas, "PlayerCountText");
        if (playerCount != null)
        {
            SetAnchored(playerCount.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-280f, -76f), new Vector2(500f, 34f));
            StyleText(playerCount, 19f, FontStyles.Bold, Accent, TextAlignmentOptions.Right);
            if (font != null)
                playerCount.font = font;
        }

        TMP_Text status = FindText(canvas, "StatusText");
        if (status != null)
        {
            SetAnchored(status.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -104f), new Vector2(620f, 34f));
            StyleText(status, 18f, FontStyles.Bold, SecondaryText, TextAlignmentOptions.Center);
            if (font != null)
                status.font = font;
        }

        RectTransform leaveButton = FindRect(canvas, "LeaveRoomButton");
        StyleButton(leaveButton, "로비 나가기", false,
            Vector2.zero, Vector2.zero, new Vector2(28f, 28f), new Vector2(250f, 92f));

    }

    private static void StyleButton(RectTransform rect, string label, bool primary,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (rect == null)
            return;

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.localScale = Vector3.one;

        Image image = rect.GetComponent<Image>();
        if (image != null)
            image.color = primary ? Accent : PanelRaised;

        Button button = rect.GetComponent<Button>();
        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = primary ? new Color(0.8f, 1f, 1f, 1f) : new Color(1f, 1f, 1f, 0.86f);
            colors.pressedColor = new Color(0.7f, 0.85f, 0.9f, 1f);
            button.colors = colors;
            if (button.GetComponent<MainMenuButtonMotion>() == null)
                button.gameObject.AddComponent<MainMenuButtonMotion>();
            button.onClick.AddListener(GameAudioController.PlayButtonClick);
        }

        TMP_Text text = rect.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.text = label;
            StyleText(text, 23, FontStyles.Bold, primary ? Background : PrimaryText, TextAlignmentOptions.Center);
        }
    }

    private static TMP_Text CreateText(Transform parent, string value, TMP_FontAsset font, float size,
        FontStyles style, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 dimensions,
        TextAlignmentOptions alignment, Color color)
    {
        GameObject go = new(value + "Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.layer = 5;
        go.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;

        TMP_Text text = go.GetComponent<TMP_Text>();
        text.text = value;
        text.font = font;
        StyleText(text, size, style, color, alignment);
        text.raycastTarget = false;
        return text;
    }

    private static void SetPanel(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        if (rect == null)
            return;

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        StyleImage(rect, color);

        Outline outline = rect.GetComponent<Outline>() ?? rect.gameObject.AddComponent<Outline>();
        outline.effectColor = AccentSoft;
        outline.effectDistance = new Vector2(1.5f, -1.5f);
    }

    private static void StyleImage(RectTransform rect, Color color)
    {
        Image image = rect.GetComponent<Image>();
        if (image != null)
            image.color = color;
    }

    private static void StyleText(TMP_Text text, float size, FontStyles style, Color color,
        TextAlignmentOptions alignment)
    {
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.faceColor = color;
        text.outlineColor = new Color(0f, 0f, 0f, 0.72f);
        text.outlineWidth = 0.12f;
        text.alignment = alignment;
        text.enableAutoSizing = false;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void SetTopStretch(RectTransform rect, float horizontalPadding, float y, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(-horizontalPadding * 2f, height);
    }

    private static void SetCentered(RectTransform rect, Vector2 position, Vector2 size)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void SetAnchored(RectTransform rect, Vector2 anchor, Vector2 pivot,
        Vector2 position, Vector2 size)
    {
        if (rect == null)
            return;

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void SetLeftCenter(RectTransform rect, float x, Vector2 size)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, 0f);
        rect.sizeDelta = size;
    }

    private static RectTransform FindRect(Transform root, string name)
    {
        Transform found = Find(root, name);
        return found as RectTransform;
    }

    private static TMP_Text FindText(Transform root, string name)
    {
        Transform found = Find(root, name);
        return found != null ? found.GetComponent<TMP_Text>() : null;
    }

    private static Transform Find(Transform root, string name)
    {
        if (root == null)
            return null;

        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = Find(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }
}
