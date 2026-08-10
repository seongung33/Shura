using UnityEngine;

public sealed class HyeolloBuff : MonoBehaviour, ISkillBehaviour
{
    [SerializeField, Min(0.1f)]
    private float baseDuration = 4f;

    [SerializeField, Min(1f)]
    private float baseSpeedMultiplier = 1.25f;

    [SerializeField, Min(1f)]
    private float upgradedSpeedMultiplier = 1.35f;

    [SerializeField, Min(0.1f)]
    private float slashRadius = 1.4f;

    [SerializeField, Min(0.05f)]
    private float slashInterval = 0.6f;

    [SerializeField, Min(0.05f)]
    private float upgradedSlashInterval = 0.4f;

    [SerializeField, Min(0.1f)]
    private float finalSlashRadius = 2.6f;

    [SerializeField, Min(0f)]
    private float finalSlashDamageMultiplier = 1.6f;

    private SkillCastContext castContext;
    private CheokJunGyeongCombatState combatState;
    private float remainingDuration;
    private float nextSlashTime;
    private float activeRadius;
    private float activeInterval;
    private int buffKey;
    private bool active;

    public void Cast(SkillCastContext context)
    {
        castContext = context;
        buffKey = GetInstanceID();
        remainingDuration = baseDuration +
            (context.SkillLevel >= 2 ? 1f : 0f);
        activeRadius = slashRadius *
            (context.SkillLevel >= 3 ? 1.15f : 1f);
        activeInterval = context.SkillLevel >= 4
            ? upgradedSlashInterval
            : slashInterval;
        float speedMultiplier = context.SkillLevel >= 3
            ? upgradedSpeedMultiplier
            : baseSpeedMultiplier;

        if (context.Owner != null)
        {
            combatState = CheokJunGyeongCombatState.GetOrAdd(context.Owner);
            combatState.SetBuff(buffKey, speedMultiplier);
            transform.SetParent(context.Owner.transform);
            transform.localPosition = Vector3.zero;
        }
        else
        {
            transform.position = context.Origin;
        }

        transform.localScale = Vector3.one * (activeRadius * 2f);
        ElementVisuals.ApplyColor(gameObject, context.Element);
        nextSlashTime = Time.time;
        active = true;
    }

    private void Update()
    {
        if (!active || GameplayPauseState.IsLevelUpActive)
        {
            return;
        }

        remainingDuration -= Time.deltaTime;

        if (remainingDuration <= 0f || castContext.Owner == null)
        {
            EndBuff(remainingDuration <= 0f);
            return;
        }

        if (Time.time >= nextSlashTime)
        {
            nextSlashTime = Time.time + activeInterval;

            if (!castContext.VisualOnly)
            {
                CheokJunGyeongDamage.Circle(
                    castContext.Owner.transform.position,
                    activeRadius,
                    castContext.Damage,
                    castContext.Element,
                    castContext.SourcePlayerId,
                    castContext.EnemyLayer,
                    RelicAttackType.DamageOverTime
                );
            }
        }
    }

    private void EndBuff(bool completed)
    {
        if (!active)
        {
            return;
        }

        active = false;
        combatState?.RemoveBuff(buffKey);

        bool showFinalSlash = completed &&
            castContext.SkillLevel >= 5 &&
            castContext.Owner != null;

        if (showFinalSlash)
        {
            transform.localScale = Vector3.one * (finalSlashRadius * 2f);

            if (!castContext.VisualOnly)
            {
                CheokJunGyeongDamage.Circle(
                    castContext.Owner.transform.position,
                    finalSlashRadius,
                    castContext.Damage * finalSlashDamageMultiplier,
                    castContext.Element,
                    castContext.SourcePlayerId,
                    castContext.EnemyLayer,
                    RelicAttackType.Area
                );
            }
        }

        Destroy(gameObject, showFinalSlash ? 0.12f : 0f);
    }

    private void OnDestroy()
    {
        if (active)
        {
            active = false;
            combatState?.RemoveBuff(buffKey);
        }
    }
}
