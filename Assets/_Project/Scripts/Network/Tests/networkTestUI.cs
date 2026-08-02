using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

public class NetworkTestUI : MonoBehaviour
{
    [Header("Relay UI")]

    [SerializeField]
    private TMP_InputField joinCodeInput;

    [SerializeField]
    private TMP_Text joinCodeText;

    [SerializeField]
    private TMP_Text statusText;

    [Header("Lobby Controls")]

    [SerializeField]
    private Button createRoomButton;

    [SerializeField]
    private Button joinRoomButton;

    [SerializeField]
    private Button leaveRoomButton;

    private bool servicesReady;
    private bool initializationInProgress;
    private bool operationInProgress;
    private bool isDestroyed;

    private ISession currentSession;
    private async void Start()
    {
        RefreshLobbyControls();
        await InitializeServicesAsync();
    }

    /// <summary>
    /// Unity Gaming Services를 초기화하고
    /// 현재 플레이어를 익명 계정으로 로그인한다.
    /// </summary>
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

            SetStatus(
                "온라인 서비스 준비 완료"
            );

            Debug.Log(
                $"익명 로그인 성공: " +
                $"{AuthenticationService.Instance.PlayerId}"
            );
        }
        catch (Exception exception)
        {
            servicesReady = false;

            SetStatus(
                "온라인 서비스 초기화 실패"
            );

            Debug.LogException(exception);
        }
        finally
        {
            initializationInProgress = false;
            RefreshLobbyControls();
        }
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

    /// <summary>
    /// Relay를 사용하는 2인 세션을 생성한다.
    /// 이 플레이어가 Host가 된다.
    /// </summary>
    public async void CreateRelaySession()
    {
        if (!CanStartOperation())
        {
            return;
        }
        if (currentSession != null)
        {
            SetStatus("이미 참가 중인 방이 있습니다. 먼저 나가주세요.");
            return;
        }
        operationInProgress = true;
        RefreshLobbyControls();

        try
        {
            SetStatus("방 생성 중...");

            SessionOptions options =
                new SessionOptions
                {
                    // 호스트를 포함한 최대 인원
                    MaxPlayers = 2
                }
                .WithRelayNetwork();

            ISession createdSession =
                await MultiplayerService.Instance
                    .CreateSessionAsync(options);
            AttachSession(createdSession);

            string joinCode = currentSession.Code;

            if (joinCodeText != null)
            {
                joinCodeText.text =
                    $"참가 코드: {joinCode}";
            }

            SetStatus(
                "방 생성 완료 - 코드를 팀원에게 보내세요."
            );

            Debug.Log(
                $"Relay 방 생성 완료. " +
                $"참가 코드: {joinCode}"
            );
        }
        catch (Exception exception)
        {
            SetStatus("방 생성 실패");
            Debug.LogException(exception);
            await CleanupFailedSessionAsync();
        }
        finally
        {
            operationInProgress = false;
            RefreshLobbyControls();
        }
    }

    /// <summary>
    /// 입력한 참가 코드로 기존 Relay 세션에 참가한다.
    /// 이 플레이어가 Client가 된다.
    /// </summary>
    public async void JoinRelaySession()
    {
        if (currentSession != null)
        {
            SetStatus("이미 참가 중인 방이 있습니다. 먼저 나가주세요.");
            return;
        }

        if (!CanStartOperation())
        {
            return;
        }

        if (joinCodeInput == null)
        {
            SetStatus(
                "Join Code Input이 연결되지 않았습니다."
            );

            Debug.LogError(
                "NetworkTestUI의 Join Code Input이 비어 있습니다."
            );

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
        RefreshLobbyControls();

        try
        {
            SetStatus("방 참가 중...");

            ISession joinedSession =
                await MultiplayerService.Instance
                    .JoinSessionByCodeAsync(joinCode);
            AttachSession(joinedSession);

            SetStatus("방 참가 완료");

            Debug.Log(
                $"Relay 방 참가 성공. 코드: {joinCode}"
            );
        }
        catch (Exception exception)
        {
            SetStatus(
                "방 참가 실패 - 코드와 호스트 상태를 확인하세요."
            );
            Debug.LogException(exception);
            await CleanupFailedSessionAsync();
        }
        finally
        {
            operationInProgress = false;
            RefreshLobbyControls();
        }
    }
    private void HandleNetworkStateChanged(
    NetworkState state
    )
    {
        if (isDestroyed)
        {
            return;
        }

        SetStatus(
            $"네트워크 상태: {state}"
        );

        Debug.Log(
            $"세션 네트워크 상태 변경: {state}"
        );
    }

    private bool CanStartOperation()
    {
        if (!servicesReady)
        {
            SetStatus(
                "온라인 서비스가 아직 준비되지 않았습니다."
            );

            return false;
        }

        if (operationInProgress)
        {
            SetStatus(
                "현재 네트워크 작업이 진행 중입니다."
            );

            return false;
        }

        return true;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }

        Debug.Log(message);
    }

    public async void LeaveRelaySession()
    {
        if (!CanStartOperation())
        {
            return;
        }

        if (currentSession == null)
        {
            SetStatus("참가 중인 방이 없습니다.");
            return;
        }

        operationInProgress = true;
        RefreshLobbyControls();

        try
        {
            SetStatus("방에서 나가는 중...");

            ISession leavingSession = currentSession;

            await leavingSession.LeaveAsync();

            DetachSession(leavingSession);
            ClearJoinCode();

            SetStatus("방에서 나왔습니다.");
        }
        catch (SessionException exception)
        {
            SetStatus($"방 나가기 실패: {exception.Message}");
            Debug.LogException(exception);
        }
        catch (Exception exception)
        {
            SetStatus($"방 나가기 중 오류: {exception.Message}");
            Debug.LogException(exception);
        }
        finally
        {
            operationInProgress = false;
            RefreshLobbyControls();
        }
    }


    public async void ReconnectRelaySession()
    {
        if (!CanStartOperation())
        {
            return;
        }

        if (currentSession == null)
        {
            SetStatus(
                "재접속할 세션 정보가 없습니다."
            );

            return;
        }

        operationInProgress = true;

        try
        {
            SetStatus("기존 세션에 재접속 중...");

            await currentSession.ReconnectAsync();

            SetStatus("세션 재접속 요청 완료");
        }
        catch (SessionException exception)
        {
            SetStatus(
                "재접속 실패 - 다시 참가 코드를 입력하세요."
            );

            Debug.LogException(exception);
        }
        catch (Exception exception)
        {
            SetStatus("재접속 중 오류 발생");

            Debug.LogException(exception);
        }
        finally
        {
            operationInProgress = false;
        }
    }
    private void OnDestroy()
    {
        isDestroyed = true;

        if (currentSession != null)
        {
            DetachSession(currentSession);
        }
    }

    private void AttachSession(ISession session)
    {
        if (session == null)
        {
            throw new ArgumentNullException(nameof(session));
        }

        if (currentSession != null)
        {
            DetachSession(currentSession);
        }

        currentSession = session;
        currentSession.Network.StateChanged += HandleNetworkStateChanged;
        RefreshLobbyControls();
    }

    private void DetachSession(ISession session)
    {
        if (session?.Network != null)
        {
            session.Network.StateChanged -= HandleNetworkStateChanged;
        }

        if (ReferenceEquals(currentSession, session))
        {
            currentSession = null;
        }

        RefreshLobbyControls();
    }

    private async Task CleanupFailedSessionAsync()
    {
        ISession failedSession = currentSession;

        if (failedSession == null)
        {
            return;
        }

        try
        {
            await failedSession.LeaveAsync();
        }
        catch (Exception cleanupException)
        {
            Debug.LogWarning(
                $"실패한 세션 정리 중 오류: {cleanupException.Message}"
            );
        }
        finally
        {
            DetachSession(failedSession);
            ClearJoinCode();
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

    private void RefreshLobbyControls()
    {
        bool hasSession = currentSession != null;
        bool canStartSession =
            servicesReady &&
            !initializationInProgress &&
            !operationInProgress &&
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
                hasSession && !operationInProgress;
        }
    }
}
