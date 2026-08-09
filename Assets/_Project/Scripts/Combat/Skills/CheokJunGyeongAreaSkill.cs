using System.Collections;
using UnityEngine;

public sealed class CheokJunGyeongAreaSkill : MonoBehaviour, ISkillBehaviour
{
    [SerializeField]
    private bool centerOnOwner = true;

    [SerializeField, Min(0f)]
    private float forwardOffset;

    [SerializeField, Min(0.1f)]
    private float baseRadius = 2.2f;

    [SerializeField, Min(0f)]
    private float secondWaveDelay = 0.25f;

    [SerializeField, Min(0f)]
    private float secondWaveDamageMultiplier = 0.7f;

    [SerializeField]
    private bool finalWaveAtLevelFive;

    [SerializeField, Min(1f)]
    private float levelFiveSecondRadiusMultiplier = 1f;

    [SerializeField, Min(1f)]
    private float finalRadiusMultiplier = 1.45f;

    [SerializeField, Min(0f)]
    private float finalDamageMultiplier = 0.8f;

    private SpriteRenderer visual;

    public void Cast(SkillCastContext context)
    {
        Vector2 direction = context.Direction.sqrMagnitude > 0.001f
            ? context.Direction.normalized
            : Vector2.right;
        Vector2 center = centerOnOwner
            ? context.Origin
            : context.Origin + direction * forwardOffset;
        float radiusMultiplier = context.ZoneRadiusMultiplier > 0f
            ? context.ZoneRadiusMultiplier
            : 1f;
        float radius = baseRadius * radiusMultiplier;

        transform.position = center;
        visual = GetComponent<SpriteRenderer>();
        ElementVisuals.ApplyColor(gameObject, context.Element);
        StartCoroutine(RunWaves(context, center, radius));
    }

    private IEnumerator RunWaves(
        SkillCastContext context,
        Vector2 center,
        float radius
    )
    {
        Strike(context, center, radius, context.Damage);

        if (context.SkillLevel >= 4)
        {
            yield return WaitForGameplaySeconds(secondWaveDelay);
            float secondRadius = context.SkillLevel >= 5
                ? radius * levelFiveSecondRadiusMultiplier
                : radius;
            Strike(
                context,
                center,
                secondRadius,
                context.Damage * secondWaveDamageMultiplier
            );
        }

        if (context.SkillLevel >= 5 && finalWaveAtLevelFive)
        {
            yield return WaitForGameplaySeconds(secondWaveDelay);
            Strike(
                context,
                center,
                radius * finalRadiusMultiplier,
                context.Damage * finalDamageMultiplier
            );
        }

        Destroy(gameObject, 0.12f);
    }

    private void Strike(
        SkillCastContext context,
        Vector2 center,
        float radius,
        float damage
    )
    {
        transform.position = center;
        transform.localScale = Vector3.one * (radius * 2f);

        if (visual != null)
        {
            visual.enabled = true;
        }

        if (!context.VisualOnly)
        {
            CheokJunGyeongDamage.Circle(
                center,
                radius,
                damage,
                context.Element,
                context.SourcePlayerId,
                context.EnemyLayer
            );
        }
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
