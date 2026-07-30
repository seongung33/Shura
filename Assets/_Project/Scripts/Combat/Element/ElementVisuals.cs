using UnityEngine;

/// <summary>
/// 오브젝트(투사체, 장판, 이펙트)의 모든 시각 요소를 속성 색으로 물들인다.
/// SpriteRenderer, TrailRenderer, ParticleSystem을 한 번에 처리한다.
/// </summary>
public static class ElementVisuals
{
    public static void ApplyColor(GameObject root, ElementType element)
    {
        Color color = ElementUtil.GetColor(element);
        ApplyColor(root, color);
    }

    public static void ApplyColor(GameObject root, Color color)
    {
        if (root == null)
        {
            return;
        }

        SpriteRenderer[] sprites =
            root.GetComponentsInChildren<SpriteRenderer>(true);

        foreach (SpriteRenderer sprite in sprites)
        {
            // 기존 알파(투명도)는 유지한다. 장판처럼 반투명한 스프라이트 대응.
            float alpha = sprite.color.a;
            sprite.color = new Color(color.r, color.g, color.b, alpha);
        }

        TrailRenderer[] trails =
            root.GetComponentsInChildren<TrailRenderer>(true);

        foreach (TrailRenderer trail in trails)
        {
            trail.startColor = color;

            Color endColor = color;
            endColor.a = 0f;
            trail.endColor = endColor;
        }

        ParticleSystem[] particles =
            root.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem particle in particles)
        {
            ParticleSystem.MainModule main = particle.main;
            main.startColor = color;
        }
    }
}
