# 주몽 기본공격·P0 스킬 Unity 세팅 가이드

작성: 2026-07-30. 신규 스크립트는 코드만으로는 동작하지 않고 아래 에디터 세팅이 필요하다.
테스트는 `Tests/CombatTest.unity`에서 진행한다.

## 1. 추가된 파일

| 파일 | 역할 |
|---|---|
| `Scripts/Combat/Element/ElementType.cs` | 7속성 enum + 색상·이름·랜덤 유틸 |
| `Scripts/Combat/Element/ElementVisuals.cs` | 스프라이트·트레일·파티클을 속성 색으로 물들임 |
| `Scripts/Combat/ISkillBehaviour.cs` | 스킬 프리팹 공통 규약 (Cast) |
| `Scripts/Combat/AutoDestroyEffect.cs` | 명중·폭발 이펙트 자동 제거 |
| `Scripts/Combat/StraightProjectile.cs` | 직선 투사체 (관통·폭발 옵션) |
| `Scripts/Combat/ElementalZone.cs` | 원소 장판 (공용 시스템) |
| `Scripts/Combat/Skills/JeoktomaDash.cs` | 적토마 (이속+접촉 피해+장판 트레일) |
| `Scripts/Player/PlayerAimDirection.cs` | 마지막 이동 방향 추적 (D-018) |
| `Scripts/Player/DirectionalAutoAttack.cs` | 주몽 기본공격 (무유도, 무속성) |
| `Scripts/Player/AutoSkillCaster.cs` | 스킬 자동 시전 + 랜덤 속성 부여 |

**기존 파일 수정: `PlayerController.cs`에 `SpeedMultiplier` 프로퍼티 추가 (적토마용, 2줄).**
개발자 A 담당 파일이므로 PR에서 명시할 것. 기존 `Projectile.cs`·`SkillRunner.cs`·`PlayerAutoAttack.cs`는 수정 없음 — 유도 투사체는 영웅 2호 등에서 재사용 가능.

## 2. 플레이어 세팅

Player 프리팹(또는 CombatTest의 플레이어)에 컴포넌트 추가:

1. `PlayerAimDirection`
2. `DirectionalAutoAttack` — Basic Skill: 기본공격 SkillData, Enemy Layer: 적 레이어, Require Enemy In Range: 켬
3. `AutoSkillCaster` — Equipped Skills에 테스트할 스킬 SkillData 추가, Element를 None으로 두면 Play 시 랜덤 부여됨 (특정 속성 테스트 시 직접 지정)
4. 기존 `PlayerAutoAttack`는 **비활성화**(체크 해제)한다 — 삭제하지 말 것

## 3. 프리팹 제작

공통: 투사체 프리팹은 **Collider2D(Is Trigger 켬) + Rigidbody2D(Body Type: Kinematic)** 필요.
스프라이트는 흰색으로 두면 속성 색이 코드로 입혀진다. 화살은 오른쪽(+X)을 향하게 그린다.

### 3.1 BasicArrow (기본공격 화살)

```text
BasicArrow (루트)
├─ StraightProjectile   pierceCount 0, explosionRadius 0
├─ BoxCollider2D        Is Trigger ✔, 크기 0.4 x 0.1 정도
├─ Rigidbody2D          Kinematic, Gravity 0
├─ SpriteRenderer       흰색 가늘고 긴 사각형 (Square 스프라이트 scale 0.4, 0.08)
└─ TrailRenderer        Time 0.15, Width 0.06 → 0
```

### 3.2 ExplosiveArrow (원소 폭발 화살)

BasicArrow 복제 후:

- `StraightProjectile`: explosionRadius **1.5**, explosionDamageMultiplier 0.7
- Explosion Effect Prefab에 `FX_Explosion` 연결
- 스프라이트를 약간 크게 (머리에 원형 촉 추가 등)

### 3.3 Pyeonjeon (편전)

BasicArrow 복제 후:

- `StraightProjectile`: pierceCount **3** (스킬 레벨업 시 증가 예정)
- SkillData의 projectileSpeed를 기본공격의 2배 이상으로
- TrailRenderer Time 0.3으로 길게 (속도감)

### 3.4 FX_Hit / FX_Explosion (이펙트)

```text
FX_Hit (루트)
├─ AutoDestroyEffect    lifeTime 0.5
└─ Particle System
    Duration 0.3, Looping ✖, Start Lifetime 0.3, Start Speed 3
    Start Size 0.1, Emission: Burst 1회 8개
    Shape: Circle, Renderer Material: Sprites-Default
```

FX_Explosion은 복제 후 Burst 20개, Start Speed 5, lifeTime 0.8. 색은 코드가 속성에 맞게 입힌다.

### 3.5 Zone_Elemental (원소 장판)

```text
Zone_Elemental (루트)
├─ ElementalZone        duration 3, radius 1, tickInterval 0.5
├─ SpriteRenderer       Circle 스프라이트, 흰색, 알파 0.35, Order in Layer -1
└─ (선택) Particle System  은은하게 위로 올라오는 입자
```

주의: Circle 스프라이트는 **지름 1 유닛** 기준이어야 radius 스케일 계산이 맞는다 (Unity 기본 Circle이 지름 1).

### 3.6 Skill_Jeoktoma (적토마)

```text
Skill_Jeoktoma (루트, 빈 오브젝트)
├─ JeoktomaDash         duration 4, speedMultiplier 1.6, trampleRadius 0.7,
│                       trampleInterval 0.4, zoneSpacing 1.2, zoneDamageMultiplier 0.3
│                       Zone Prefab: Zone_Elemental
└─ (선택) Particle System  플레이어 뒤로 흩날리는 잔상 입자
```

콜라이더 불필요 (밟기 판정은 OverlapCircle로 처리).

## 4. SkillData 에셋 (생성 완료 — 프리팹 연결만 필요)

`ScriptableObjects/Skills/`에 아래 4개가 이미 생성되어 있다 (수치는 임시값, 테스트 후 조정).
**Skill Prefab 칸은 비어 있으므로** 3장의 프리팹 제작 후 Inspector에서 연결할 것:

| 에셋 | displayName | cooldown | damage | range | projectileSpeed | skillPrefab |
|---|---|---|---|---|---|---|
| SD_BasicArrow | 기본 화살 | 0.8 | 10 | 6 | 10 | BasicArrow |
| SD_ExplosiveArrow | 원소 폭발 화살 | 4 | 15 | 7 | 9 | ExplosiveArrow |
| SD_Pyeonjeon | 편전 | 3 | 12 | 8 | 22 | Pyeonjeon |
| SD_Jeoktoma | 적토마 | 8 | 8 | 5 | 0 | Skill_Jeoktoma |

## 5. 테스트 절차 (CombatTest 씬)

1. 적 프리팹 3~5개 배치 (레이어가 Enemy Layer 마스크와 일치하는지 확인)
2. Play → 이동하지 않아도 시작 방향(오른쪽)으로 기본공격 발사 확인
3. WASD 이동 후 멈춤 → 마지막 이동 방향으로 발사되는지 확인
4. Console에서 "보유 스킬: ... [속성]" 로그로 랜덤 속성 확인
5. 폭발 화살: 명중 시 주변 적 동시 피해 + 폭발 이펙트 색이 속성 색인지
6. 편전: 일직선 적 4마리 배치 → 3마리 관통(총 4명중) 확인
7. 적토마: 시전 중 이동 속도 증가, 지나간 자리 장판 생성, 장판 위 적 틱 피해
8. AutoSkillCaster 우클릭 → "모든 스킬 속성 랜덤 재부여"로 색 변화 확인

## 6. 알려진 제한 (다음 작업)

- 명중 시 속성 기록 → SynergyResolver 연계 판정 미구현 (TODO 주석 위치 참조)
- 적토마 중첩 시전 시 이속 배율이 단순 초기화됨 (MVP 허용)
- 멀티플레이 동기화 미적용 — 로컬 기준
- 스킬 습득 UI(레벨 3택1) 미구현 — `AutoSkillCaster.EquipSkill()`이 연결 지점
