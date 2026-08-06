using UnityEngine;

/// <summary>
/// 스킬 시전에 필요한 정보 묶음.
/// 시전자(캐스터)가 만들어서 스킬 프리팹에 전달한다.
/// </summary>
public struct SkillCastContext
{
    public GameObject Owner;        // 시전한 플레이어
    public Vector2 Origin;          // 발사 위치
    public Vector2 Direction;       // 조준 방향 (정규화됨)
    public float Damage;
    public float ProjectileSpeed;
    public ElementType Element;     // 습득 시 랜덤 부여된 속성
    public LayerMask EnemyLayer;
    public bool VisualOnly;         // 네트워크 표시용 복제본은 피해를 주지 않음
    public ulong SourcePlayerId;    // 네트워크 소유자 ID. 로컬/미지정은 ulong.MaxValue
}

/// <summary>
/// 스킬 프리팹의 루트에 붙는 공통 규약.
/// AutoSkillCaster / DirectionalAutoAttack이 프리팹을 생성한 뒤 Cast를 호출한다.
/// 기존 유도형 Projectile은 이 구조를 사용하지 않고 그대로 보존한다 (D-013).
/// </summary>
public interface ISkillBehaviour
{
    void Cast(SkillCastContext context);
}
