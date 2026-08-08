using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class NetworkLobbySceneLoader : MonoBehaviour
{
    [SerializeField]
    private NetworkManager networkManager;

    [SerializeField]
    private string lobbySceneName = "MultiPlayerLobby";

    [SerializeField, Min(1f)]
    private float networkReadyTimeoutSeconds = 10f;

    public bool IsTransitionInProgress { get; private set; }

    public event Action<string> StatusChanged;

    private bool callbackRegistered;

    private void Awake()
    {
        ResolveNetworkManager();
        RegisterCallback();
    }

    private void Start()
    {
        ResolveNetworkManager();
        RegisterCallback();

        // 로더보다 먼저 호스트가 시작된 경우도 처리
        if (networkManager != null &&
            networkManager.IsListening &&
            networkManager.IsServer)
        {
            EnterLobbyWhenReady();
        }
    }

    private void OnDestroy()
    {
        if (callbackRegistered && networkManager != null)
        {
            networkManager.OnServerStarted -= HandleServerStarted;
        }
    }

    private void HandleServerStarted()
    {
        EnterLobbyWhenReady();
    }

    public void EnterLobbyWhenReady()
    {
        if (IsTransitionInProgress)
            return;

        ResolveNetworkManager();

        if (networkManager == null)
        {
            SetStatus("NetworkManager를 찾지 못했습니다.");
            return;
        }

        if (string.IsNullOrWhiteSpace(lobbySceneName))
        {
            SetStatus("멀티 로비 씬 이름이 설정되지 않았습니다.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(lobbySceneName))
        {
            SetStatus(
                $"{lobbySceneName} 씬이 Build Profiles의 Scene List에 없습니다."
            );
            return;
        }

        IsTransitionInProgress = true;
        StartCoroutine(EnterLobbyCoroutine());
    }

    private IEnumerator EnterLobbyCoroutine()
    {
        float remainingSeconds = networkReadyTimeoutSeconds;

        SetStatus("호스트 네트워크 준비를 기다리는 중입니다.");

        while (!CanHostLoadLobby() && remainingSeconds > 0f)
        {
            remainingSeconds -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (!CanHostLoadLobby())
        {
            IsTransitionInProgress = false;
            SetStatus("호스트 네트워크 준비 시간이 초과되었습니다.");
            yield break;
        }

        NetworkSessionState sessionState =
            networkManager.GetComponent<NetworkSessionState>();

        if (sessionState == null)
        {
            IsTransitionInProgress = false;
            SetStatus("NetworkSessionState를 찾지 못했습니다.");
            yield break;
        }

        remainingSeconds = networkReadyTimeoutSeconds;

        SetStatus("참가 코드 준비를 기다리는 중입니다.");

        while (string.IsNullOrWhiteSpace(sessionState.JoinCode) &&
               remainingSeconds > 0f)
        {
            remainingSeconds -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (string.IsNullOrWhiteSpace(sessionState.JoinCode))
        {
            IsTransitionInProgress = false;
            SetStatus("참가 코드 준비 시간이 초과되었습니다.");
            yield break;
        }

        PreserveConnectedPlayerObjects();

        SceneEventProgressStatus result =
            networkManager.SceneManager.LoadScene(
                lobbySceneName,
                LoadSceneMode.Single
            );

        if (result != SceneEventProgressStatus.Started)
        {
            IsTransitionInProgress = false;
            SetStatus($"멀티 로비 이동 실패: {result}");
            yield break;
        }

        SetStatus("멀티 로비로 이동합니다.");
    }

    private bool CanHostLoadLobby()
    {
        return networkManager != null &&
               networkManager.IsListening &&
               networkManager.IsServer &&
               networkManager.SceneManager != null &&
               networkManager.ConnectedClientsIds.Count > 0;
    }

    private void PreserveConnectedPlayerObjects()
    {
        foreach (NetworkClient client in
                 networkManager.ConnectedClientsList)
        {
            NetworkObject playerObject = client.PlayerObject;

            if (playerObject == null)
                continue;

            playerObject.DestroyWithScene = false;
            DontDestroyOnLoad(playerObject.gameObject);
        }
    }

    private void ResolveNetworkManager()
    {
        if (networkManager == null)
        {
            networkManager = NetworkManager.Singleton;
        }
    }

    private void RegisterCallback()
    {
        if (callbackRegistered || networkManager == null)
            return;

        networkManager.OnServerStarted += HandleServerStarted;
        callbackRegistered = true;
    }

    private void SetStatus(string message)
    {
        StatusChanged?.Invoke(message);
        Debug.Log(message);
    }
}