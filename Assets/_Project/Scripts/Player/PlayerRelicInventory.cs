using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerRelicInventory : MonoBehaviour
{
    public const int MaximumRelics = 3;
    private const int CandidateCount = 3;

    private static readonly HashSet<PlayerRelicInventory> ActiveInventories =
        new();
    private static readonly Dictionary<ulong, PlayerRelicInventory>
        InventoriesBySource = new();

    [SerializeField]
    private RelicData[] relicPool = Array.Empty<RelicData>();

    [SerializeField]
    private LayerMask enemyLayer;

    private readonly List<RelicId> ownedRelics = new();
    private readonly List<RelicId> localCandidates = new();
    private readonly Dictionary<RelicId, float> nextRelicTriggerTimes = new();

    private NetworkPlayerRelics networkRelics;
    private RelicEffectExecutor effectExecutor;
    private RelicChoicePanelPresenter choicePresenter;
    private bool localChoiceActive;
    private bool localHasSelected;
    private int localSessionId;
    private int pendingLocalRewards;
    private float localChoiceDuration;
    private double localDeadline;
    private ulong registeredSourceId;
    private bool sourceRegistered;

    public event Action StateChanged;

    public bool ChoiceActive => IsNetworkControlled
        ? networkRelics.ChoiceActive
        : localChoiceActive;
    public bool HasSelected => IsNetworkControlled
        ? networkRelics.HasSelected
        : localHasSelected;
    public int ChoiceSessionId => IsNetworkControlled
        ? networkRelics.ChoiceSessionId
        : localSessionId;
    public double ChoiceDeadline => IsNetworkControlled
        ? networkRelics.ChoiceDeadline
        : localDeadline;
    public int CurrentCandidateCount => IsNetworkControlled
        ? networkRelics.CandidateCount
        : localCandidates.Count;
    public int OwnedCount => ownedRelics.Count;
    public bool HasRelic(RelicId id) =>
        id != RelicId.None && ownedRelics.Contains(id);
    public bool CanRunAuthoritativeEffects =>
        !IsNetworkControlled || networkRelics.IsServer;

    private bool IsNetworkControlled =>
        networkRelics != null && networkRelics.IsSpawned;

    private void Awake()
    {
        networkRelics = GetComponent<NetworkPlayerRelics>();
        effectExecutor = GetComponent<RelicEffectExecutor>();

        if (effectExecutor == null)
        {
            effectExecutor = gameObject.AddComponent<RelicEffectExecutor>();
        }

        effectExecutor.Configure(this, enemyLayer);

        if (networkRelics == null)
        {
            RegisterSource(ulong.MaxValue);
        }
    }

    private void OnEnable()
    {
        ActiveInventories.Add(this);

        if (!sourceRegistered)
        {
            if (networkRelics != null && networkRelics.IsSpawned)
            {
                RegisterSource(networkRelics.OwnerClientId);
            }
            else if (networkRelics == null)
            {
                RegisterSource(ulong.MaxValue);
            }
        }

        GameplayPauseState.SetRelicChoiceActive(this, ChoiceActive);
    }

    private void OnDisable()
    {
        ActiveInventories.Remove(this);
        GameplayPauseState.SetRelicChoiceActive(this, false);
        UnregisterSource();
    }

    private void Update()
    {
        if (IsNetworkControlled ||
            !localChoiceActive ||
            localHasSelected ||
            Time.unscaledTimeAsDouble < localDeadline)
        {
            return;
        }

        ApplyLocalChoice(UnityEngine.Random.Range(0, localCandidates.Count));
    }

    public static bool TryGet(
        ulong sourcePlayerId,
        out PlayerRelicInventory inventory
    )
    {
        if (InventoriesBySource.TryGetValue(sourcePlayerId, out inventory) &&
            inventory != null)
        {
            return true;
        }

        InventoriesBySource.Remove(sourcePlayerId);
        inventory = null;
        return false;
    }

    public static PlayerRelicInventory[] GetActiveSnapshot()
    {
        ActiveInventories.RemoveWhere(candidate => candidate == null);
        PlayerRelicInventory[] snapshot =
            new PlayerRelicInventory[ActiveInventories.Count];
        ActiveInventories.CopyTo(snapshot);
        return snapshot;
    }

    public void BindNetwork(NetworkPlayerRelics configuredNetworkRelics)
    {
        networkRelics = configuredNetworkRelics;
        RegisterSource(configuredNetworkRelics.OwnerClientId);
        NotifyNetworkStateChanged();
    }

    public void UnbindNetwork()
    {
        GameplayPauseState.SetRelicChoiceActive(this, false);
        UnregisterSource();
    }

    public void BeginLocalChoice(float duration)
    {
        if (IsNetworkControlled ||
            ownedRelics.Count >= MaximumRelics)
        {
            return;
        }

        int reservedRewards = pendingLocalRewards +
            (localChoiceActive ? 1 : 0);

        if (reservedRewards >= MaximumRelics - ownedRelics.Count)
        {
            return;
        }

        localChoiceDuration = Mathf.Max(1f, duration);

        if (localChoiceActive)
        {
            pendingLocalRewards++;
            return;
        }

        StartLocalChoice();
    }

    private void StartLocalChoice()
    {
        if (ownedRelics.Count >= MaximumRelics)
        {
            return;
        }

        List<RelicId> candidates = GenerateCandidates();

        if (candidates.Count < CandidateCount)
        {
            return;
        }

        localCandidates.Clear();
        localCandidates.AddRange(candidates);
        localSessionId++;
        localDeadline = Time.unscaledTimeAsDouble + localChoiceDuration;
        localHasSelected = false;
        localChoiceActive = true;
        NotifyStateChanged();
    }

    public void RequestChoice(int index)
    {
        if (IsNetworkControlled)
        {
            networkRelics.RequestChoice(index);
            return;
        }

        ApplyLocalChoice(index);
    }

    public RelicId GetCandidate(int index)
    {
        if (IsNetworkControlled)
        {
            return networkRelics.GetCandidate(index);
        }

        return index >= 0 && index < localCandidates.Count
            ? localCandidates[index]
            : RelicId.None;
    }

    public RelicData GetRelicData(RelicId id)
    {
        foreach (RelicData relic in relicPool)
        {
            if (relic != null && relic.Id == id)
            {
                return relic;
            }
        }

        return null;
    }

    public RelicData GetOwnedRelic(int index)
    {
        return index >= 0 && index < ownedRelics.Count
            ? GetRelicData(ownedRelics[index])
            : null;
    }

    public List<RelicId> GenerateCandidates()
    {
        List<RelicId> available = new();

        foreach (RelicData relic in relicPool)
        {
            if (relic == null ||
                relic.Id == RelicId.None ||
                ownedRelics.Contains(relic.Id) ||
                available.Contains(relic.Id))
            {
                continue;
            }

            available.Add(relic.Id);
        }

        for (int index = available.Count - 1; index > 0; index--)
        {
            int swapIndex = UnityEngine.Random.Range(0, index + 1);
            (available[index], available[swapIndex]) =
                (available[swapIndex], available[index]);
        }

        if (available.Count > CandidateCount)
        {
            available.RemoveRange(CandidateCount, available.Count - CandidateCount);
        }

        return available;
    }

    public bool TryAddRelic(RelicId id)
    {
        if (id == RelicId.None ||
            ownedRelics.Count >= MaximumRelics ||
            ownedRelics.Contains(id) ||
            GetRelicData(id) == null)
        {
            return false;
        }

        ownedRelics.Add(id);
        return true;
    }

    public void ReplaceOwnedRelics(IEnumerable<int> syncedRelics)
    {
        ownedRelics.Clear();

        foreach (int relicValue in syncedRelics)
        {
            RelicId id = (RelicId)relicValue;

            if (id != RelicId.None &&
                ownedRelics.Count < MaximumRelics &&
                !ownedRelics.Contains(id))
            {
                ownedRelics.Add(id);
            }
        }

        NotifyStateChanged();
    }

    public bool TryGrantAdditionalPierce()
    {
        return CanRunAuthoritativeEffects &&
            HasRelic(RelicId.DivineArrowhead);
    }

    public void HandleDirectHit(
        Transform primaryTarget,
        Vector2 hitPosition,
        float directDamage,
        ElementType element,
        RelicAttackType attackType
    )
    {
        if (!CanRunAuthoritativeEffects)
        {
            return;
        }

        RelicId relicId = attackType switch
        {
            RelicAttackType.Projectile => RelicId.ThunderFragment,
            RelicAttackType.DamageOverTime => RelicId.WindTalisman,
            RelicAttackType.Area => RelicId.BrokenCannon,
            RelicAttackType.Melee => RelicId.GeneralJade,
            RelicAttackType.Dash => RelicId.GeneralJade,
            _ => RelicId.None
        };

        TryExecuteHitRelic(
            relicId,
            primaryTarget,
            hitPosition,
            directDamage,
            element
        );
    }

    public void HandleDirectKill(
        Vector2 killPosition,
        float directDamage,
        ElementType element
    )
    {
        RelicData relic = GetRelicData(RelicId.GoblinFire);

        if (CanRunAuthoritativeEffects && TryRoll(relic))
        {
            effectExecutor.ExecuteKillEffect(
                relic,
                killPosition,
                directDamage,
                registeredSourceId,
                element
            );
        }
    }

    public void ShowEffect(
        RelicId id,
        Vector2 origin,
        Vector2 target,
        float radius,
        float duration,
        ElementType element
    )
    {
        if (IsNetworkControlled)
        {
            networkRelics.ShowEffectServer(
                id,
                origin,
                target,
                radius,
                duration,
                element
            );
            return;
        }

        RelicEffectVisuals.Play(id, origin, target, radius, duration, element);
    }

    public void NotifyNetworkStateChanged()
    {
        NotifyStateChanged();
    }

    private void ApplyLocalChoice(int index)
    {
        if (!localChoiceActive ||
            localHasSelected ||
            index < 0 ||
            index >= localCandidates.Count ||
            !TryAddRelic(localCandidates[index]))
        {
            return;
        }

        localHasSelected = true;
        localChoiceActive = false;

        if (pendingLocalRewards > 0 && ownedRelics.Count < MaximumRelics)
        {
            pendingLocalRewards--;
            StartLocalChoice();
            return;
        }

        NotifyStateChanged();
    }

    private void TryExecuteHitRelic(
        RelicId id,
        Transform primaryTarget,
        Vector2 hitPosition,
        float directDamage,
        ElementType element
    )
    {
        RelicData relic = GetRelicData(id);

        if (TryRoll(relic))
        {
            effectExecutor.ExecuteHitEffect(
                relic,
                primaryTarget,
                hitPosition,
                directDamage,
                registeredSourceId,
                element
            );
        }
    }

    private bool TryRoll(RelicData relic)
    {
        if (relic == null ||
            !HasRelic(relic.Id) ||
            (nextRelicTriggerTimes.TryGetValue(relic.Id, out float readyTime) &&
                Time.time < readyTime) ||
            UnityEngine.Random.value >= relic.TriggerChance)
        {
            return false;
        }

        nextRelicTriggerTimes[relic.Id] =
            Time.time + relic.InternalCooldown;
        return true;
    }

    private void NotifyStateChanged()
    {
        GameplayPauseState.SetRelicChoiceActive(this, ChoiceActive);

        bool isLocalViewer = !IsNetworkControlled || networkRelics.IsOwner;

        if (isLocalViewer && choicePresenter == null && ChoiceActive)
        {
            choicePresenter = gameObject.AddComponent<RelicChoicePanelPresenter>();
            choicePresenter.Bind(this);
        }

        StateChanged?.Invoke();
        choicePresenter?.Refresh();
    }

    private void RegisterSource(ulong sourcePlayerId)
    {
        UnregisterSource();
        registeredSourceId = sourcePlayerId;
        InventoriesBySource[sourcePlayerId] = this;
        sourceRegistered = true;
    }

    private void UnregisterSource()
    {
        if (sourceRegistered &&
            InventoriesBySource.TryGetValue(
                registeredSourceId,
                out PlayerRelicInventory registered
            ) &&
            registered == this)
        {
            InventoriesBySource.Remove(registeredSourceId);
        }

        sourceRegistered = false;
    }
}
