using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterData", menuName = "Game/Character Data")]
public class CharacterData : ScriptableObject
{
    [SerializeField, Min(0)]
    private int characterId;

    [Header("기본 정보")]
    public string characterName;
    public string role;

    [TextArea(2, 4)]
    public string description;

    public Sprite portrait;

    [Header("전투 구성")]
    [SerializeField]
    private GameObject gameplayVisualPrefab;

    [SerializeField, Min(1f)]
    private float maxHealth = 100f;

    [SerializeField, Min(0.1f)]
    private float moveSpeed = 5f;

    [SerializeField]
    private SkillData basicSkill;

    [SerializeField]
    private List<SkillData> startingSkills = new List<SkillData>();

    [Tooltip("네트워크 레벨업에서 획득/강화 후보로 사용할 스킬. 비어 있으면 시작 스킬을 재사용합니다.")]
    [SerializeField]
    private List<SkillData> levelUpSkills = new List<SkillData>();

    public int CharacterId => characterId;
    public GameObject GameplayVisualPrefab => gameplayVisualPrefab;
    public float MaxHealth => maxHealth;
    public float MoveSpeed => moveSpeed;
    public SkillData BasicSkill => basicSkill;
    public IReadOnlyList<SkillData> StartingSkills => startingSkills;
    public IReadOnlyList<SkillData> LevelUpSkills =>
        levelUpSkills != null && levelUpSkills.Count > 0
            ? levelUpSkills
            : startingSkills;
    public bool UsesLevelUpSkillPool =>
        levelUpSkills != null && levelUpSkills.Count > 0;
}
