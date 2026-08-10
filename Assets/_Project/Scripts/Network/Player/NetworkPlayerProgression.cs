using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(PlayerRuntimeGrowth))]
public sealed class NetworkPlayerProgression : NetworkBehaviour,
    ILevelUpChoiceSource
{
    private const int MaximumCards = 3;
    private const int MaximumOwnedSkills = 3;

    private static readonly List<NetworkPlayerProgression> SpawnedPlayers =
        new();

    private readonly NetworkVariable<bool> choiceActive = new();
    private readonly NetworkVariable<bool> hasSelected = new();
    private readonly NetworkVariable<int> candidateCount = new();
    private readonly NetworkVariable<int> choiceTeamLevel = new();
    private readonly NetworkVariable<int> choiceSessionId = new();
    private readonly NetworkVariable<double> choiceDeadline = new();

    private readonly NetworkVariable<LevelUpCandidateState> firstCandidate =
        new();
    private readonly NetworkVariable<LevelUpCandidateState> secondCandidate =
        new();
    private readonly NetworkVariable<LevelUpCandidateState> thirdCandidate =
        new();

    private readonly NetworkVariable<PlayerGrowthNetworkState> growthState =
        new(PlayerGrowthNetworkState.Default);

    private NetworkList<SkillProgressNetworkState> skillStates;
    private readonly List<SkillData> skillPool = new();
    private readonly List<int> initialOwnedSkillIndices = new();

    private LevelUpSettings settings = new();
    private PlayerRuntimeGrowth runtimeGrowth;
    private AutoSkillCaster autoSkillCaster;
    private LevelUpPanelPresenter panelPresenter;
    private CharacterData configuredCharacter;
    private bool serverStateInitialized;

    public bool ChoiceActive => choiceActive.Value;
    public bool HasSelected => hasSelected.Value;
    public int ChoiceTeamLevel => choiceTeamLevel.Value;
    public int ChoiceSessionId => choiceSessionId.Value;
    public double ChoiceDeadline => choiceDeadline.Value;
    public int CandidateCount => candidateCount.Value;
    public LevelUpSettings Settings => settings;
    public bool CanParticipateInLevelUp =>
        IsSpawned && configuredCharacter != null;

    private void Awake()
    {
        skillStates = new NetworkList<SkillProgressNetworkState>();
        runtimeGrowth = GetComponent<PlayerRuntimeGrowth>();
        autoSkillCaster = GetComponent<AutoSkillCaster>();
    }

    private void Update()
    {
        // 씬 전환이나 네트워크 변수 초기 동기화 순서 때문에 콜백을 놓쳐도
        // 소유자의 활성 레벨업 UI는 다음 프레임에 반드시 복구한다.
        if (IsOwner && choiceActive.Value && panelPresenter == null)
        {
            RefreshOwnerPanel();
        }
    }

    public override void OnNetworkSpawn()
    {
        choiceActive.OnValueChanged += HandleChoiceActiveChanged;
        hasSelected.OnValueChanged += HandleChoiceStateChanged;
        candidateCount.OnValueChanged += HandleChoiceStateChanged;
        choiceTeamLevel.OnValueChanged += HandleChoiceStateChanged;
        choiceSessionId.OnValueChanged += HandleChoiceStateChanged;
        choiceDeadline.OnValueChanged += HandleChoiceDeadlineChanged;
        firstCandidate.OnValueChanged += HandleCandidateChanged;
        secondCandidate.OnValueChanged += HandleCandidateChanged;
        thirdCandidate.OnValueChanged += HandleCandidateChanged;
        growthState.OnValueChanged += HandleGrowthChanged;
        skillStates.OnListChanged += HandleSkillListChanged;

        if (!SpawnedPlayers.Contains(this))
        {
            SpawnedPlayers.Add(this);
        }

        GameplayPauseState.SetLevelUpActive(this, choiceActive.Value);
        TeamLevelUpCoordinator.Active?.Register(this);

        if (IsServer && configuredCharacter != null && !serverStateInitialized)
        {
            ResetProgressionServer();
        }
        else
        {
            RefreshRuntimeState();
        }

        RefreshOwnerPanel();
    }

    public override void OnNetworkDespawn()
    {
        choiceActive.OnValueChanged -= HandleChoiceActiveChanged;
        hasSelected.OnValueChanged -= HandleChoiceStateChanged;
        candidateCount.OnValueChanged -= HandleChoiceStateChanged;
        choiceTeamLevel.OnValueChanged -= HandleChoiceStateChanged;
        choiceSessionId.OnValueChanged -= HandleChoiceStateChanged;
        choiceDeadline.OnValueChanged -= HandleChoiceDeadlineChanged;
        firstCandidate.OnValueChanged -= HandleCandidateChanged;
        secondCandidate.OnValueChanged -= HandleCandidateChanged;
        thirdCandidate.OnValueChanged -= HandleCandidateChanged;
        growthState.OnValueChanged -= HandleGrowthChanged;
        skillStates.OnListChanged -= HandleSkillListChanged;

        TeamLevelUpCoordinator.Active?.Unregister(this);
        GameplayPauseState.SetLevelUpActive(this, false);
        SpawnedPlayers.Remove(this);

        if (panelPresenter != null)
        {
            Destroy(panelPresenter);
        }
    }

    public void ConfigureSettings(LevelUpSettings configuredSettings)
    {
        settings = configuredSettings ?? new LevelUpSettings();
        runtimeGrowth.Configure(settings);
        RefreshRuntimeState();
        RefreshOwnerPanel();
    }

    public void ConfigureCharacter(CharacterData character)
    {
        configuredCharacter = character;
        runtimeGrowth.ConfigureBasicSkill(character != null
            ? character.BasicSkill
            : null);
        skillPool.Clear();
        initialOwnedSkillIndices.Clear();

        if (character == null)
        {
            RefreshRuntimeState();
            return;
        }

        IReadOnlyList<SkillData> availableSkills = character.LevelUpSkills;

        if (character.BasicSkill != null)
        {
            skillPool.Add(character.BasicSkill);
            initialOwnedSkillIndices.Add(0);
        }

        if (availableSkills != null)
        {
            foreach (SkillData skill in availableSkills)
            {
                if (skill != null && !skillPool.Contains(skill))
                {
                    skillPool.Add(skill);
                }
            }
        }

        // 명시적인 레벨업 풀이 없는 기존 캐릭터만 시작 스킬을 Lv1로 가져온다.
        if (!character.UsesLevelUpSkillPool && character.StartingSkills != null)
        {
            foreach (SkillData startingSkill in character.StartingSkills)
            {
                int index = skillPool.IndexOf(startingSkill);

                if (index >= 0 && !initialOwnedSkillIndices.Contains(index))
                {
                    initialOwnedSkillIndices.Add(index);
                }
            }
        }

        if (IsServer && IsSpawned)
        {
            ResetProgressionServer();
        }
        else
        {
            RefreshRuntimeState();
        }
    }

    public bool BeginChoiceServer(
        int teamLevel,
        int sessionId,
        double deadline,
        bool isSkillChoiceLevel
    )
    {
        if (!IsServer || configuredCharacter == null)
        {
            return false;
        }

        PlayerGrowthNetworkState state = growthState.Value;
        state.TeamLevel = Mathf.Max(state.TeamLevel, teamLevel);
        growthState.Value = state;

        List<LevelUpCandidateState> generated = GenerateCandidates(
            isSkillChoiceLevel
        );

        if (generated.Count == 0)
        {
            return false;
        }

        firstCandidate.Value = GetOrEmpty(generated, 0);
        secondCandidate.Value = GetOrEmpty(generated, 1);
        thirdCandidate.Value = GetOrEmpty(generated, 2);
        candidateCount.Value = Mathf.Min(MaximumCards, generated.Count);
        choiceTeamLevel.Value = teamLevel;
        choiceDeadline.Value = deadline;
        hasSelected.Value = false;
        choiceSessionId.Value = sessionId;
        choiceActive.Value = true;

        GameplayPauseState.SetLevelUpActive(this, true);
        RefreshOwnerPanel();
        return true;
    }

    public void EndChoiceServer()
    {
        if (!IsServer)
        {
            return;
        }

        choiceActive.Value = false;
        GameplayPauseState.SetLevelUpActive(this, false);
        RefreshOwnerPanel();
    }

    public void AutoSelectServer()
    {
        if (!IsServer || !choiceActive.Value || hasSelected.Value ||
            candidateCount.Value <= 0)
        {
            return;
        }

        int index = Random.Range(0, candidateCount.Value);
        ApplyCandidateServer(index);
    }

    public void CompletePendingChoiceServer()
    {
        if (IsServer)
        {
            GetComponent<NetworkPlayerExperience>()?.CompleteChoiceServer();
        }
    }

    public void ResetProgressionServer()
    {
        if (!IsServer)
        {
            return;
        }

        growthState.Value = PlayerGrowthNetworkState.Default;
        skillStates.Clear();
        serverStateInitialized = true;

        foreach (int skillIndex in initialOwnedSkillIndices)
        {
            if (skillIndex < 0 || skillIndex >= skillPool.Count)
            {
                continue;
            }

            skillStates.Add(new SkillProgressNetworkState
            {
                SkillPoolIndex = skillIndex,
                Level = 1,
                Element = GetSkillData(skillIndex) == configuredCharacter.BasicSkill
                    ? ElementType.None
                    : settings.GetRandomAllowedElement()
            });
        }

        hasSelected.Value = false;
        candidateCount.Value = 0;
        choiceActive.Value = false;
        RefreshRuntimeState();
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
            GetServerTime() >= choiceDeadline.Value ||
            index < 0 ||
            index >= candidateCount.Value)
        {
            return;
        }

        ApplyCandidateServer(index);
    }

    public LevelUpCandidateState GetCandidate(int index)
    {
        switch (index)
        {
            case 0: return firstCandidate.Value;
            case 1: return secondCandidate.Value;
            case 2: return thirdCandidate.Value;
            default: return default;
        }
    }

    public SkillData GetSkillData(int poolIndex)
    {
        return poolIndex >= 0 && poolIndex < skillPool.Count
            ? skillPool[poolIndex]
            : null;
    }

    public int GetCurrentSkillLevel(int poolIndex)
    {
        return TryGetSkillState(poolIndex, out SkillProgressNetworkState state)
            ? state.Level
            : 0;
    }

    public string GetSelectionStatusText()
    {
        List<NetworkPlayerProgression> participants = new();

        foreach (NetworkPlayerProgression player in SpawnedPlayers)
        {
            if (player != null && player.IsSpawned &&
                player.choiceActive.Value &&
                player.choiceSessionId.Value == choiceSessionId.Value)
            {
                participants.Add(player);
            }
        }

        participants.Sort(
            (left, right) => left.OwnerClientId.CompareTo(right.OwnerClientId)
        );

        StringBuilder builder = new();

        foreach (NetworkPlayerProgression participant in participants)
        {
            if (builder.Length > 0)
            {
                builder.Append("    ");
            }

            builder.Append(participant.IsOwner ? "나: " : "상대: ");
            builder.Append(participant.hasSelected.Value ? "완료" : "선택 중");
        }

        return builder.ToString();
    }

    private List<LevelUpCandidateState> GenerateCandidates(
        bool isSkillChoiceLevel
    )
    {
        List<LevelUpCandidateState> generated = new(MaximumCards);

        if (isSkillChoiceLevel)
        {
            List<int> eligibleSkills = new();
            int ownedSkillCount = GetOwnedActiveSkillCount();

            for (int index = 0; index < skillPool.Count; index++)
            {
                SkillData skill = skillPool[index];
                int currentLevel = GetCurrentSkillLevel(index);

                if (skill != null &&
                    currentLevel < skill.MaxLevel &&
                    (currentLevel > 0 || ownedSkillCount < MaximumOwnedSkills))
                {
                    eligibleSkills.Add(index);
                }
            }

            Shuffle(eligibleSkills);
            eligibleSkills.Sort((left, right) =>
                GetCurrentSkillLevel(left).CompareTo(
                    GetCurrentSkillLevel(right)
                )
            );

            foreach (int skillIndex in eligibleSkills)
            {
                if (generated.Count >= MaximumCards)
                {
                    break;
                }

                bool isOwned = TryGetSkillState(
                    skillIndex,
                    out SkillProgressNetworkState ownedState
                );

                generated.Add(new LevelUpCandidateState
                {
                    Kind = LevelUpCandidateKind.Skill,
                    SkillPoolIndex = skillIndex,
                    TargetSkillLevel = isOwned ? ownedState.Level + 1 : 1,
                    Element = isOwned
                        ? ownedState.Element
                        : settings.GetRandomAllowedElement()
                });
            }
        }

        else
        {
            AddOwnedSkillCandidates(generated, 2);
        }

        FillWithGeneralCandidates(generated);
        return generated;
    }

    private void FillWithGeneralCandidates(
        List<LevelUpCandidateState> generated
    )
    {
        List<GeneralUpgradeType> pool = new();

        foreach (GeneralUpgradeDefinition definition in settings.GeneralUpgrades)
        {
            if (definition != null &&
                IsGeneralUpgradeApplicable(definition.Type) &&
                !pool.Contains(definition.Type))
            {
                pool.Add(definition.Type);
            }
        }

        Shuffle(pool);

        foreach (GeneralUpgradeType type in pool)
        {
            if (generated.Count >= MaximumCards)
            {
                break;
            }

            generated.Add(new LevelUpCandidateState
            {
                Kind = LevelUpCandidateKind.GeneralUpgrade,
                GeneralUpgrade = type,
                SkillPoolIndex = -1
            });
        }
    }

    private void ApplyCandidateServer(int index)
    {
        LevelUpCandidateState candidate = GetCandidate(index);
        bool applied = false;

        switch (candidate.Kind)
        {
            case LevelUpCandidateKind.GeneralUpgrade:
                applied = ApplyGeneralUpgradeServer(candidate.GeneralUpgrade);
                break;
            case LevelUpCandidateKind.Skill:
                applied = ApplySkillUpgradeServer(candidate);
                break;
        }

        if (!applied)
        {
            return;
        }

        hasSelected.Value = true;
        RefreshRuntimeState();
        TeamLevelUpCoordinator.Active?.NotifyChoiceAppliedServer(this);
    }

    private bool ApplyGeneralUpgradeServer(GeneralUpgradeType type)
    {
        if (!settings.TryGetGeneralUpgrade(
                type,
                out GeneralUpgradeDefinition definition
            ))
        {
            return false;
        }

        PlayerGrowthNetworkState state = growthState.Value;
        float amount = Mathf.Max(0f, definition.Amount);

        switch (type)
        {
            case GeneralUpgradeType.Damage:
                state.DamageMultiplier += amount;
                break;
            case GeneralUpgradeType.AttackInterval:
                state.AttackIntervalMultiplier *=
                    Mathf.Clamp(1f - amount, 0.1f, 1f);
                break;
            case GeneralUpgradeType.SkillCooldown:
                state.SkillCooldownMultiplier *=
                    Mathf.Clamp(1f - amount, 0.1f, 1f);
                break;
            case GeneralUpgradeType.MoveSpeed:
                state.MoveSpeedMultiplier += amount;
                break;
            case GeneralUpgradeType.ProjectileSpeed:
                state.ProjectileSpeedMultiplier += amount;
                break;
            case GeneralUpgradeType.ProjectileCount:
                state.ProjectileCountBonus +=
                    Mathf.Max(1, Mathf.RoundToInt(amount));
                break;
            case GeneralUpgradeType.BasicAttackRicochet:
                state.BasicAttackBounceCount +=
                    Mathf.Max(1, Mathf.RoundToInt(amount));
                break;
            default:
                return false;
        }

        growthState.Value = state;
        return true;
    }

    private bool ApplySkillUpgradeServer(LevelUpCandidateState candidate)
    {
        SkillData skill = GetSkillData(candidate.SkillPoolIndex);

        if (skill == null ||
            candidate.TargetSkillLevel < 1 ||
            candidate.TargetSkillLevel > skill.MaxLevel)
        {
            return false;
        }

        if (TryGetSkillState(
                candidate.SkillPoolIndex,
                out SkillProgressNetworkState current
            ))
        {
            if (candidate.TargetSkillLevel != current.Level + 1 ||
                candidate.Element != current.Element)
            {
                return false;
            }

            current.Level = candidate.TargetSkillLevel;
            int currentIndex = FindSkillStateIndex(candidate.SkillPoolIndex);
            skillStates[currentIndex] = current;
            return true;
        }

        if (GetOwnedActiveSkillCount() >= MaximumOwnedSkills ||
            candidate.TargetSkillLevel != 1 ||
            !settings.IsAllowedElement(candidate.Element))
        {
            return false;
        }

        skillStates.Add(new SkillProgressNetworkState
        {
            SkillPoolIndex = candidate.SkillPoolIndex,
            Level = 1,
            Element = candidate.Element
        });
        return true;
    }

    private void AddOwnedSkillCandidates(
        List<LevelUpCandidateState> generated,
        int maximumCount
    )
    {
        List<int> owned = new();

        for (int index = 0; index < skillPool.Count; index++)
        {
            int level = GetCurrentSkillLevel(index);
            SkillData skill = skillPool[index];

            if (skill != null && level > 0 && level < skill.MaxLevel)
            {
                owned.Add(index);
            }
        }

        Shuffle(owned);
        owned.Sort((left, right) =>
            GetCurrentSkillLevel(left).CompareTo(GetCurrentSkillLevel(right))
        );

        foreach (int skillIndex in owned)
        {
            if (generated.Count >= maximumCount ||
                generated.Count >= MaximumCards)
            {
                break;
            }

            SkillProgressNetworkState state = skillStates[
                FindSkillStateIndex(skillIndex)
            ];
            generated.Add(new LevelUpCandidateState
            {
                Kind = LevelUpCandidateKind.Skill,
                SkillPoolIndex = skillIndex,
                TargetSkillLevel = state.Level + 1,
                Element = state.Element
            });
        }
    }

    private int GetOwnedActiveSkillCount()
    {
        int count = 0;

        foreach (SkillProgressNetworkState state in skillStates)
        {
            SkillData skill = GetSkillData(state.SkillPoolIndex);

            if (skill != null && skill != configuredCharacter?.BasicSkill)
            {
                count++;
            }
        }

        return count;
    }

    private bool IsGeneralUpgradeApplicable(GeneralUpgradeType type)
    {
        if (type != GeneralUpgradeType.BasicAttackRicochet)
        {
            return true;
        }

        SkillData basicSkill = configuredCharacter != null
            ? configuredCharacter.BasicSkill
            : null;
        return basicSkill != null &&
            basicSkill.SkillPrefab != null &&
            basicSkill.SkillPrefab.GetComponent<StraightProjectile>() != null;
    }

    private bool TryGetSkillState(
        int skillPoolIndex,
        out SkillProgressNetworkState state
    )
    {
        int index = FindSkillStateIndex(skillPoolIndex);

        if (index >= 0)
        {
            state = skillStates[index];
            return true;
        }

        state = default;
        return false;
    }

    private int FindSkillStateIndex(int skillPoolIndex)
    {
        for (int index = 0; index < skillStates.Count; index++)
        {
            if (skillStates[index].SkillPoolIndex == skillPoolIndex)
            {
                return index;
            }
        }

        return -1;
    }

    private void RefreshRuntimeState()
    {
        if (runtimeGrowth == null)
        {
            return;
        }

        runtimeGrowth.Configure(settings);
        runtimeGrowth.ApplyCommonGrowth(growthState.Value);

        List<SkillProgressNetworkState> states = new(skillStates.Count);
        List<RuntimeSkillLoadout> loadout = new(skillStates.Count);

        for (int index = 0; index < skillStates.Count; index++)
        {
            SkillProgressNetworkState state = skillStates[index];
            states.Add(state);

            SkillData skill = GetSkillData(state.SkillPoolIndex);

            if (skill != null)
            {
                if (skill == configuredCharacter?.BasicSkill)
                {
                    continue;
                }

                loadout.Add(new RuntimeSkillLoadout(
                    skill,
                    state.Element,
                    state.Level
                ));
            }
        }

        runtimeGrowth.ApplySkillStates(states, skillPool);
        autoSkillCaster?.ConfigureProgressionSkills(loadout);
    }

    private void HandleChoiceActiveChanged(bool previous, bool current)
    {
        GameplayPauseState.SetLevelUpActive(this, current);
        RefreshOwnerPanel();
    }

    private void HandleChoiceStateChanged(int previous, int current)
    {
        RefreshOwnerPanel();
    }

    private void HandleChoiceStateChanged(bool previous, bool current)
    {
        RefreshOwnerPanel();
    }

    private void HandleChoiceDeadlineChanged(double previous, double current)
    {
        RefreshOwnerPanel();
    }

    private void HandleCandidateChanged(
        LevelUpCandidateState previous,
        LevelUpCandidateState current
    )
    {
        RefreshOwnerPanel();
    }

    private void HandleGrowthChanged(
        PlayerGrowthNetworkState previous,
        PlayerGrowthNetworkState current
    )
    {
        RefreshRuntimeState();
    }

    private void HandleSkillListChanged(
        NetworkListEvent<SkillProgressNetworkState> changeEvent
    )
    {
        RefreshRuntimeState();
    }

    private void RefreshOwnerPanel()
    {
        if (!IsOwner)
        {
            return;
        }

        if (panelPresenter == null && choiceActive.Value)
        {
            panelPresenter = gameObject.AddComponent<LevelUpPanelPresenter>();
            panelPresenter.Bind(this);
        }

        panelPresenter?.Refresh();
    }

    private static LevelUpCandidateState GetOrEmpty(
        IReadOnlyList<LevelUpCandidateState> candidates,
        int index
    )
    {
        return index >= 0 && index < candidates.Count
            ? candidates[index]
            : default;
    }

    private static void Shuffle<T>(IList<T> values)
    {
        for (int index = values.Count - 1; index > 0; index--)
        {
            int other = Random.Range(0, index + 1);
            (values[index], values[other]) = (values[other], values[index]);
        }
    }

    private static double GetServerTime()
    {
        NetworkManager manager = NetworkManager.Singleton;
        return manager != null && manager.IsListening
            ? manager.ServerTime.Time
            : Time.unscaledTimeAsDouble;
    }
}
