using System.Collections;
using UnityEngine;

public sealed class CheolhyeolSlash : MonoBehaviour, ISkillBehaviour
{
    [SerializeField, Range(1f, 360f)]
    private float arcAngle = 120f;

    [SerializeField, Min(0f)]
    private float afterimageDamageMultiplier = 0.55f;

    [SerializeField, Min(0f)]
    private float afterimageDelay = 0.08f;

    public void Cast(SkillCastContext context)
    {
        Vector2 direction = context.Direction.sqrMagnitude > 0.001f
            ? context.Direction.normalized
            : Vector2.right;
        float range = Mathf.Max(0.1f, context.Range);

        transform.SetPositionAndRotation(
            context.Origin,
            Quaternion.FromToRotation(Vector3.right, direction)
        );
        transform.localScale = new Vector3(range, range * 0.65f, 1f);
        ElementVisuals.ApplyColor(gameObject, ElementType.None);

        if (!context.VisualOnly)
        {
            CheokJunGyeongDamage.Arc(
                context.Origin,
                direction,
                range,
                arcAngle,
                context.Damage,
                ElementType.None,
                context.SourcePlayerId,
                context.EnemyLayer
            );
        }

        CheokJunGyeongCombatState state = context.Owner != null
            ? context.Owner.GetComponent<CheokJunGyeongCombatState>()
            : null;

        if (state != null && state.HasAfterimage)
        {
            StartCoroutine(StrikeAfterimage(context, direction, range));
        }
        else
        {
            Destroy(gameObject, 0.14f);
        }
    }

    private IEnumerator StrikeAfterimage(
        SkillCastContext context,
        Vector2 direction,
        float range
    )
    {
        yield return WaitForGameplaySeconds(afterimageDelay);

        transform.localScale = new Vector3(
            range * 1.15f,
            range * 0.75f,
            1f
        );

        if (!context.VisualOnly)
        {
            CheokJunGyeongDamage.Arc(
                context.Origin,
                direction,
                range * 1.15f,
                arcAngle,
                context.Damage * afterimageDamageMultiplier,
                ElementType.None,
                context.SourcePlayerId,
                context.EnemyLayer
            );
        }

        Destroy(gameObject, 0.12f);
    }

    private static IEnumerator WaitForGameplaySeconds(float duration)
    {
        float remaining = duration;

        while (remaining > 0f)
        {
            if (!GameplayPauseState.IsLevelUpActive)
            {
                remaining -= Time.deltaTime;
            }

            yield return null;
        }
    }
}
