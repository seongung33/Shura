using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class RelicChoicePanelPresenter : MonoBehaviour
{
    private const int CardCount = 3;

    private readonly TMP_Text[] cardTexts = new TMP_Text[CardCount];
    private readonly Button[] cardButtons = new Button[CardCount];

    private PlayerRelicInventory inventory;
    private GameObject canvasObject;
    private GameObject panelObject;
    private TMP_Text timerText;
    private TMP_Text statusText;
    private int displayedSessionId = -1;
    private bool requestPending;

    public void Bind(PlayerRelicInventory configuredInventory)
    {
        if (inventory != null)
        {
            inventory.StateChanged -= Refresh;
        }

        inventory = configuredInventory;
        inventory.StateChanged += Refresh;
        CreateUi();
        Refresh();
    }

    public void Refresh()
    {
        if (inventory == null || panelObject == null)
        {
            return;
        }

        bool visible = inventory.ChoiceActive;
        panelObject.SetActive(visible);

        if (!visible)
        {
            requestPending = false;
            return;
        }

        if (displayedSessionId != inventory.ChoiceSessionId)
        {
            displayedSessionId = inventory.ChoiceSessionId;
            requestPending = false;
        }

        for (int index = 0; index < CardCount; index++)
        {
            bool hasCard = index < inventory.CurrentCandidateCount;
            cardButtons[index].gameObject.SetActive(hasCard);

            if (!hasCard)
            {
                continue;
            }

            RelicData relic = inventory.GetRelicData(
                inventory.GetCandidate(index)
            );
            cardTexts[index].text = relic != null
                ? $"[{relic.DisplayName}]\n\n{relic.Description}"
                : "유물 정보 없음";
            cardButtons[index].interactable =
                !inventory.HasSelected && !requestPending;
        }

        RefreshTimer();
    }

    private void Update()
    {
        if (inventory != null && inventory.ChoiceActive)
        {
            RefreshTimer();
        }
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.StateChanged -= Refresh;
        }

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
            "RelicChoiceCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );
        DontDestroyOnLoad(canvasObject);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 210;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        panelObject = new GameObject(
            "RelicChoicePanel",
            typeof(RectTransform),
            typeof(Image)
        );
        panelObject.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panelObject.GetComponent<Image>().color =
            new Color(0.02f, 0.018f, 0.045f, 0.92f);

        TMP_Text title = CreateText(
            "Title",
            panelObject.transform,
            new Vector2(0.25f, 0.84f),
            new Vector2(0.75f, 0.96f),
            62f,
            new Color(0.85f, 0.72f, 1f)
        );
        title.text = "ELITE RELIC";
        title.fontStyle = FontStyles.Bold;

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
            new Color(1f, 0.82f, 0.3f)
        );
        timerText.fontStyle = FontStyles.Bold;

        statusText = CreateText(
            "Status",
            panelObject.transform,
            new Vector2(0.25f, 0.015f),
            new Vector2(0.75f, 0.075f),
            26f,
            new Color(0.78f, 0.88f, 1f)
        );
    }

    private void CreateCard(int index)
    {
        GameObject card = new GameObject(
            $"RelicCard{index + 1}",
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

        card.GetComponent<Image>().color =
            new Color(0.09f, 0.065f, 0.16f, 0.98f);
        Outline outline = card.GetComponent<Outline>();
        outline.effectColor = new Color(0.7f, 0.5f, 1f, 0.9f);
        outline.effectDistance = new Vector2(3f, -3f);

        Button button = card.GetComponent<Button>();
        int capturedIndex = index;
        button.onClick.AddListener(() => HandleCardClicked(capturedIndex));
        cardButtons[index] = button;

        TMP_Text cardText = CreateText(
            "CardText",
            card.transform,
            new Vector2(0.07f, 0.06f),
            new Vector2(0.93f, 0.94f),
            30f,
            Color.white
        );
        cardText.enableAutoSizing = true;
        cardText.fontSizeMin = 18f;
        cardText.fontSizeMax = 31f;
        cardTexts[index] = cardText;
    }

    private void HandleCardClicked(int index)
    {
        if (inventory == null || requestPending || inventory.HasSelected)
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

        inventory.RequestChoice(index);
    }

    private void RefreshTimer()
    {
        double remaining = Math.Max(
            0d,
            inventory.ChoiceDeadline - GetServerTime()
        );
        timerText.text = $"{Math.Ceiling(remaining):0}초";
        statusText.text =
            $"보유 유물 {inventory.OwnedCount}/{PlayerRelicInventory.MaximumRelics}";
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        float fontSize,
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
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject(
                "RelicChoiceEventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule)
            );
        }
    }

    private static double GetServerTime()
    {
        NetworkManager manager = NetworkManager.Singleton;
        return manager != null && manager.IsListening
            ? manager.ServerTime.Time
            : Time.unscaledTimeAsDouble;
    }
}
