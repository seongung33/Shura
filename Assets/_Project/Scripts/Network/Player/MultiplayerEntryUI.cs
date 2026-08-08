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
        ResolveReferences();
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
                !operationInProgress &&
                !transitionInProgress;
        }

        if (backButton != null)
        {
            backButton.gameObject.SetActive(!hasSession);
            backButton.interactable =
                !operationInProgress &&
                !transitionInProgress;
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
