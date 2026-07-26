using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

public class NetworkTestUI : MonoBehaviour
{
    [Header("Relay UI")]

    [SerializeField]
    private TMP_InputField joinCodeInput;

    [SerializeField]
    private TMP_Text joinCodeText;

    [SerializeField]
    private TMP_Text statusText;

    private bool servicesReady;
    private bool operationInProgress;

    private async void Start()
    {
        await InitializeServicesAsync();
    }

    /// <summary>
    /// Unity Gaming Services를 초기화하고
    /// 현재 플레이어를 익명 계정으로 로그인한다.
    /// </summary>
    private async Task InitializeServicesAsync()
    {
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

        operationInProgress = true;

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

            var session =
                await MultiplayerService.Instance
                    .CreateSessionAsync(options);

            string joinCode = session.Code;

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
        }
        finally
        {
            operationInProgress = false;
        }
    }

    /// <summary>
    /// 입력한 참가 코드로 기존 Relay 세션에 참가한다.
    /// 이 플레이어가 Client가 된다.
    /// </summary>
    public async void JoinRelaySession()
    {
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

        try
        {
            SetStatus("방 참가 중...");

            await MultiplayerService.Instance
                .JoinSessionByCodeAsync(joinCode);

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
        }
        finally
        {
            operationInProgress = false;
        }
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
}