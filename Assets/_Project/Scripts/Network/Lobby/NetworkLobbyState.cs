using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// MultiPlayerLobby 씬의 2인용 캐릭터 선택 상태다.
/// 클라이언트는 선택 요청만 보내고 최종 상태는 서버가 기록한다.
/// 맵 선택은 추후 별도 상태 컴포넌트로 확장한다.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public sealed class NetworkLobbyState : NetworkBehaviour
{
    public const ulong EmptyClientId = ulong.MaxValue;
    public const int MaximumPlayerSlots = 2;

    [SerializeField, Min(1)]
    private int availableCharacterCount = 1;

    private readonly NetworkVariable<ulong> firstClientId =
        new NetworkVariable<ulong>(EmptyClientId);

    private readonly NetworkVariable<int> firstCharacterId =
        new NetworkVariable<int>(-1);

    private readonly NetworkVariable<ulong> secondClientId =
        new NetworkVariable<ulong>(EmptyClientId);

    private readonly NetworkVariable<int> secondCharacterId =
        new NetworkVariable<int>(-1);

    public int ConnectedPlayerCount
    {
        get
        {
            int count = 0;

            if (firstClientId.Value != EmptyClientId)
            {
                count++;
            }

            if (secondClientId.Value != EmptyClientId)
            {
                count++;
            }

            return count;
        }
    }

    public event Action StateChanged;

    private bool serverCallbacksRegistered;

    public override void OnNetworkSpawn()
    {
        SubscribeNetworkVariables();

        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback +=
                HandleClientConnected;

            NetworkManager.OnClientDisconnectCallback +=
                HandleClientDisconnected;

            serverCallbacksRegistered = true;
            SynchronizeSlotsWithConnectedClients();
        }

        NotifyStateChanged();
    }

    public override void OnNetworkDespawn()
    {
        UnsubscribeNetworkVariables();

        if (NetworkManager != null && serverCallbacksRegistered)
        {
            NetworkManager.OnClientConnectedCallback -=
                HandleClientConnected;

            NetworkManager.OnClientDisconnectCallback -=
                HandleClientDisconnected;

            serverCallbacksRegistered = false;
        }
    }

    public void RequestCharacterSelection(int characterId)
    {
        if (IsSpawned)
        {
            SetCharacterRpc(characterId);
        }
    }

    public bool TryGetSlot(
        int slotIndex,
        out ulong clientId,
        out int characterId
    )
    {
        switch (slotIndex)
        {
            case 0:
                clientId = firstClientId.Value;
                characterId = firstCharacterId.Value;
                return clientId != EmptyClientId;

            case 1:
                clientId = secondClientId.Value;
                characterId = secondCharacterId.Value;
                return clientId != EmptyClientId;

            default:
                clientId = EmptyClientId;
                characterId = -1;
                return false;
        }
    }

    public bool TryGetPlayerSelection(
        ulong clientId,
        out int characterId
    )
    {
        int slotIndex = FindSlotIndex(clientId);

        if (slotIndex < 0)
        {
            characterId = -1;
            return false;
        }

        return TryGetSlot(
            slotIndex,
            out _,
            out characterId
        );
    }

    [Rpc(SendTo.Server)]
    private void SetCharacterRpc(
        int characterId,
        RpcParams rpcParams = default
    )
    {
        if (characterId < 0 ||
            characterId >= availableCharacterCount)
        {
            return;
        }

        ulong senderClientId =
            rpcParams.Receive.SenderClientId;

        int slotIndex = EnsureClientHasSlot(senderClientId);

        if (slotIndex < 0 ||
            GetCharacterId(slotIndex) == characterId)
        {
            return;
        }

        SetCharacterId(slotIndex, characterId);
    }

    private void HandleClientConnected(ulong clientId)
    {
        AssignClientToEmptySlot(clientId);
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        int slotIndex = FindSlotIndex(clientId);

        if (slotIndex >= 0)
        {
            ClearSlot(slotIndex);
        }
    }

    private void SynchronizeSlotsWithConnectedClients()
    {
        if (firstClientId.Value != EmptyClientId &&
            !IsClientConnected(firstClientId.Value))
        {
            ClearSlot(0);
        }

        if (secondClientId.Value != EmptyClientId &&
            !IsClientConnected(secondClientId.Value))
        {
            ClearSlot(1);
        }

        foreach (ulong clientId in
                 NetworkManager.ConnectedClientsIds)
        {
            AssignClientToEmptySlot(clientId);
        }
    }

    private bool IsClientConnected(ulong clientId)
    {
        foreach (ulong connectedClientId in
                 NetworkManager.ConnectedClientsIds)
        {
            if (connectedClientId == clientId)
            {
                return true;
            }
        }

        return false;
    }

    private int EnsureClientHasSlot(ulong clientId)
    {
        int existingSlot = FindSlotIndex(clientId);

        return existingSlot >= 0
            ? existingSlot
            : AssignClientToEmptySlot(clientId);
    }

    private int AssignClientToEmptySlot(ulong clientId)
    {
        int existingSlot = FindSlotIndex(clientId);

        if (existingSlot >= 0)
        {
            return existingSlot;
        }

        if (firstClientId.Value == EmptyClientId)
        {
            firstCharacterId.Value = -1;
            firstClientId.Value = clientId;
            return 0;
        }

        if (secondClientId.Value == EmptyClientId)
        {
            secondCharacterId.Value = -1;
            secondClientId.Value = clientId;
            return 1;
        }

        Debug.LogWarning(
            $"로비 슬롯이 가득 차 Client {clientId}를 배치하지 못했습니다."
        );

        return -1;
    }

    private int FindSlotIndex(ulong clientId)
    {
        if (firstClientId.Value == clientId)
        {
            return 0;
        }

        if (secondClientId.Value == clientId)
        {
            return 1;
        }

        return -1;
    }

    private int GetCharacterId(int slotIndex)
    {
        return slotIndex == 0
            ? firstCharacterId.Value
            : secondCharacterId.Value;
    }

    private void SetCharacterId(int slotIndex, int characterId)
    {
        if (slotIndex == 0)
        {
            firstCharacterId.Value = characterId;
        }
        else
        {
            secondCharacterId.Value = characterId;
        }
    }

    private void ClearSlot(int slotIndex)
    {
        if (slotIndex == 0)
        {
            firstClientId.Value = EmptyClientId;
            firstCharacterId.Value = -1;
        }
        else
        {
            secondClientId.Value = EmptyClientId;
            secondCharacterId.Value = -1;
        }
    }

    private void SubscribeNetworkVariables()
    {
        firstClientId.OnValueChanged += HandleClientIdChanged;
        firstCharacterId.OnValueChanged += HandleCharacterIdChanged;
        secondClientId.OnValueChanged += HandleClientIdChanged;
        secondCharacterId.OnValueChanged += HandleCharacterIdChanged;
    }

    private void UnsubscribeNetworkVariables()
    {
        firstClientId.OnValueChanged -= HandleClientIdChanged;
        firstCharacterId.OnValueChanged -= HandleCharacterIdChanged;
        secondClientId.OnValueChanged -= HandleClientIdChanged;
        secondCharacterId.OnValueChanged -= HandleCharacterIdChanged;
    }

    private void HandleClientIdChanged(ulong previous, ulong current)
    {
        NotifyStateChanged();
    }

    private void HandleCharacterIdChanged(int previous, int current)
    {
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }
}
