using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerRuntimeGrowth))]
public sealed class LocalPlayerProgression : MonoBehaviour,
    ILevelUpChoiceSource
{
    private const int MaximumCards = 3;
    private const int MaximumOwnedSkills = 3;

    private readonly List<SkillData> skillPool = new();
    private readonly List<SkillProgressNetworkState> skillStates = new();
    private readonly List<LevelUpCandidateState> candidates = new();

    private PlayerGrowthNetworkState growthState =
        PlayerGrowthNetworkState.Default;
    private LevelUpSettings settings = new();
    private CharacterData character;
    private PlayerRuntimeGrowth runtimeGrowth;
    private AutoSkillCaster autoSkillCaster;
    private LevelUpPanelPresenter panelPresenter;
    private TeamExperience subscribedExperience;
    private bool choiceActive;
    private bool hasSelected;
    private int choiceTeamLevel;
    private int choiceSessionId;
    private double choiceDeadline;

    public bool ChoiceActive => choiceActive;
    public bool HasSelected => hasSelected;
    public int ChoiceTeamLevel => choiceTeamLevel;
    public int ChoiceSessionId => choiceSessionId;
    public double ChoiceDeadline => choiceDeadline;
    public int CandidateCount => candidates.Count;
    public LevelUpSettings Settings => settings;

    private void Awake()
    {
        runtimeGrowth = GetComponent<PlayerRuntimeGrowth>();
        autoSkillCaster = GetComponent<AutoSkillCaster>();
    }

    private void Start()
    {
        SubscribeToExperience();
    }

    private void Update()
    {
        if (subscribedExperience == null)
        {
            SubscribeToExperience();
        }

        if (choiceActive && !hasSelected &&
            Time.unscaledTimeAsDouble >= choiceDeadline)
        {
            RequestChoice(Random.Range(0, candidates.Count));
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromExperience();
        GameplayPauseState.SetLevelUpActive(this, false);

        if (panelPresenter != null)
        {
            Destroy(panelPresenter);
        }
    }

    public void Configure(
        CharacterData configuredCharacter,
        LevelUpSettings configuredSettings
    )
    {
        character = configuredCharacter;
        settings = configuredSettings ?? new LevelUpSettings();
        skillPool.Clear();
        skillStates.Clear();
        growthState = PlayerGrowthNetworkState.Default;

        if (character != null)
        {
            foreach (SkillData skill in character.LevelUpSkills)
            {
                if (skill != null && !skillPool.Contains(skill))
                {
                    skillPool.Add(skill);
                }
            }

            if (!character.UsesLevelUpSkillPool)
            {
                foreach (SkillData skill in character.StartingSkills)
                {
                    int index = skillPool.IndexOf(skill);

                    if (index >= 0 && FindSkillStateIndex(index) < 0)
                    {
                        skillStates.Add(new SkillProgressNetworkState
                        {
                            SkillPoolIndex = index,
                            Level = 1,
                            Element = settings.GetRandomAllowedElement()
                        });
                    }
                }
            }
        }

        RefreshRuntimeState();
        SubscribeToExperience();
    }

    public LevelUpCandidateState GetCandidate(int index)
    {
        return index >= 0 && index < candidates.Count
            ? candidates[index]
            : default;
    }

    public SkillData GetSkillData(int poolIndex)
    {
        return poolIndex >= 0 && poolIndex < skillPool.Count
            ? skillPool[poolIndex]
            : null;
    }

    public int GetCurrentSkillLevel(int poolIndex)
    {
        int index = FindSkillStateIndex(poolIndex);
        return index >= 0 ? skillStates[index].Level : 0;
    }

    public string GetSelectionStatusText()
    {
        return hasSelected ? "선택 완료" : "선택 중";
    }

    public void RequestChoice(int index)
    {
        if (!choiceActive || hasSelected ||
            index < 0 || index >= candidates.Count)
        {
            return;
        }

        LevelUpCandidateState candidate = candidates[index];
        bool applied = candidate.Kind switch
        {
            LevelUpCandidateKind.GeneralUpgrade =>
                ApplyGeneralUpgrade(candidate.GeneralUpgrade),
            LevelUpCandidateKind.Skill => ApplySkillUpgrade(candidate),
            _ => false
        };

        if (!applied)
        {
            return;
        }

        hasSelected = true;
        RefreshRuntimeState();
        panelPresenter?.Refresh();
        EndChoice();
        subscribedExperience?.CompleteLevelUpChoice(choiceTeamLevel);
    }

    private bool HandleLevelUpRequested(int teamLevel)
    {
        if (character == null || choiceActive)
        {
            return false;
        }

        GenerateCandidates(settings.IsSkillChoiceLevel(teamLevel));

        if (candidates.Count == 0)
        {
            return false;
        }

        choiceTeamLevel = teamLevel;
        choiceSessionId++;
        choiceDeadline = Time.unscaledTimeAsDouble + settings.ChoiceDuration;
        hasSelected = false;
        choiceActive = true;
        GameplayPauseState.SetLevelUpActive(this, true);

        if (panelPresenter == null)
        {
            panelPresenter = gameObject.AddComponent<LevelUpPanelPresenter>();
            panelPresenter.Bind(this);
        }

        panelPresenter.Refresh();
        return true;
    }

    private void EndChoice()
    {
        choiceActive = false;
        GameplayPauseState.SetLevelUpActive(this, false);
        panelPresenter?.Refresh();
    }

    private void GenerateCandidates(bool skillChoiceLevel)
    {
        candidates.Clear();

        if (skillChoiceLevel)
        {
            List<int> eligible = new();

            for (int index = 0; index < skillPool.Count; index++)
            {
                SkillData skill = skillPool[index];
                int currentLevel = GetCurrentSkillLevel(index);

                if (skill != null && currentLevel < skill.MaxLevel &&
                    (currentLevel > 0 || skillStates.Count < MaximumOwnedSkills))
                {
                    eligible.Add(index);
                }
            }

            Shuffle(eligible);

            foreach (int skillIndex in eligible)
            {
                if (candidates.Count >= MaximumCards)
                {
                    break;
                }

                int stateIndex = FindSkillStateIndex(skillIndex);
                bool owned = stateIndex >= 0;
                SkillProgressNetworkState state = owned
                    ? skillStates[stateIndex]
                    : default;
                candidates.Add(new LevelUpCandidateState
                {
                    Kind = LevelUpCandidateKind.Skill,
                    SkillPoolIndex = skillIndex,
                    TargetSkillLevel = owned ? state.Level + 1 : 1,
                    Element = owned
                        ? state.Element
                        : settings.GetRandomAllowedElement()
                });
            }
        }

        List<GeneralUpgradeType> generalPool = new();

        foreach (GeneralUpgradeDefinition definition in settings.GeneralUpgrades)
        {
            if (definition != null &&
                IsGeneralUpgradeApplicable(definition.Type) &&
                !generalPool.Contains(definition.Type))
            {
                generalPool.Add(definition.Type);
            }
        }

        Shuffle(generalPool);

        foreach (GeneralUpgradeType type in generalPool)
        {
            if (candidates.Count >= MaximumCards)
            {
                break;
            }

            candidates.Add(new LevelUpCandidateState
            {
                Kind = LevelUpCandidateKind.GeneralUpgrade,
                GeneralUpgrade = type,
                SkillPoolIndex = -1
            });
        }
    }

    private bool ApplyGeneralUpgrade(GeneralUpgradeType type)
    {
        if (!settings.TryGetGeneralUpgrade(
                type,
                out GeneralUpgradeDefinition definition
            ))
        {
            return false;
        }

        float amount = Mathf.Max(0f, definition.Amount);

        switch (type)
        {
            case GeneralUpgradeType.Damage:
                growthState.DamageMultiplier += amount;
                break;
            case GeneralUpgradeType.AttackInterval:
                growthState.AttackIntervalMultiplier *=
                    Mathf.Clamp(1f - amount, 0.1f, 1f);
                break;
            case GeneralUpgradeType.SkillCooldown:
                growthState.SkillCooldownMultiplier *=
                    Mathf.Clamp(1f - amount, 0.1f, 1f);
                break;
            case GeneralUpgradeType.MoveSpeed:
                growthState.MoveSpeedMultiplier += amount;
                break;
            case GeneralUpgradeType.ProjectileSpeed:
                growthState.ProjectileSpeedMultiplier += amount;
                break;
            case GeneralUpgradeType.ProjectileCount:
                growthState.ProjectileCountBonus +=
                    Mathf.Max(1, Mathf.RoundToInt(amount));
                break;
            case GeneralUpgradeType.BasicAttackRicochet:
                growthState.BasicAttackBounceCount +=
                    Mathf.Max(1, Mathf.RoundToInt(amount));
                break;
            default:
                return false;
        }

        return true;
    }

    private bool ApplySkillUpgrade(LevelUpCandidateState candidate)
    {
        SkillData skill = GetSkillData(candidate.SkillPoolIndex);

        if (skill == null || candidate.TargetSkillLevel < 1 ||
            candidate.TargetSkillLevel > skill.MaxLevel)
        {
            return false;
        }

        int stateIndex = FindSkillStateIndex(candidate.SkillPoolIndex);

        if (stateIndex >= 0)
        {
            SkillProgressNetworkState state = skillStates[stateIndex];

            if (candidate.TargetSkillLevel != state.Level + 1 ||
                candidate.Element != state.Element)
            {
                return false;
            }

            state.Level = candidate.TargetSkillLevel;
            skillStates[stateIndex] = state;
            return true;
        }

        if (skillStates.Count >= MaximumOwnedSkills ||
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

    private bool IsGeneralUpgradeApplicable(GeneralUpgradeType type)
    {
        if (type != GeneralUpgradeType.BasicAttackRicochet)
        {
            return true;
        }

        SkillData basicSkill = character != null ? character.BasicSkill : null;
        return basicSkill != null && basicSkill.SkillPrefab != null &&
            basicSkill.SkillPrefab.GetComponent<StraightProjectile>() != null;
    }

    private int FindSkillStateIndex(int poolIndex)
    {
        for (int index = 0; index < skillStates.Count; index++)
        {
            if (skillStates[index].SkillPoolIndex == poolIndex)
            {
                return index;
            }
        }

        return -1;
    }

    private void RefreshRuntimeState()
    {
        runtimeGrowth.Configure(settings);
        runtimeGrowth.ApplyCommonGrowth(growthState);
        runtimeGrowth.ApplySkillStates(skillStates, skillPool);

        List<RuntimeSkillLoadout> loadout = new(skillStates.Count);

        foreach (SkillProgressNetworkState state in skillStates)
        {
            SkillData skill = GetSkillData(state.SkillPoolIndex);

            if (skill != null)
            {
                loadout.Add(new RuntimeSkillLoadout(
                    skill,
                    state.Element,
                    state.Level
                ));
            }
        }

        autoSkillCaster?.ConfigureProgressionSkills(loadout);
    }

    private void SubscribeToExperience()
    {
        TeamExperience experience = TeamExperience.Active;

        if (experience == null || experience == subscribedExperience)
        {
            return;
        }

        UnsubscribeFromExperience();
        subscribedExperience = experience;
        subscribedExperience.LevelUpChoiceRequested += HandleLevelUpRequested;
    }

    private void UnsubscribeFromExperience()
    {
        if (subscribedExperience == null)
        {
            return;
        }

        subscribedExperience.LevelUpChoiceRequested -= HandleLevelUpRequested;
        subscribedExperience = null;
    }

    private static void Shuffle<T>(IList<T> values)
    {
        for (int index = values.Count - 1; index > 0; index--)
        {
            int other = Random.Range(0, index + 1);
            (values[index], values[other]) = (values[other], values[index]);
        }
    }
}
