using UnityEngine;

public sealed class IlgidangcheonBuff : MonoBehaviour, ISkillBehaviour
{
    [SerializeField, Min(0.1f)]
    private float duration = 6f;

    [SerializeField, Min(1f)]
    private float speedMultiplier = 1.3f;

    [SerializeField, Range(0.1f, 1f)]
    private float basicAttackIntervalMultiplier = 0.45f;

    [SerializeField, Min(1f)]
    private float basicAttackRangeMultiplier = 1.35f;

    [SerializeField, Min(0.1f)]
    private float finalSlashRadius = 3.5f;

    private SkillCastContext castContext;
    private CheokJunGyeongCombatState combatState;
    private float remainingDuration;
    private int buffKey;
    private bool active;

    public void Cast(SkillCastContext context)
    {
        castContext = context;
        buffKey = GetInstanceID();
        remainingDuration = duration;

        if (context.Owner != null)
        {
            combatState = CheokJunGyeongCombatState.GetOrAdd(context.Owner);
            combatState.SetBuff(
                buffKey,
                speedMultiplier,
                basicAttackIntervalMultiplier,
                basicAttackRangeMultiplier,
                true
            );
            transform.SetParent(context.Owner.transform);
            transform.localPosition = Vector3.zero;
        }
        else
        {
            transform.position = context.Origin;
        }

        transform.localScale = Vector3.one * 2.5f;
        ElementVisuals.ApplyColor(gameObject, context.Element);
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

        bool showFinalSlash = completed && castContext.Owner != null;

        if (showFinalSlash)
        {
            transform.localScale = Vector3.one * (finalSlashRadius * 2f);

            if (!castContext.VisualOnly)
            {
                CheokJunGyeongDamage.Circle(
                    castContext.Owner.transform.position,
                    finalSlashRadius,
                    castContext.Damage,
                    castContext.Element,
                    castContext.SourcePlayerId,
                    castContext.EnemyLayer
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
