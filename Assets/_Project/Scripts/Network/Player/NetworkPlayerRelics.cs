using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(PlayerRelicInventory))]
public sealed class NetworkPlayerRelics : NetworkBehaviour
{
    private const int MaximumCandidates = 3;

    [SerializeField, Min(1f)]
    private float choiceDuration = 15f;

    private readonly NetworkVariable<bool> choiceActive = new();
    private readonly NetworkVariable<bool> hasSelected = new();
    private readonly NetworkVariable<int> choiceSessionId = new();
    private readonly NetworkVariable<double> choiceDeadline = new();
    private readonly NetworkVariable<int> candidateCount = new();
    private readonly NetworkVariable<int> firstCandidate = new();
    private readonly NetworkVariable<int> secondCandidate = new();
    private readonly NetworkVariable<int> thirdCandidate = new();

    private NetworkList<int> ownedRelics;
    private PlayerRelicInventory inventory;
    private int pendingRewardCount;

    public bool ChoiceActive => choiceActive.Value;
    public bool HasSelected => hasSelected.Value;
    public int ChoiceSessionId => choiceSessionId.Value;
    public double ChoiceDeadline => choiceDeadline.Value;
    public int CandidateCount => candidateCount.Value;

    private void Awake()
    {
        ownedRelics = new NetworkList<int>();
        inventory = GetComponent<PlayerRelicInventory>();
    }

    public override void OnNetworkSpawn()
    {
        choiceActive.OnValueChanged += HandleStateChanged;
        hasSelected.OnValueChanged += HandleStateChanged;
        choiceSessionId.OnValueChanged += HandleStateChanged;
        choiceDeadline.OnValueChanged += HandleDeadlineChanged;
        candidateCount.OnValueChanged += HandleStateChanged;
        firstCandidate.OnValueChanged += HandleStateChanged;
        secondCandidate.OnValueChanged += HandleStateChanged;
        thirdCandidate.OnValueChanged += HandleStateChanged;
        ownedRelics.OnListChanged += HandleOwnedRelicsChanged;

        inventory.BindNetwork(this);

        if (IsServer)
        {
            ResetRelicsServer();
        }

        SynchronizeOwnedRelics();
        inventory.NotifyNetworkStateChanged();
    }

    public override void OnNetworkDespawn()
    {
        choiceActive.OnValueChanged -= HandleStateChanged;
        hasSelected.OnValueChanged -= HandleStateChanged;
        choiceSessionId.OnValueChanged -= HandleStateChanged;
        choiceDeadline.OnValueChanged -= HandleDeadlineChanged;
        candidateCount.OnValueChanged -= HandleStateChanged;
        firstCandidate.OnValueChanged -= HandleStateChanged;
        secondCandidate.OnValueChanged -= HandleStateChanged;
        thirdCandidate.OnValueChanged -= HandleStateChanged;
        ownedRelics.OnListChanged -= HandleOwnedRelicsChanged;

        inventory.UnbindNetwork();
    }

    private void Update()
    {
        if (!IsServer ||
            !choiceActive.Value ||
            hasSelected.Value ||
            GetServerTime() < choiceDeadline.Value)
        {
            return;
        }

        ApplyChoiceServer(Random.Range(0, candidateCount.Value));
    }

    public bool BeginChoiceServer()
    {
        if (!IsServer ||
            ownedRelics.Count >= PlayerRelicInventory.MaximumRelics)
        {
            return false;
        }

        int reservedRewards = pendingRewardCount +
            (choiceActive.Value ? 1 : 0);

        if (reservedRewards >=
            PlayerRelicInventory.MaximumRelics - ownedRelics.Count)
        {
            return false;
        }

        if (choiceActive.Value)
        {
            pendingRewardCount++;
            return true;
        }

        return StartChoiceServer();
    }

    private bool StartChoiceServer()
    {
        if (!IsServer ||
            ownedRelics.Count >= PlayerRelicInventory.MaximumRelics)
        {
            return false;
        }

        List<RelicId> candidates = inventory.GenerateCandidates();

        if (candidates.Count < MaximumCandidates)
        {
            return false;
        }

        firstCandidate.Value = (int)candidates[0];
        secondCandidate.Value = (int)candidates[1];
        thirdCandidate.Value = (int)candidates[2];
        candidateCount.Value = MaximumCandidates;
        choiceSessionId.Value++;
        choiceDeadline.Value = GetServerTime() + Mathf.Max(1f, choiceDuration);
        hasSelected.Value = false;
        choiceActive.Value = true;
        inventory.NotifyNetworkStateChanged();
        return true;
    }

    public void RequestChoice(int index)
    {
        if (!IsOwner || !choiceActive.Value || hasSelected.Value)
        {
            return;
        }

        RequestChoiceRpc(choiceSessionId.Value, index);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void RequestChoiceRpc(int sessionId, int index)
    {
        if (!choiceActive.Value ||
            hasSelected.Value ||
            sessionId != choiceSessionId.Value ||
            GetServerTime() >= choiceDeadline.Value)
        {
            return;
        }

        ApplyChoiceServer(index);
    }

    public RelicId GetCandidate(int index)
    {
        return index switch
        {
            0 => (RelicId)firstCandidate.Value,
            1 => (RelicId)secondCandidate.Value,
            2 => (RelicId)thirdCandidate.Value,
            _ => RelicId.None
        };
    }

    public void ShowEffectServer(
        RelicId id,
        Vector2 origin,
        Vector2 target,
        float radius,
        float duration
    )
    {
        if (IsServer)
        {
            PlayEffectRpc((int)id, origin, target, radius, duration);
        }
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    private void PlayEffectRpc(
        int relicValue,
        Vector2 origin,
        Vector2 target,
        float radius,
        float duration
    )
    {
        RelicEffectVisuals.Play(
            (RelicId)relicValue,
            origin,
            target,
            radius,
            duration
        );
    }

    private void ApplyChoiceServer(int index)
    {
        if (!IsServer ||
            !choiceActive.Value ||
            hasSelected.Value ||
            index < 0 ||
            index >= candidateCount.Value)
        {
            return;
        }

        RelicId selected = GetCandidate(index);

        if (!inventory.TryAddRelic(selected))
        {
            return;
        }

        ownedRelics.Add((int)selected);
        hasSelected.Value = true;
        choiceActive.Value = false;

        if (pendingRewardCount > 0 &&
            ownedRelics.Count < PlayerRelicInventory.MaximumRelics)
        {
            pendingRewardCount--;
            StartChoiceServer();
            return;
        }

        inventory.NotifyNetworkStateChanged();
    }

    private void ResetRelicsServer()
    {
        ownedRelics.Clear();
        choiceActive.Value = false;
        hasSelected.Value = false;
        candidateCount.Value = 0;
        firstCandidate.Value = 0;
        secondCandidate.Value = 0;
        thirdCandidate.Value = 0;
        pendingRewardCount = 0;
    }

    private void SynchronizeOwnedRelics()
    {
        List<int> synchronized = new(ownedRelics.Count);

        foreach (int relic in ownedRelics)
        {
            synchronized.Add(relic);
        }

        inventory.ReplaceOwnedRelics(synchronized);
    }

    private void HandleStateChanged(bool previous, bool current)
    {
        inventory.NotifyNetworkStateChanged();
    }

    private void HandleStateChanged(int previous, int current)
    {
        inventory.NotifyNetworkStateChanged();
    }

    private void HandleDeadlineChanged(double previous, double current)
    {
        inventory.NotifyNetworkStateChanged();
    }

    private void HandleOwnedRelicsChanged(NetworkListEvent<int> changeEvent)
    {
        SynchronizeOwnedRelics();
    }

    private static double GetServerTime()
    {
        NetworkManager manager = NetworkManager.Singleton;
        return manager != null && manager.IsListening
            ? manager.ServerTime.Time
            : Time.unscaledTimeAsDouble;
    }
}
