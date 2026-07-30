using UnityEngine;

/// <summary>
/// 7속성 시스템 (D-010). None은 무속성(기본공격 등).
/// </summary>
public enum ElementType
{
    None,
    Poison,     // 독
    Water,      // 물
    Lightning,  // 번개
    Ice,        // 얼음
    Earth,      // 흙
    Wind,       // 바람
    Fire        // 불
}

public static class ElementUtil
{
    /// <summary>
    /// 속성별 이펙트 색상 (2026-07-30 확정: 독=초록, 바람=흰색).
    /// </summary>
    public static Color GetColor(ElementType element)
    {
        switch (element)
        {
            case ElementType.Poison:
                return new Color(0.25f, 0.85f, 0.25f); // 초록
            case ElementType.Water:
                return new Color(0.2f, 0.5f, 1f);      // 파랑
            case ElementType.Lightning:
                return new Color(1f, 0.92f, 0.2f);     // 노랑
            case ElementType.Ice:
                return new Color(0.6f, 0.9f, 1f);      // 하늘색
            case ElementType.Earth:
                return new Color(0.6f, 0.4f, 0.2f);    // 갈색
            case ElementType.Wind:
                return new Color(0.95f, 0.95f, 0.95f); // 흰색
            case ElementType.Fire:
                return new Color(1f, 0.45f, 0.1f);     // 주황
            default:
                return new Color(0.75f, 0.75f, 0.75f); // 무속성 회색
        }
    }

    public static string GetKoreanName(ElementType element)
    {
        switch (element)
        {
            case ElementType.Poison: return "독";
            case ElementType.Water: return "물";
            case ElementType.Lightning: return "번개";
            case ElementType.Ice: return "얼음";
            case ElementType.Earth: return "흙";
            case ElementType.Wind: return "바람";
            case ElementType.Fire: return "불";
            default: return "무속성";
        }
    }

    /// <summary>
    /// None을 제외한 7속성 중 하나를 랜덤으로 반환한다.
    /// 스킬 습득 시 속성 랜덤 부여(D-010)에 사용.
    /// </summary>
    public static ElementType GetRandomElement()
    {
        // enum에서 None(0)을 제외한 1~7 범위
        int value = Random.Range(1, 8);
        return (ElementType)value;
    }
}
