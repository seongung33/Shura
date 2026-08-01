using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NetworkGameResultState))]
public class NetworkGameResultActions : NetworkBehaviour
{
    [SerializeField]
    private string gameplaySceneName = "Main";

    [SerializeField]
    private string lobbySceneName = "NetworkTest";

    [SerializeField, Min(0f)]
    private float shutdownDelay = 0.25f;

    private NetworkGameResultState resultState;
    private bool transitionStarted;

    public bool CanControlResult =>
        IsSpawned && IsOwner && IsServer && resultState.HasFinished;

    private void Awake()
    {
        resultState = GetComponent<NetworkGameResultState>();
    }

    public void RestartGame()
    {
        if (!CanControlResult || transitionStarted)
        {
            return;
        }

        if (!CanLoadScene(gameplaySceneName))
        {
            return;
        }

        SceneEventProgressStatus status = NetworkManager.SceneManager.LoadScene(
            gameplaySceneName,
            LoadSceneMode.Single
        );

        if (status != SceneEventProgressStatus.Started)
        {
            Debug.LogError($"네트워크 게임 재시작 실패: {status}");
            return;
        }

        transitionStarted = true;
        Time.timeScale = 1f;
        resultState.ResetResultServer();
    }

    public void ReturnToLobby()
    {
        if (!CanControlResult || transitionStarted)
        {
            return;
        }

        if (!CanLoadScene(lobbySceneName))
        {
            return;
        }

        transitionStarted = true;
        ReturnToLobbyRpc();
    }

    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    private void ReturnToLobbyRpc()
    {
        if (!transitionStarted)
        {
            transitionStarted = true;
        }

        NetworkLobbyReturnLoader.Begin(
            NetworkManager,
            lobbySceneName,
            shutdownDelay
        );
    }

    private static bool CanLoadScene(string sceneName)
    {
        if (!string.IsNullOrWhiteSpace(sceneName) &&
            Application.CanStreamedLevelBeLoaded(sceneName))
        {
            return true;
        }

        Debug.LogError($"Build Settings에서 씬을 찾을 수 없습니다: {sceneName}");
        return false;
    }
}

internal sealed class NetworkLobbyReturnLoader : MonoBehaviour
{
    public static void Begin(
        NetworkManager networkManager,
        string lobbySceneName,
        float shutdownDelay
    )
    {
        GameObject loaderObject = new GameObject("NetworkLobbyReturnLoader");
        DontDestroyOnLoad(loaderObject);

        NetworkLobbyReturnLoader loader =
            loaderObject.AddComponent<NetworkLobbyReturnLoader>();

        loader.StartCoroutine(
            loader.ShutdownAndLoadLobby(
                networkManager,
                lobbySceneName,
                shutdownDelay
            )
        );
    }

    private IEnumerator ShutdownAndLoadLobby(
        NetworkManager networkManager,
        string lobbySceneName,
        float shutdownDelay
    )
    {
        yield return new WaitForSecondsRealtime(shutdownDelay);

        Time.timeScale = 1f;

        if (networkManager != null)
        {
            if (networkManager.IsListening)
            {
                networkManager.Shutdown();
            }

            Destroy(networkManager.gameObject);
        }

        yield return null;
        SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
        Destroy(gameObject);
    }
}
