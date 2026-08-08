using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 로비 나가기와 호스트 연결 종료 시의 로컬 복귀만 담당한다.
/// 네트워크가 종료된 다음에는 일반 SceneManager로 Entry 씬을 연다.
/// </summary>
public sealed class MultiplayerLobbyExitController : MonoBehaviour
{
    [SerializeField]
    private NetworkManager networkManager;

    [SerializeField]
    private NetworkSessionState sessionState;

    [SerializeField]
    private string multiplayerEntrySceneName = "MultiPlayerEntry";

    public bool IsExitInProgress { get; private set; }

    public event Action<string> StatusChanged;

    private bool callbackRegistered;
    private bool applicationIsQuitting;
    private ulong localClientId;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        RegisterCallback();
    }

    private void Start()
    {
        ResolveReferences();
        RegisterCallback();

        if (networkManager != null && networkManager.IsListening)
        {
            localClientId = networkManager.LocalClientId;
        }
    }

    private void OnDisable()
    {
        UnregisterCallback();
    }

    public async void LeaveLobby()
    {
        if (IsExitInProgress)
        {
            return;
        }

        await ReturnToEntryAsync(leaveSession: true);
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (applicationIsQuitting ||
            IsExitInProgress ||
            networkManager == null ||
            networkManager.IsServer ||
            clientId != localClientId)
        {
            return;
        }

        ReturnAfterHostDisconnect();
    }

    private async void ReturnAfterHostDisconnect()
    {
        SetStatus("호스트 연결이 종료되어 멀티 입장 화면으로 돌아갑니다.");
        await ReturnToEntryAsync(leaveSession: true);
    }

    private async Task ReturnToEntryAsync(bool leaveSession)
    {
        IsExitInProgress = true;
        SetStatus("멀티 로비에서 나가는 중...");

        try
        {
            ResolveReferences();

            if (leaveSession &&
                sessionState != null &&
                sessionState.HasSession)
            {
                try
                {
                    await sessionState.LeaveSessionAsync();
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(
                        $"세션 나가기 중 오류: {exception.Message}"
                    );
                }
            }

            if (networkManager != null && networkManager.IsListening)
            {
                networkManager.Shutdown();
            }

            // NGO 2.13의 NetworkManager는 시작되면 DDOL 씬으로 이동한다.
            // Shutdown만 하면 다음 Entry 씬의 NetworkManager와 중복될 수 있으므로
            // 기존 매니저 오브젝트까지 제거한 뒤 새 Entry 씬을 연다.
            if (networkManager != null)
            {
                Destroy(networkManager.gameObject);
                networkManager = null;
                callbackRegistered = false;
                await Task.Yield();
            }

            if (!Application.CanStreamedLevelBeLoaded(
                    multiplayerEntrySceneName
                ))
            {
                SetStatus(
                    $"{multiplayerEntrySceneName} 씬이 " +
                    "Build Profiles의 Scene List에 없습니다."
                );
                IsExitInProgress = false;
                return;
            }

            SceneManager.LoadScene(
                multiplayerEntrySceneName,
                LoadSceneMode.Single
            );
        }
        catch (Exception exception)
        {
            IsExitInProgress = false;
            SetStatus($"멀티 로비 나가기 실패: {exception.Message}");
            Debug.LogException(exception);
        }
    }

    private void ResolveReferences()
    {
        if (networkManager == null)
        {
            networkManager = NetworkManager.Singleton;
        }

        if (sessionState == null && networkManager != null)
        {
            sessionState =
                networkManager.GetComponent<NetworkSessionState>();
        }
    }

    private void RegisterCallback()
    {
        if (callbackRegistered || networkManager == null)
        {
            return;
        }

        if (networkManager.IsListening)
        {
            localClientId = networkManager.LocalClientId;
        }

        networkManager.OnClientDisconnectCallback +=
            HandleClientDisconnected;

        callbackRegistered = true;
    }

    private void UnregisterCallback()
    {
        if (!callbackRegistered || networkManager == null)
        {
            return;
        }

        networkManager.OnClientDisconnectCallback -=
            HandleClientDisconnected;

        callbackRegistered = false;
    }

    private void SetStatus(string message)
    {
        StatusChanged?.Invoke(message);
        Debug.Log(message);
    }

    private void OnApplicationQuit()
    {
        applicationIsQuitting = true;
    }
}
