using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterData", menuName = "Game/Character Data")]
public class CharacterData : ScriptableObject
{
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

    public GameObject GameplayVisualPrefab => gameplayVisualPrefab;
    public float MaxHealth => maxHealth;
    public float MoveSpeed => moveSpeed;
    public SkillData BasicSkill => basicSkill;
    public IReadOnlyList<SkillData> StartingSkills => startingSkills;
}
