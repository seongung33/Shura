using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(NetworkGameResultState))]
public class NetworkGameResultPresenter : NetworkBehaviour
{
    private static readonly Color VictoryColor =
        new Color(0.95f, 0.78f, 0.2f, 1f);

    private static readonly Color DefeatColor =
        new Color(0.95f, 0.3f, 0.3f, 1f);

    private NetworkGameResultState resultState;
    private GameObject canvasObject;
    private GameObject resultPanel;
    private TMP_Text resultText;

    private void Awake()
    {
        resultState = GetComponent<NetworkGameResultState>();
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
    }

    private void CreateResultView()
    {
        canvasObject = new GameObject(
            "NetworkResultCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler)
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
        panelRect.sizeDelta = new Vector2(520f, 240f);

        Image panelImage = resultPanel.GetComponent<Image>();
        panelImage.color = new Color(0.04f, 0.06f, 0.1f, 0.9f);

        GameObject textObject = new GameObject(
            "ResultText",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );
        textObject.transform.SetParent(resultPanel.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.08f, 0.15f);
        textRect.anchorMax = new Vector2(0.92f, 0.85f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        resultText = textObject.GetComponent<TextMeshProUGUI>();
        resultText.alignment = TextAlignmentOptions.Center;
        resultText.enableAutoSizing = true;
        resultText.fontSizeMin = 36f;
        resultText.fontSizeMax = 72f;
        resultText.fontStyle = FontStyles.Bold;

        resultPanel.SetActive(false);
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
        resultPanel.SetActive(true);
    }
}
