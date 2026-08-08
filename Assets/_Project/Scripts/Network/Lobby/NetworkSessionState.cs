using System;
using System.Threading.Tasks;
using Unity.Services.Multiplayer;
using UnityEngine;

/// <summary>
/// 정식 멀티플레이 흐름의 Relay 세션 참조를 보관한다.
/// NetworkManager와 같은 오브젝트에 배치되어 네트워크 씬 전환 동안 유지된다.
/// </summary>
[DisallowMultipleComponent]
public sealed class NetworkSessionState : MonoBehaviour
{
    public ISession CurrentSession { get; private set; }

    public bool HasSession => CurrentSession != null;

    public string JoinCode =>
        CurrentSession != null
            ? CurrentSession.Code
            : string.Empty;

    public event Action SessionChanged;
    public event Action<NetworkState> NetworkStateChanged;

    public void AttachSession(ISession session)
    {
        if (session == null)
        {
            throw new ArgumentNullException(nameof(session));
        }

        if (CurrentSession != null &&
            !ReferenceEquals(CurrentSession, session))
        {
            throw new InvalidOperationException(
                "이미 다른 멀티플레이 세션이 연결되어 있습니다."
            );
        }

        DetachNetworkCallback(CurrentSession);
        CurrentSession = session;
        AttachNetworkCallback(CurrentSession);
        SessionChanged?.Invoke();
    }

    public async Task LeaveSessionAsync()
    {
        ISession leavingSession = CurrentSession;

        if (leavingSession == null)
        {
            return;
        }

        try
        {
            await leavingSession.LeaveAsync();
        }
        finally
        {
            ClearSession(leavingSession);
        }
    }

    public void ClearSession(ISession expectedSession = null)
    {
        if (expectedSession != null &&
            !ReferenceEquals(CurrentSession, expectedSession))
        {
            return;
        }

        if (CurrentSession == null)
        {
            return;
        }

        DetachNetworkCallback(CurrentSession);
        CurrentSession = null;
        SessionChanged?.Invoke();
    }

    private void AttachNetworkCallback(ISession session)
    {
        if (session?.Network != null)
        {
            session.Network.StateChanged += HandleNetworkStateChanged;
        }
    }

    private void DetachNetworkCallback(ISession session)
    {
        if (session?.Network != null)
        {
            session.Network.StateChanged -= HandleNetworkStateChanged;
        }
    }

    private void HandleNetworkStateChanged(NetworkState state)
    {
        NetworkStateChanged?.Invoke(state);
    }

    private void OnDestroy()
    {
        DetachNetworkCallback(CurrentSession);
        CurrentSession = null;
    }
}
