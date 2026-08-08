using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class NetworkGameFlowController : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    [SerializeField, Min(1)] private int minimumPlayers = 1;
    [SerializeField, Min(1)] private int maximumPlayers = 2;
    [SerializeField] private string gameplaySceneName;
    [SerializeField] private UnityEvent<int> onPlayerCountChanged;
    [SerializeField] private UnityEvent<bool> onStartAvailabilityChanged;
    [SerializeField] private UnityEvent<string> onStatusChanged;
    [SerializeField] private Button startGameButton;
    [SerializeField] private TMP_Text playerCountText;
    private TMP_Text statusText;

    private bool callbacksRegistered;
    private bool lobbyUiBound;
    private bool sceneLoadInProgress;

    public int ConnectedPlayerCount =>
        networkManager != null && networkManager.IsListening
            ? networkManager.ConnectedClientsIds.Count
            : 0;

    public bool CanStartGame =>
        networkManager != null &&
        networkManager.IsServer &&
        networkManager.IsListening &&
        !sceneLoadInProgress &&
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

    public void BindLobbyUI(
        Button button,
        TMP_Text countText,
        TMP_Text lobbyStatusText)
    {
        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveListener(StartGame);
        }

        startGameButton = button;
        playerCountText = countText;
        statusText = lobbyStatusText;
        lobbyUiBound = true;

        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveListener(StartGame);
            startGameButton.onClick.AddListener(StartGame);
            startGameButton.gameObject.SetActive(true);
        }

        RefreshState();
    }

    public void UnbindLobbyUI(Button button)
    {
        if (startGameButton != button)
            return;

        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveListener(StartGame);
        }

        startGameButton = null;
        playerCountText = null;
        statusText = null;
        lobbyUiBound = false;
    }


    public void StartGame()
    {
        if (sceneLoadInProgress)
        {
            return;
        }

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

        sceneLoadInProgress = true;

        if (startGameButton != null)
        {
            startGameButton.interactable = false;
        }

        NetworkStageBootstrap.CachePlayerPrefab(
            networkManager.NetworkConfig.PlayerPrefab
        );

        foreach (NetworkClient client in networkManager.ConnectedClientsList)
        {
            if (client.PlayerObject == null)
            {
                continue;
            }

            client.PlayerObject.DestroyWithScene = false;
            DontDestroyOnLoad(client.PlayerObject.gameObject);
        }

        SceneEventProgressStatus result = networkManager.SceneManager.LoadScene(
            gameplaySceneName,
            LoadSceneMode.Single
        );

        if (result != SceneEventProgressStatus.Started)
        {
            sceneLoadInProgress = false;
            RefreshState();
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
        bool canStartGame = CanStartGame;

        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(
                lobbyUiBound ||
                (networkManager != null &&
                 networkManager.IsListening &&
                 networkManager.IsServer)
            );
            startGameButton.interactable = canStartGame;
        }

        if (playerCountText != null)
        {
            playerCountText.text =
                $"접속 인원: {playerCount}/{maximumPlayers}";
        }

        onPlayerCountChanged?.Invoke(playerCount);
        onStartAvailabilityChanged?.Invoke(canStartGame);

        if (networkManager == null || !networkManager.IsListening)
        {
            SetStatus("네트워크 연결을 기다리는 중입니다.");
        }
        else if (canStartGame)
        {
            SetStatus($"게임 시작 준비 완료 ({playerCount}/{maximumPlayers})");
        }
        else if (!networkManager.IsServer)
        {
            if (lobbyUiBound)
            {
                SetStatus("호스트가 게임을 시작하기를 기다리는 중입니다.");
            }
            else
            {
                SetStatus(
                    $"호스트가 게임을 시작하기를 기다리는 중입니다. " +
                    $"({playerCount}/{maximumPlayers})"
                );
            }
        }
        else
        {
            SetStatus($"플레이어를 기다리는 중입니다. ({playerCount}/{maximumPlayers})");
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }

        onStatusChanged?.Invoke(message);
        Debug.Log(message);
    }
}
