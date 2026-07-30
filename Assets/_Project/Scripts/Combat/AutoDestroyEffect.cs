using UnityEngine;

/// <summary>
/// 명중·폭발 이펙트 프리팹에 붙여서 일정 시간 뒤 자동 제거한다.
/// 속성 색을 받아 파티클을 물들일 수도 있다.
/// </summary>
public class AutoDestroyEffect : MonoBehaviour
{
    [Min(0.05f)]
    [SerializeField]
    private float lifeTime = 1f;

    private void Awake()
    {
        Destroy(gameObject, lifeTime);
    }

    /// <summary>
    /// 생성 직후 속성 색을 적용하고 싶을 때 호출한다.
    /// </summary>
    public void SetElement(ElementType element)
    {
        ElementVisuals.ApplyColor(gameObject, element);
    }
}
