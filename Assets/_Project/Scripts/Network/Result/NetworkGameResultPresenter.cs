using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[RequireComponent(typeof(NetworkGameResultState))]
[RequireComponent(typeof(NetworkGameResultActions))]
public class NetworkGameResultPresenter : NetworkBehaviour
{
    private static readonly Color VictoryColor =
        new Color(0.95f, 0.78f, 0.2f, 1f);

    private static readonly Color DefeatColor =
        new Color(0.95f, 0.3f, 0.3f, 1f);

    private NetworkGameResultState resultState;
    private NetworkGameResultActions resultActions;
    private GameObject canvasObject;
    private GameObject resultPanel;
    private TMP_Text resultText;
    private Button restartButton;
    private Button lobbyButton;
    private GameObject ownedEventSystem;

    private void Awake()
    {
        resultState = GetComponent<NetworkGameResultState>();
        resultActions = GetComponent<NetworkGameResultActions>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            return;
        }

        CreateResultView();
        resultState.ResultChanged += HandleResultChanged;
        HandleResultChanged(resultState.Result);
    }

    public override void OnNetworkDespawn()
    {
        if (resultState != null)
        {
            resultState.ResultChanged -= HandleResultChanged;
        }

        if (canvasObject != null)
        {
            Destroy(canvasObject);
        }

        if (ownedEventSystem != null)
        {
            Destroy(ownedEventSystem);
        }
    }

    private void CreateResultView()
    {
        canvasObject = new GameObject(
            "NetworkResultCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        DontDestroyOnLoad(canvasObject);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        resultPanel = new GameObject(
            "ResultPanel",
            typeof(RectTransform),
            typeof(Image)
        );
        resultPanel.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect = resultPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(560f, 360f);

        Image panelImage = resultPanel.GetComponent<Image>();
        panelImage.color = new Color(0.04f, 0.06f, 0.1f, 0.9f);

        GameObject textObject = new GameObject(
            "ResultText",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );
        textObject.transform.SetParent(resultPanel.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.08f, 0.56f);
        textRect.anchorMax = new Vector2(0.92f, 0.92f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        resultText = textObject.GetComponent<TextMeshProUGUI>();
        resultText.alignment = TextAlignmentOptions.Center;
        resultText.enableAutoSizing = true;
        resultText.fontSizeMin = 36f;
        resultText.fontSizeMax = 72f;
        resultText.fontStyle = FontStyles.Bold;

        restartButton = CreateButton(
            "RestartButton",
            "다시 시작",
            new Vector2(-125f, -70f),
            resultActions.RestartGame
        );

        lobbyButton = CreateButton(
            "LobbyButton",
            "로비로",
            new Vector2(125f, -70f),
            resultActions.ReturnToLobby
        );

        resultPanel.SetActive(false);
    }

    private Button CreateButton(
        string objectName,
        string label,
        Vector2 position,
        UnityEngine.Events.UnityAction onClick
    )
    {
        GameObject buttonObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(Image),
            typeof(Button)
        );
        buttonObject.transform.SetParent(resultPanel.transform, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = position;
        buttonRect.sizeDelta = new Vector2(220f, 72f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.88f, 0.9f, 0.95f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(onClick);

        GameObject labelObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12f, 8f);
        labelRect.offsetMax = new Vector2(-12f, -8f);

        TMP_Text labelText = labelObject.GetComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.color = new Color(0.08f, 0.1f, 0.16f, 1f);
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.enableAutoSizing = true;
        labelText.fontSizeMin = 20f;
        labelText.fontSizeMax = 36f;

        return button;
    }

    private void HandleResultChanged(NetworkGameResult result)
    {
        if (resultPanel == null || resultText == null)
        {
            return;
        }

        if (result == NetworkGameResult.Playing)
        {
            resultPanel.SetActive(false);
            return;
        }

        bool victory = result == NetworkGameResult.Victory;
        resultText.text = victory ? "승리" : "패배";
        resultText.color = victory ? VictoryColor : DefeatColor;

        bool hostCanControl = resultActions.CanControlResult;
        restartButton.gameObject.SetActive(hostCanControl);
        lobbyButton.gameObject.SetActive(hostCanControl);

        EnsureEventSystem();
        resultPanel.SetActive(true);
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        ownedEventSystem = new GameObject(
            "NetworkResultEventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule)
        );

        DontDestroyOnLoad(ownedEventSystem);
    }
}
