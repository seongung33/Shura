using Unity.Netcode;
using UnityEngine;

public class NetworkTestUI : MonoBehaviour
{
    public void StartHost()
    {
        if (!CanStartNetwork())
        {
            return;
        }

        bool success =
            NetworkManager.Singleton.StartHost();

        Debug.Log(
            success
                ? "호스트 시작 성공"
                : "호스트 시작 실패"
        );
    }

    public void StartClient()
    {
        if (!CanStartNetwork())
        {
            return;
        }

        bool success =
            NetworkManager.Singleton.StartClient();

        Debug.Log(
            success
                ? "클라이언트 시작 요청"
                : "클라이언트 시작 실패"
        );
    }

    public void ShutdownNetwork()
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        if (!NetworkManager.Singleton.IsListening)
        {
            return;
        }

        NetworkManager.Singleton.Shutdown();

        Debug.Log("네트워크 종료");
    }

    private bool CanStartNetwork()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError(
                "씬에서 NetworkManager를 찾을 수 없습니다."
            );

            return false;
        }

        if (NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning(
                "네트워크가 이미 실행 중입니다."
            );

            return false;
        }

        return true;
    }
}