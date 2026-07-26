using UnityEngine;

[CreateAssetMenu(
    fileName = "NewSkillData",
    menuName = "Shura/Skill Data"
)]
public class SkillData : ScriptableObject
{
    [Header("Information")]

    [SerializeField]
    private string skillId;

    [SerializeField]
    private string displayName;

    [TextArea]
    [SerializeField]
    private string description;

    [Header("Combat")]

    [Min(0.01f)]
    [SerializeField]
    private float cooldown = 1f;

    [Min(0f)]
    [SerializeField]
    private float damage = 10f;

    [Min(0f)]
    [SerializeField]
    private float range = 5f;

    [Min(0f)]
    [SerializeField]
    private float projectileSpeed = 8f;

    [Header("Prefab")]

    [SerializeField]
    private GameObject skillPrefab;

    public string SkillId => skillId;
    public string DisplayName => displayName;
    public string Description => description;

    public float Cooldown => cooldown;
    public float Damage => damage;
    public float Range => range;
    public float ProjectileSpeed => projectileSpeed;

    public GameObject SkillPrefab => skillPrefab;
}