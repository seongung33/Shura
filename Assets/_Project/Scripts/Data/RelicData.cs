using UnityEngine;

public enum RelicId
{
    None,
    ThunderFragment,
    WindTalisman,
    BrokenCannon,
    DivineArrowhead,
    GoblinFire,
    GeneralJade
}

[CreateAssetMenu(fileName = "RelicData", menuName = "Shura/Relic Data")]
public sealed class RelicData : ScriptableObject
{
    private static readonly System.Collections.Generic.Dictionary<RelicId, Sprite>
        FallbackIcons = new();
    [SerializeField]
    private RelicId id;

    [SerializeField]
    private string displayName;

    [SerializeField]
    private Sprite icon;

    [SerializeField, TextArea(2, 4)]
    private string description;

    [SerializeField, Range(0f, 1f)]
    private float triggerChance;

    [SerializeField, Min(0f)]
    private float damageMultiplier;

    [SerializeField, Min(0f)]
    private float radius;

    [SerializeField, Min(1)]
    private int maxTargets = 1;

    [SerializeField, Min(0f)]
    private float duration;

    [SerializeField, Min(0.05f)]
    private float tickInterval = 0.5f;

    public RelicId Id => id;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon != null ? icon : ResolveFallbackIcon();
    public float TriggerChance => Mathf.Clamp01(triggerChance);
    public float DamageMultiplier => Mathf.Max(0f, damageMultiplier);
    public float Radius => Mathf.Max(0f, radius);
    public int MaxTargets => Mathf.Max(1, maxTargets);
    public float Duration => Mathf.Max(0f, duration);
    public float TickInterval => Mathf.Max(0.05f, tickInterval);

    private Sprite ResolveFallbackIcon()
    {
        if (FallbackIcons.TryGetValue(id, out Sprite cached))
        {
            return cached;
        }

        string resourcePath = id switch
        {
            RelicId.ThunderFragment => "UI/HudIcons/relic_experience_bead",
            RelicId.WindTalisman => "UI/HudIcons/relic_ice_talisman",
            RelicId.BrokenCannon => "UI/HudIcons/relic_cooldown_hourglass",
            RelicId.DivineArrowhead => "UI/HudIcons/relic_magnetic_compass",
            RelicId.GoblinFire => "UI/HudIcons/relic_ultimate_lotus",
            RelicId.GeneralJade => "UI/HudIcons/relic_healing_gourd",
            _ => null
        };
        Sprite loaded = string.IsNullOrEmpty(resourcePath)
            ? null
            : Resources.Load<Sprite>(resourcePath);
        FallbackIcons[id] = loaded;
        return loaded;
    }
}
