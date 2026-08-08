using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class MultiplayerLobbyUI : MonoBehaviour
{
    private const string JoinCodeFontName = "LiberationSans SDF";

    [Header("로비 정보")]
    [SerializeField] private TMP_Text joinCodeText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private TMP_Text statusText;

    [Header("로비 버튼")]
    [SerializeField] private Button startGameButton;

    [Header("네트워크 상태")]
    [SerializeField] private NetworkLobbyState networkLobbyState;

    private NetworkSessionState sessionState;
    private NetworkGameFlowController gameFlowController;
    private bool lobbyStateSubscribed;

    private IEnumerator Start()
    {
        float waitTime = 0f;

        while (NetworkManager.Singleton == null &&
               waitTime < 5f)
        {
            waitTime += Time.unscaledDeltaTime;
            yield return null;
        }

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager를 찾지 못했습니다.");
            yield break;
        }

        sessionState =
            NetworkManager.Singleton.GetComponent<NetworkSessionState>();

        if (sessionState != null)
        {
            sessionState.SessionChanged += RefreshJoinCode;
        }
        else
        {
            Debug.LogError(
                "NetworkManager에 NetworkSessionState가 없습니다."
            );
        }

        gameFlowController =
            NetworkManager.Singleton.GetComponent<NetworkGameFlowController>();

        RefreshJoinCode();

        waitTime = 0f;

        while (networkLobbyState != null &&
               !networkLobbyState.IsSpawned &&
               waitTime < 5f)
        {
            waitTime += Time.unscaledDeltaTime;
            yield return null;
        }

        if (networkLobbyState != null &&
            networkLobbyState.IsSpawned)
        {
            networkLobbyState.StateChanged += RefreshPlayerCount;
            lobbyStateSubscribed = true;
            RefreshPlayerCount();
        }
        else
        {
            Debug.LogError(
                "NetworkLobbyState가 네트워크에 Spawn되지 않았습니다."
            );
        }

        if (gameFlowController == null)
        {
            Debug.LogError(
                "NetworkManager에 NetworkGameFlowController가 없습니다."
            );
            yield break;
        }

        gameFlowController.BindLobbyUI(
            startGameButton,
            null,
            statusText
        );
    }

    private void OnDestroy()
    {
        if (sessionState != null)
        {
            sessionState.SessionChanged -= RefreshJoinCode;
        }

        if (lobbyStateSubscribed && networkLobbyState != null)
        {
            networkLobbyState.StateChanged -= RefreshPlayerCount;
            lobbyStateSubscribed = false;
        }

        if (gameFlowController != null)
        {
            gameFlowController.UnbindLobbyUI(startGameButton);
        }
    }

    public void RefreshJoinCode()
    {
        if (joinCodeText == null)
        {
            return;
        }

        string joinCode =
            sessionState != null
                ? sessionState.JoinCode
                : string.Empty;

        joinCodeText.text =
            string.IsNullOrWhiteSpace(joinCode)
                ? "참가 코드 없음"
                : $"참가 코드: <font=\"{JoinCodeFontName}\">" +
                  $"{joinCode}</font>";
    }

    private void RefreshPlayerCount()
    {
        if (playerCountText == null || networkLobbyState == null)
        {
            return;
        }

        playerCountText.text =
            $"접속 인원: {networkLobbyState.ConnectedPlayerCount}/" +
            NetworkLobbyState.MaximumPlayerSlots;
    }
}
