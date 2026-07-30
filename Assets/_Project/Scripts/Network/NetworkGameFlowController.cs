using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class NetworkGameFlowController : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    [SerializeField, Min(1)] private int minimumPlayers = 2;
    [SerializeField] private string gameplaySceneName;
    [SerializeField] private UnityEvent<int> onPlayerCountChanged;
    [SerializeField] private UnityEvent<bool> onStartAvailabilityChanged;
    [SerializeField] private UnityEvent<string> onStatusChanged;

    private bool callbacksRegistered;

    public int ConnectedPlayerCount =>
        networkManager != null && networkManager.IsListening
            ? networkManager.ConnectedClientsIds.Count
            : 0;

    public bool CanStartGame =>
        networkManager != null &&
        networkManager.IsServer &&
        networkManager.IsListening &&
        ConnectedPlayerCount >= minimumPlayers;

    private void Awake()
    {
        if (networkManager == null)
        {
            networkManager = NetworkManager.Singleton;
        }
    }

    private void Start()
    {
        RegisterCallbacks();
        RefreshState();
    }

    private void OnEnable()
    {
        RegisterCallbacks();
    }

    private void OnDisable()
    {
        UnregisterCallbacks();
    }

    public void StartGame()
    {
        if (networkManager == null)
        {
            SetStatus("NetworkManager를 찾을 수 없습니다.");
            return;
        }

        if (!networkManager.IsServer)
        {
            SetStatus("호스트만 게임을 시작할 수 있습니다.");
            return;
        }

        if (ConnectedPlayerCount < minimumPlayers)
        {
            SetStatus($"플레이어를 기다리는 중입니다. ({ConnectedPlayerCount}/{minimumPlayers})");
            return;
        }

        if (string.IsNullOrWhiteSpace(gameplaySceneName))
        {
            SetStatus("게임 씬 이름이 설정되지 않았습니다.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(gameplaySceneName))
        {
            SetStatus($"{gameplaySceneName} 씬이 Build Settings에 없습니다.");
            return;
        }

        SceneEventProgressStatus result = networkManager.SceneManager.LoadScene(
            gameplaySceneName,
            LoadSceneMode.Single
        );

        if (result != SceneEventProgressStatus.Started)
        {
            SetStatus($"게임 씬 전환을 시작하지 못했습니다: {result}");
            return;
        }

        SetStatus("게임 씬으로 이동합니다.");
    }

    private void RegisterCallbacks()
    {
        if (callbacksRegistered || networkManager == null)
        {
            return;
        }

        networkManager.OnClientConnectedCallback += HandleClientConnectionChanged;
        networkManager.OnClientDisconnectCallback += HandleClientConnectionChanged;
        callbacksRegistered = true;
    }

    private void UnregisterCallbacks()
    {
        if (!callbacksRegistered || networkManager == null)
        {
            return;
        }

        networkManager.OnClientConnectedCallback -= HandleClientConnectionChanged;
        networkManager.OnClientDisconnectCallback -= HandleClientConnectionChanged;
        callbacksRegistered = false;
    }

    private void HandleClientConnectionChanged(ulong clientId)
    {
        RefreshState();
    }

    private void RefreshState()
    {
        int playerCount = ConnectedPlayerCount;
        onPlayerCountChanged?.Invoke(playerCount);
        onStartAvailabilityChanged?.Invoke(CanStartGame);

        if (networkManager == null || !networkManager.IsListening)
        {
            SetStatus("네트워크 연결을 기다리는 중입니다.");
        }
        else if (CanStartGame)
        {
            SetStatus($"게임 시작 준비 완료 ({playerCount}/{minimumPlayers})");
        }
        else
        {
            SetStatus($"플레이어를 기다리는 중입니다. ({playerCount}/{minimumPlayers})");
        }
    }

    private void SetStatus(string message)
    {
        onStatusChanged?.Invoke(message);
        Debug.Log(message);
    }
}
