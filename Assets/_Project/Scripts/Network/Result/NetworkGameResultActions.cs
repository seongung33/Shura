using System.Collections;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NetworkGameResultState))]
public class NetworkGameResultActions : NetworkBehaviour
{
    [SerializeField]
    private string gameplaySceneName = "Main";

    [SerializeField]
    private string multiplayerEntrySceneName = "MultiPlayerEntry";

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

        if (!CanLoadScene(multiplayerEntrySceneName))
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
            multiplayerEntrySceneName,
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
        string multiplayerEntrySceneName,
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
                multiplayerEntrySceneName,
                shutdownDelay
            )
        );
    }

    private IEnumerator ShutdownAndLoadLobby(
        NetworkManager networkManager,
        string multiplayerEntrySceneName,
        float shutdownDelay
    )
    {
        yield return new WaitForSecondsRealtime(shutdownDelay);

        Time.timeScale = 1f;

        NetworkSessionState sessionState = networkManager != null
            ? networkManager.GetComponent<NetworkSessionState>()
            : null;

        if (sessionState != null && sessionState.HasSession)
        {
            Task leaveTask = sessionState.LeaveSessionAsync();

            while (!leaveTask.IsCompleted)
            {
                yield return null;
            }

            if (leaveTask.IsFaulted)
            {
                Debug.LogWarning(
                    "결과 화면에서 세션 나가기 중 오류가 발생했습니다: " +
                    leaveTask.Exception?.GetBaseException().Message
                );
            }
        }

        if (networkManager != null)
        {
            if (networkManager.IsListening)
            {
                networkManager.Shutdown();
            }

            Destroy(networkManager.gameObject);
        }

        yield return null;
        SceneManager.LoadScene(
            multiplayerEntrySceneName,
            LoadSceneMode.Single
        );
        Destroy(gameObject);
    }
}
