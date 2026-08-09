using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class GameplayPauseMenu : MonoBehaviour
{
    private static readonly Color PanelColor = new Color(0.025f, 0.04f, 0.09f, 0.97f);
    private GameObject menuOverlay;
    private TMP_Text guideText;
    private Button menuButton;
    private GameObject ownedEventSystem;
    private bool isOpen;
    private bool transitionStarted;

    private void Start()
    {
        CreateView();
        EnsureEventSystem();
    }

    private void Update()
    {
        if (transitionStarted || Keyboard.current == null ||
            !Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        SetOpen(!isOpen);
    }

    private void OnDestroy()
    {
        if (!IsNetworkSessionRunning())
        {
            Time.timeScale = 1f;
        }

        if (ownedEventSystem != null)
        {
            Destroy(ownedEventSystem);
        }
    }

    private void SetOpen(bool open)
    {
        isOpen = open;
        menuOverlay.SetActive(open);
        menuButton.gameObject.SetActive(!open);

        bool networkSession = IsNetworkSessionRunning();
        guideText.text = networkSession
            ? "멀티 플레이는 메뉴가 열린 동안에도 계속 진행됩니다."
            : "게임이 일시정지되었습니다.";

        if (!networkSession)
        {
            Time.timeScale = open ? 0f : 1f;
        }
    }

    private async void ReturnToMainMenu()
    {
        if (transitionStarted)
        {
            return;
        }

        transitionStarted = true;
        Time.timeScale = 1f;

        NetworkManager manager = NetworkManager.Singleton;
        if (manager != null)
        {
            NetworkSessionState sessionState = manager.GetComponent<NetworkSessionState>();
            if (sessionState != null && sessionState.HasSession)
            {
                try
                {
                    await sessionState.LeaveSessionAsync();
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"세션 나가기 중 오류: {exception.Message}");
                }
            }

            if (manager.IsListening)
            {
                manager.Shutdown();
            }

            Destroy(manager.gameObject);
            await Task.Yield();
        }

        SinglePlayerSelection.Clear();
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }

    private static void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void CreateView()
    {
        GameObject canvasObject = new GameObject(
            "GameplayPauseCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        menuButton = CreateButton(canvasObject.transform, "MenuButton", "메뉴", ToggleFromButton);
        SetRect(
            menuButton.GetComponent<RectTransform>(),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-28f, -28f),
            new Vector2(150f, 62f),
            Vector2.one
        );

        menuOverlay = new GameObject("PauseOverlay", typeof(RectTransform), typeof(Image));
        menuOverlay.transform.SetParent(canvasObject.transform, false);
        Stretch(menuOverlay.GetComponent<RectTransform>());
        menuOverlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.58f);

        GameObject panel = new GameObject("PausePanel", typeof(RectTransform), typeof(Image), typeof(Outline));
        panel.transform.SetParent(menuOverlay.transform, false);
        SetRect(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 560f), new Vector2(0.5f, 0.5f));
        panel.GetComponent<Image>().color = PanelColor;
        Outline outline = panel.GetComponent<Outline>();
        outline.effectColor = new Color(0.2f, 0.65f, 0.85f, 0.9f);
        outline.effectDistance = new Vector2(3f, -3f);

        TMP_Text title = CreateText(panel.transform, "Title", "일시정지", 64f);
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(520f, 88f), new Vector2(0.5f, 1f));

        guideText = CreateText(panel.transform, "Guide", string.Empty, 25f);
        SetRect(guideText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -142f), new Vector2(520f, 58f), new Vector2(0.5f, 1f));

        Button resumeButton = CreateButton(panel.transform, "ResumeButton", "계속하기", ToggleFromButton);
        SetRect(resumeButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 42f), new Vector2(400f, 78f), new Vector2(0.5f, 0.5f));

        Button mainMenuButton = CreateButton(panel.transform, "MainMenuButton", "메인 메뉴", ReturnToMainMenu);
        SetRect(mainMenuButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -58f), new Vector2(400f, 78f), new Vector2(0.5f, 0.5f));

        Button quitButton = CreateButton(panel.transform, "QuitButton", "게임 종료", QuitGame);
        SetRect(quitButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -158f), new Vector2(400f, 78f), new Vector2(0.5f, 0.5f));

        menuOverlay.SetActive(false);
    }

    private void ToggleFromButton()
    {
        SetOpen(!isOpen);
    }

    private static Button CreateButton(Transform parent, string name, string label, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        buttonObject.GetComponent<Image>().color = new Color(0.15f, 0.55f, 0.72f, 1f);
        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(action);

        TMP_Text labelText = CreateText(buttonObject.transform, "Label", label, 34f);
        Stretch(labelText.rectTransform, 10f);
        return button;
    }

    private static TMP_Text CreateText(Transform parent, string name, string value, float maxSize)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TMP_Text text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 16f;
        text.fontSizeMax = maxSize;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        return text;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Vector2 pivot)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    private static bool IsNetworkSessionRunning()
    {
        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        ownedEventSystem = new GameObject(
            "GameplayPauseEventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule)
        );
        ownedEventSystem.transform.SetParent(transform, false);
    }
}
