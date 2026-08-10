using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 새 MultiplayerEntry 씬 전용 UI다.
/// 검증된 NetworkTestUI를 수정하거나 상속하지 않고,
/// 동일한 UGS/Relay 생성·참가 계약을 독립적으로 사용한다.
/// </summary>
public sealed class MultiplayerEntryUI : MonoBehaviour
{
    private const string JoinCodeFontName = "LiberationSans SDF";
    private const string BackgroundResourcePath = "Backgrounds/jangsan_forest_floor";

    private static readonly Color ScreenColor =
        new Color32(6, 8, 18, 255);
    private static readonly Color PanelColor =
        new Color32(20, 18, 35, 244);
    private static readonly Color PrimaryColor =
        new Color32(220, 86, 115, 255);
    private static readonly Color ButtonColor =
        new Color32(28, 25, 52, 255);
    private static readonly Color TextColor =
        new Color32(224, 242, 255, 255);
    [Header("Relay UI")]

    [SerializeField]
    private TMP_InputField joinCodeInput;

    [SerializeField]
    private TMP_Text joinCodeText;

    [SerializeField]
    private TMP_Text statusText;

    [Header("Entry Controls")]

    [SerializeField]
    private Button createRoomButton;

    [SerializeField]
    private Button joinRoomButton;

    [SerializeField]
    private Button leaveRoomButton;

    [SerializeField]
    private Button backButton;

    [Header("New Multiplayer Flow Only")]

    [SerializeField]
    private NetworkSessionState sessionState;

    [SerializeField]
    private NetworkLobbySceneLoader lobbySceneLoader;

    private bool servicesReady;
    private bool initializationInProgress;
    private bool operationInProgress;
    private bool isDestroyed;
    private bool callbacksRegistered;

    private async void Start()
    {
        GameAudioController.EnsureExists();
        ResolveReferences();
        ApplyVisualTheme();
        RegisterCallbacks();
        RefreshControls();
        await InitializeServicesAsync();
    }

    public async void RetryInitializeServices()
    {
        if (initializationInProgress || operationInProgress)
        {
            SetStatus("현재 초기화 또는 네트워크 작업이 진행 중입니다.");
            return;
        }

        servicesReady = false;
        await InitializeServicesAsync();
    }

    public async void CreateRelaySession()
    {
        ResolveReferences();

        if (!CanStartOperation(requireLobbyLoader: true))
        {
            return;
        }

        if (sessionState.HasSession)
        {
            SetStatus("이미 참가 중인 방이 있습니다. 먼저 나가주세요.");
            return;
        }

        operationInProgress = true;
        RefreshControls();

        ISession createdSession = null;

        try
        {
            SetStatus("방 생성 중...");

            SessionOptions options =
                new SessionOptions
                {
                    MaxPlayers = 2
                }
                .WithRelayNetwork();

            createdSession =
                await MultiplayerService.Instance
                    .CreateSessionAsync(options);

            sessionState.AttachSession(createdSession);
            ShowJoinCode(sessionState.JoinCode);

            SetStatus(
                "방 생성 완료 - 멀티 로비로 이동합니다."
            );

            Debug.Log(
                $"Relay 방 생성 완료. " +
                $"참가 코드: {sessionState.JoinCode}"
            );

            lobbySceneLoader.EnterLobbyWhenReady();
        }
        catch (Exception exception)
        {
            SetStatus("방 생성 실패");
            Debug.LogException(exception);
            await CleanupFailedSessionAsync(createdSession);
        }
        finally
        {
            operationInProgress = false;

            if (!isDestroyed)
            {
                RefreshControls();
            }
        }
    }

    public async void JoinRelaySession()
    {
        ResolveReferences();

        if (!CanStartOperation(requireLobbyLoader: false))
        {
            return;
        }

        if (sessionState.HasSession)
        {
            SetStatus("이미 참가 중인 방이 있습니다. 먼저 나가주세요.");
            return;
        }

        if (joinCodeInput == null)
        {
            SetStatus("Join Code Input이 연결되지 않았습니다.");
            return;
        }

        string joinCode =
            joinCodeInput.text
                .Trim()
                .ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(joinCode))
        {
            SetStatus("참가 코드를 입력하세요.");
            return;
        }

        operationInProgress = true;
        RefreshControls();

        ISession joinedSession = null;

        try
        {
            SetStatus("방 참가 중...");

            joinedSession =
                await MultiplayerService.Instance
                    .JoinSessionByCodeAsync(joinCode);

            sessionState.AttachSession(joinedSession);
            ShowJoinCode(joinCode);

            // 게스트는 직접 씬을 로드하지 않는다.
            // 호스트의 NGO NetworkSceneManager가 현재 로비 씬으로 동기화한다.
            SetStatus("방 참가 완료 - 로비 동기화 중...");

            Debug.Log($"Relay 방 참가 성공. 코드: {joinCode}");
        }
        catch (Exception exception)
        {
            SetStatus("방 참가 실패 - 코드와 호스트 상태를 확인하세요.");
            Debug.LogException(exception);
            await CleanupFailedSessionAsync(joinedSession);
        }
        finally
        {
            operationInProgress = false;

            if (!isDestroyed)
            {
                RefreshControls();
            }
        }
    }

    public async void LeaveRelaySession()
    {
        ResolveReferences();

        if (!CanStartOperation(requireLobbyLoader: false))
        {
            return;
        }

        if (!sessionState.HasSession)
        {
            SetStatus("참가 중인 방이 없습니다.");
            return;
        }

        operationInProgress = true;
        RefreshControls();

        try
        {
            SetStatus("방에서 나가는 중...");
            await sessionState.LeaveSessionAsync();
            SetStatus("방에서 나왔습니다.");
        }
        catch (Exception exception)
        {
            SetStatus($"방 나가기 중 오류: {exception.Message}");
            Debug.LogException(exception);
        }
        finally
        {
            ClearJoinCode();
            ShutdownNetworkIfListening();
            operationInProgress = false;

            if (!isDestroyed)
            {
                RefreshControls();
            }
        }
    }

    private async Task InitializeServicesAsync()
    {
        if (initializationInProgress)
        {
            SetStatus("온라인 서비스 초기화가 이미 진행 중입니다.");
            return;
        }

        initializationInProgress = true;

        try
        {
            SetStatus("온라인 서비스 초기화 중...");

            if (UnityServices.State ==
                ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance
                    .SignInAnonymouslyAsync();
            }

            servicesReady = true;
            SetStatus("온라인 서비스 준비 완료");
        }
        catch (Exception exception)
        {
            servicesReady = false;
            SetStatus("온라인 서비스 초기화 실패");
            Debug.LogException(exception);
        }
        finally
        {
            initializationInProgress = false;

            if (!isDestroyed)
            {
                RefreshControls();
            }
        }
    }

    private bool CanStartOperation(bool requireLobbyLoader)
    {
        if (!servicesReady)
        {
            SetStatus("온라인 서비스가 아직 준비되지 않았습니다.");
            return false;
        }

        if (operationInProgress)
        {
            SetStatus("현재 네트워크 작업이 진행 중입니다.");
            return false;
        }

        if (sessionState == null)
        {
            SetStatus("NetworkSessionState가 연결되지 않았습니다.");
            return false;
        }

        if (requireLobbyLoader && lobbySceneLoader == null)
        {
            SetStatus("NetworkLobbySceneLoader가 연결되지 않았습니다.");
            return false;
        }

        return true;
    }

    private async Task CleanupFailedSessionAsync(ISession failedSession)
    {
        try
        {
            if (failedSession == null)
            {
                return;
            }

            if (sessionState != null &&
                ReferenceEquals(
                    sessionState.CurrentSession,
                    failedSession
                ))
            {
                await sessionState.LeaveSessionAsync();
            }
            else
            {
                await failedSession.LeaveAsync();
            }
        }
        catch (Exception cleanupException)
        {
            Debug.LogWarning(
                $"실패한 세션 정리 중 오류: {cleanupException.Message}"
            );
        }
        finally
        {
            if (sessionState != null)
            {
                sessionState.ClearSession(failedSession);
            }

            ShutdownNetworkIfListening();
            ClearJoinCode();
        }
    }

    private static void ShutdownNetworkIfListening()
    {
        NetworkManager manager = NetworkManager.Singleton;

        if (manager != null && manager.IsListening)
        {
            manager.Shutdown();
        }
    }

    private void ResolveReferences()
    {
        if (sessionState == null)
        {
            NetworkManager manager = NetworkManager.Singleton;

            if (manager != null)
            {
                sessionState =
                    manager.GetComponent<NetworkSessionState>();
            }
        }
    }

    private void ApplyVisualTheme()
    {
        Canvas canvas = GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            return;
        }

        Camera sceneCamera = Camera.main;
        if (sceneCamera != null)
        {
            sceneCamera.backgroundColor = ScreenColor;
        }

        RectTransform canvasRect = canvas.transform as RectTransform;
        if (canvasRect == null)
        {
            return;
        }

        ConfigureCanvas(canvas);

        TMP_FontAsset uiFont = createRoomButton != null
            ? createRoomButton.GetComponentInChildren<TMP_Text>(true)?.font
            : null;

        EnsureArtwork(canvasRect);
        EnsureBackdrop(canvasRect);
        EnsureHeading(canvasRect, uiFont);

        StyleText(statusText, 24f, TextColor, FontStyles.Bold, uiFont);
        SetRect(statusText, new Vector2(0f, 165f), new Vector2(620f, 56f));

        StyleText(joinCodeText, 27f, TextColor, FontStyles.Bold, uiFont);
        SetRect(joinCodeText, new Vector2(0f, 105f), new Vector2(620f, 48f));

        StyleButton(createRoomButton, new Vector2(0f, 35f), "방 만들기", true);
        StyleButton(joinRoomButton, new Vector2(0f, -35f), "방 참가", true);
        StyleInput(joinCodeInput, new Vector2(0f, -105f));
        StylePlayerCount(canvasRect, uiFont);
        StyleButton(backButton, new Vector2(0f, -225f), "뒤로가기", false);
        StyleButton(leaveRoomButton, new Vector2(0f, -225f), "방 나가기", false);
    }

    private static void ConfigureCanvas(Canvas canvas)
    {
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

    private static void EnsureArtwork(RectTransform canvasRect)
    {
        Transform existing = canvasRect.Find("EntryArtworkRuntime");
        GameObject artwork = existing != null
            ? existing.gameObject
            : new GameObject("EntryArtworkRuntime", typeof(RectTransform));
        artwork.transform.SetParent(canvasRect, false);
        artwork.transform.SetAsFirstSibling();
        artwork.SetActive(true);
        RectTransform artworkRect = (RectTransform)artwork.transform;
        Stretch(artworkRect);

        Transform backgroundTransform = artwork.transform.Find("JangsanForestBackground");
        Image background;
        if (backgroundTransform == null)
        {
            background = new GameObject(
                "JangsanForestBackground",
                typeof(RectTransform),
                typeof(Image),
                typeof(AspectRatioFitter),
                typeof(MainMenuBackdropMotion)
            ).GetComponent<Image>();
            background.transform.SetParent(artwork.transform, false);
        }
        else
        {
            background = backgroundTransform.GetComponent<Image>();
        }

        Stretch(background.rectTransform);
        background.sprite = Resources.Load<Sprite>(BackgroundResourcePath);
        background.color = new Color(0.72f, 0.78f, 0.9f, 1f);
        background.raycastTarget = false;
        AspectRatioFitter fitter = background.GetComponent<AspectRatioFitter>();
        if (background.sprite != null && fitter != null)
        {
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = background.sprite.rect.width / background.sprite.rect.height;
        }

        Transform shadeTransform = artwork.transform.Find("ReadabilityShade");
        Image shade;
        if (shadeTransform == null)
        {
            shade = new GameObject(
                "ReadabilityShade",
                typeof(RectTransform),
                typeof(Image)
            ).GetComponent<Image>();
            shade.transform.SetParent(artwork.transform, false);
        }
        else
        {
            shade = shadeTransform.GetComponent<Image>();
        }

        Stretch(shade.rectTransform);
        shade.color = new Color(0f, 0.015f, 0.04f, 0.58f);
        shade.raycastTarget = false;

        artwork.transform.SetAsFirstSibling();
    }

    private static void EnsureBackdrop(RectTransform canvasRect)
    {
        Transform existing = canvasRect.Find("EntryBackdrop");
        GameObject backdrop = existing != null
            ? existing.gameObject
            : new GameObject("EntryBackdrop", typeof(RectTransform), typeof(Image));

        RectTransform rect = backdrop.GetComponent<RectTransform>();
        rect.SetParent(canvasRect, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(720f, 640f);

        Image image = backdrop.GetComponent<Image>();
        image.color = PanelColor;
        image.raycastTarget = false;
        backdrop.transform.SetSiblingIndex(
            Mathf.Min(1, canvasRect.childCount - 1)
        );

        Outline outline = backdrop.GetComponent<Outline>();
        if (outline == null)
        {
            outline = backdrop.AddComponent<Outline>();
        }

        outline.effectColor = PrimaryColor;
        outline.effectDistance = new Vector2(3f, -3f);
    }

    private static void EnsureHeading(RectTransform canvasRect, TMP_FontAsset uiFont)
    {
        Transform existing = canvasRect.Find("EntryHeading");
        TMP_Text heading;

        if (existing == null)
        {
            GameObject headingObject = new GameObject(
                "EntryHeading",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)
            );
            headingObject.transform.SetParent(canvasRect, false);
            heading = headingObject.GetComponent<TMP_Text>();
        }
        else
        {
            heading = existing.GetComponent<TMP_Text>();
        }

        heading.text = "멀티플레이";
        heading.alignment = TextAlignmentOptions.Center;
        heading.color = TextColor;
        heading.fontSize = 48f;
        heading.fontStyle = FontStyles.Bold;
        if (uiFont != null)
        {
            heading.font = uiFont;
        }
        heading.raycastTarget = false;
        SetRect(heading, new Vector2(0f, 255f), new Vector2(620f, 70f));
    }

    private static void StylePlayerCount(
        RectTransform canvasRect,
        TMP_FontAsset uiFont
    )
    {
        TMP_Text directText = canvasRect.Find("MultiplayerPanel/PlayerCountText")
            ?.GetComponent<TMP_Text>();
        TMP_Text[] texts = directText != null
            ? new[] { directText }
            : canvasRect.GetComponentsInChildren<TMP_Text>(true);

        foreach (TMP_Text candidate in texts)
        {
            if (candidate == null ||
                !candidate.text.TrimStart().StartsWith("접속 인원"))
            {
                continue;
            }

            StyleText(candidate, 20f, TextColor, FontStyles.Bold, uiFont);
            SetRect(candidate, new Vector2(0f, -165f), new Vector2(500f, 38f));
            break;
        }
    }

    private static void StyleButton(
        Button button,
        Vector2 position,
        string label,
        bool primary
    )
    {
        if (button == null)
        {
            return;
        }

        SetRect(button, position, new Vector2(500f, 58f));

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = primary ? ButtonColor : new Color32(16, 54, 74, 255);
            image.raycastTarget = true;
            button.targetGraphic = image;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(119, 220, 244, 255);
        colors.pressedColor = new Color32(44, 146, 180, 255);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color32(65, 77, 91, 210);
        colors.colorMultiplier = 1f;
        button.colors = colors;

        if (button.GetComponent<MainMenuButtonMotion>() == null)
        {
            button.gameObject.AddComponent<MainMenuButtonMotion>();
        }
        button.onClick.AddListener(GameAudioController.PlayButtonClick);

        TMP_Text[] buttonTexts = button.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text buttonText in buttonTexts)
        {
            buttonText.text = label;
            buttonText.color = TextColor;
            buttonText.faceColor = TextColor;
            buttonText.enableVertexGradient = false;
            buttonText.colorGradient = new VertexGradient(TextColor);
            buttonText.fontSize = 28f;
            buttonText.fontStyle = FontStyles.Bold;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.raycastTarget = false;
        }

        Text[] legacyTexts = button.GetComponentsInChildren<Text>(true);
        foreach (Text legacyText in legacyTexts)
        {
            legacyText.text = label;
            legacyText.color = TextColor;
            legacyText.fontSize = 28;
            legacyText.fontStyle = FontStyle.Bold;
            legacyText.alignment = TextAnchor.MiddleCenter;
            legacyText.raycastTarget = false;
        }
    }

    private static void StyleInput(TMP_InputField input, Vector2 position)
    {
        if (input == null)
        {
            return;
        }

        SetRect(input, position, new Vector2(500f, 58f));

        Image background = input.GetComponent<Image>();
        if (background != null)
        {
            background.color = new Color32(224, 242, 255, 255);
        }

        input.textComponent.color = new Color32(8, 30, 51, 255);
        input.textComponent.fontSize = 25f;
        input.textComponent.fontStyle = FontStyles.Bold;

        if (input.placeholder is TMP_Text placeholder)
        {
            placeholder.text = "참가 코드 6자리 입력";
            placeholder.color = new Color32(75, 103, 126, 210);
            placeholder.fontSize = 22f;
        }
    }

    private static void StyleText(
        TMP_Text text,
        float fontSize,
        Color color,
        FontStyles style,
        TMP_FontAsset uiFont
    )
    {
        if (text == null)
        {
            return;
        }

        text.color = color;
        text.faceColor = color;
        text.enableVertexGradient = false;
        text.colorGradient = new VertexGradient(color);
        text.fontSize = fontSize;
        text.fontStyle = style;
        if (uiFont != null)
        {
            text.font = uiFont;
        }
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;
    }

    private static void SetRect(Component component, Vector2 position, Vector2 size)
    {
        if (component == null || component.transform is not RectTransform rect)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void RegisterCallbacks()
    {
        if (callbacksRegistered)
        {
            return;
        }

        if (sessionState != null)
        {
            sessionState.NetworkStateChanged +=
                HandleNetworkStateChanged;
        }

        if (lobbySceneLoader != null)
        {
            lobbySceneLoader.StatusChanged += SetStatus;
        }

        callbacksRegistered = true;
    }

    private void UnregisterCallbacks()
    {
        if (!callbacksRegistered)
        {
            return;
        }

        if (sessionState != null)
        {
            sessionState.NetworkStateChanged -=
                HandleNetworkStateChanged;
        }

        if (lobbySceneLoader != null)
        {
            lobbySceneLoader.StatusChanged -= SetStatus;
        }

        callbacksRegistered = false;
    }

    private void HandleNetworkStateChanged(NetworkState state)
    {
        SetStatus($"네트워크 상태: {state}");
    }

    private void ShowJoinCode(string joinCode)
    {
        if (joinCodeText != null)
        {
            joinCodeText.text =
                $"참가 코드: <font=\"{JoinCodeFontName}\">" +
                $"{joinCode}</font>";
        }
    }

    private void ClearJoinCode()
    {
        if (joinCodeText != null)
        {
            joinCodeText.text = string.Empty;
        }

        if (joinCodeInput != null)
        {
            joinCodeInput.text = string.Empty;
        }
    }

    private void RefreshControls()
    {
        ResolveReferences();

        bool hasSession =
            sessionState != null && sessionState.HasSession;

        bool transitionInProgress =
            lobbySceneLoader != null &&
            lobbySceneLoader.IsTransitionInProgress;

        bool canStartSession =
            servicesReady &&
            !initializationInProgress &&
            !operationInProgress &&
            !transitionInProgress &&
            !hasSession;

        if (createRoomButton != null)
        {
            createRoomButton.gameObject.SetActive(!hasSession);
            createRoomButton.interactable = canStartSession;
        }

        if (joinRoomButton != null)
        {
            joinRoomButton.gameObject.SetActive(!hasSession);
            joinRoomButton.interactable = canStartSession;
        }

        if (joinCodeInput != null)
        {
            joinCodeInput.gameObject.SetActive(!hasSession);
            joinCodeInput.interactable = canStartSession;
        }

        if (leaveRoomButton != null)
        {
            leaveRoomButton.gameObject.SetActive(hasSession);
            leaveRoomButton.interactable =
                hasSession &&
                !operationInProgress;
        }

        if (backButton != null)
        {
            backButton.gameObject.SetActive(!hasSession);
            // Returning to the main menu must remain available while the
            // online service or scene transition is still being prepared.
            backButton.interactable = !operationInProgress;
        }
    }

    private void SetStatus(string message)
    {
        if (isDestroyed)
        {
            return;
        }

        if (statusText != null)
        {
            statusText.text = message;
            if (message.Contains("실패") ||
                message.Contains("오류") ||
                message.Contains("초과") ||
                message.Contains("종료"))
            {
                statusText.color = new Color32(255, 108, 116, 255);
                statusText.faceColor = statusText.color;
            }
            else if (message.Contains("완료"))
            {
                statusText.color = new Color32(118, 235, 184, 255);
                statusText.faceColor = statusText.color;
            }
            else
            {
                statusText.color = TextColor;
                statusText.faceColor = TextColor;
            }
        }

        Debug.Log(message);
    }

    private void OnDestroy()
    {
        isDestroyed = true;
        UnregisterCallbacks();

        // 씬 전환 중에는 세션을 비우거나 LeaveAsync를 호출하지 않는다.
        // NetworkManager의 NetworkSessionState가 다음 씬까지 유지한다.
    }
}
