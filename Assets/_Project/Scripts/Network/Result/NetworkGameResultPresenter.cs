using System.Collections;
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
    private GameObject resultRoot;
    private GameObject resultPanel;
    private TMP_Text resultText;
    private TMP_Text guideText;
    private Button restartButton;
    private Button lobbyButton;
    private GameObject ownedEventSystem;
    private CanvasGroup resultGroup;
    private Coroutine revealRoutine;

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

        resultRoot = new GameObject(
            "ResultRoot",
            typeof(RectTransform),
            typeof(CanvasGroup)
        );
        resultRoot.transform.SetParent(canvasObject.transform, false);
        Stretch((RectTransform)resultRoot.transform);
        resultGroup = resultRoot.GetComponent<CanvasGroup>();

        GameObject backdrop = new GameObject(
            "Backdrop",
            typeof(RectTransform),
            typeof(Image)
        );
        backdrop.transform.SetParent(resultRoot.transform, false);
        Stretch((RectTransform)backdrop.transform);
        backdrop.GetComponent<Image>().color =
            new Color(0f, 0.01f, 0.03f, 0.7f);

        resultPanel = new GameObject(
            "ResultPanel",
            typeof(RectTransform),
            typeof(Image),
            typeof(Outline)
        );
        resultPanel.transform.SetParent(resultRoot.transform, false);

        RectTransform panelRect = resultPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(640f, 410f);

        Image panelImage = resultPanel.GetComponent<Image>();
        panelImage.color = new Color(0.025f, 0.05f, 0.09f, 0.97f);
        Outline panelOutline = resultPanel.GetComponent<Outline>();
        panelOutline.effectColor = new Color(0.08f, 0.72f, 0.88f, 0.85f);
        panelOutline.effectDistance = new Vector2(2f, -2f);

        GameObject textObject = new GameObject(
            "ResultText",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );
        textObject.transform.SetParent(resultPanel.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.08f, 0.61f);
        textRect.anchorMax = new Vector2(0.92f, 0.92f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        resultText = textObject.GetComponent<TextMeshProUGUI>();
        resultText.alignment = TextAlignmentOptions.Center;
        resultText.enableAutoSizing = true;
        resultText.fontSizeMin = 36f;
        resultText.fontSizeMax = 72f;
        resultText.fontStyle = FontStyles.Bold;

        GameObject guideObject = new GameObject(
            "GuideText",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );
        guideObject.transform.SetParent(resultPanel.transform, false);
        RectTransform guideRect = guideObject.GetComponent<RectTransform>();
        guideRect.anchorMin = new Vector2(0.1f, 0.42f);
        guideRect.anchorMax = new Vector2(0.9f, 0.62f);
        guideRect.offsetMin = Vector2.zero;
        guideRect.offsetMax = Vector2.zero;
        guideText = guideObject.GetComponent<TextMeshProUGUI>();
        guideText.alignment = TextAlignmentOptions.Center;
        guideText.enableAutoSizing = true;
        guideText.fontSizeMin = 19f;
        guideText.fontSizeMax = 27f;
        guideText.color = new Color(0.72f, 0.82f, 0.92f, 1f);
        guideText.faceColor = guideText.color;

        restartButton = CreateButton(
            "RestartButton",
            "다시 시작",
            new Vector2(-140f, -105f),
            resultActions.RestartGame
        );

        lobbyButton = CreateButton(
            "LobbyButton",
            "멀티 입장으로",
            new Vector2(140f, -105f),
            resultActions.ReturnToLobby
        );
        lobbyButton.GetComponent<Image>().color =
            new Color(0.09f, 0.18f, 0.28f, 1f);
        TMP_Text lobbyLabel = lobbyButton.GetComponentInChildren<TMP_Text>();
        lobbyLabel.color = new Color(0.92f, 0.97f, 1f, 1f);
        lobbyLabel.faceColor = lobbyLabel.color;

        resultRoot.SetActive(false);
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
        buttonRect.sizeDelta = new Vector2(250f, 72f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.08f, 0.72f, 0.88f, 1f);

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
        labelText.color = new Color(0.02f, 0.05f, 0.09f, 1f);
        labelText.faceColor = labelText.color;
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
            if (revealRoutine != null)
            {
                StopCoroutine(revealRoutine);
                revealRoutine = null;
            }
            resultRoot.SetActive(false);
            return;
        }

        bool victory = result == NetworkGameResult.Victory;
        resultText.text = victory ? "승리" : "패배";
        resultText.color = victory ? VictoryColor : DefeatColor;

        bool hostCanControl = resultActions.CanControlResult;
        restartButton.gameObject.SetActive(hostCanControl);
        lobbyButton.gameObject.SetActive(hostCanControl);
        guideText.text = hostCanControl
            ? "다시 도전하거나 멀티 입장 화면으로 돌아갈 수 있습니다."
            : "호스트가 다음 진행을 선택하고 있습니다.";

        EnsureEventSystem();
        resultRoot.SetActive(true);
        if (revealRoutine != null)
        {
            StopCoroutine(revealRoutine);
        }
        revealRoutine = StartCoroutine(RevealResult());
    }

    private IEnumerator RevealResult()
    {
        RectTransform rect = (RectTransform)resultPanel.transform;
        resultGroup.alpha = 0f;
        rect.localScale = Vector3.one * 0.94f;
        float elapsed = 0f;
        const float duration = 0.26f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            resultGroup.alpha = t;
            rect.localScale = Vector3.Lerp(
                Vector3.one * 0.94f,
                Vector3.one,
                t
            );
            yield return null;
        }

        resultGroup.alpha = 1f;
        rect.localScale = Vector3.one;
        revealRoutine = null;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
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
