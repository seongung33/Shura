# 구현 기록 — 주몽 기본공격 · P0 스킬 3종 (2026-07-31)

> **구현 이력 문서:** 2026년 7월 31일 당시의 작업과 로컬 Play 결과를 기록한다. 현재 상태는 `12_CURRENT_PROJECT_STATUS.md`를 따른다. SkillData는 현재 `ScriptableObjects/Skills/Jumong/` 경로에 커밋되어 있다.

담당: 아침호랑이 (기본공격·스킬)
상태: **Play 테스트 완료, 정상 동작 확인**
관련 결정: D-010, D-013, D-014, D-015~D-019
세팅 절차: `GUIDE_JUMONG_SKILLS_SETUP.md`

## 1. 구현 범위

| 기능 | 문서 근거 | 상태 |
|---|---|---|
| 주몽 기본공격 (무유도 단발 화살, 마지막 이동 방향) | D-013, D-018 | 완료 |
| 원소 폭발 화살 (P0) | D-014 | 완료 |
| 편전 (P0, 관통) | D-014 | 완료 |
| 적토마 (P0, 이속 + 밟기 피해 + 원소 장판) | D-014 | 완료 |
| 원소 장판 공용 시스템 | 11번 문서 5장 | 완료 |
| 관통 공용 판정 | 11번 문서 5장 | 완료 |
| 7속성 + 랜덤 부여 + 속성별 이펙트 색 | D-010, D-016 | 완료 |

## 2. 추가·수정 파일

### 신규 스크립트 (10개)

| 파일 | 역할 |
|---|---|
| `Scripts/Combat/Element/ElementType.cs` | 7속성 enum, 속성별 색상·한글명, 랜덤 속성 부여 |
| `Scripts/Combat/Element/ElementVisuals.cs` | SpriteRenderer·TrailRenderer·ParticleSystem을 속성 색으로 일괄 적용 |
| `Scripts/Combat/ISkillBehaviour.cs` | 스킬 프리팹 공통 규약(`Cast`) + `SkillCastContext` 구조체 |
| `Scripts/Combat/StraightProjectile.cs` | 직선 투사체. 관통 수·폭발 반경을 프리팹 설정으로 전환 |
| `Scripts/Combat/ElementalZone.cs` | 원소 장판(공용). 지속시간·반경·틱 간격 기반 범위 피해 |
| `Scripts/Combat/AutoDestroyEffect.cs` | 명중·폭발 이펙트 자동 제거 + 속성 색 적용 |
| `Scripts/Combat/Skills/JeoktomaDash.cs` | 적토마. 이속 버프 + 주기적 밟기 피해 + 이동 거리 기반 장판 생성 |
| `Scripts/Player/PlayerAimDirection.cs` | Rigidbody2D 속도로 마지막 이동 방향 추적 |
| `Scripts/Player/DirectionalAutoAttack.cs` | 기본공격 자동 발사(무속성, 사거리 내 적 존재 시) |
| `Scripts/Player/AutoSkillCaster.cs` | 보유 스킬 쿨다운 자동 시전 + 습득 시 랜덤 속성 부여 |

### 기존 파일 수정

| 파일 | 변경 | 비고 |
|---|---|---|
| `Scripts/Player/PlayerController.cs` | `SpeedMultiplier` 프로퍼티 추가 (적토마 이속 버프용) | 개발자 A 담당 파일 — PR에서 명시 |
| `Prefabs/Players/Player.prefab` | `PlayerHealth` 스크립트 참조 GUID 복구 | develop에 있던 Missing Script 오류 수정 |

### 보존 (수정 안 함)

`Projectile.cs`(유도형), `SkillRunner.cs`, `PlayerAutoAttack.cs` — 유도 투사체는 영웅 2호 등에서 재사용 가능하므로 삭제하지 않고 Player 프리팹에서 비활성화만 함.

## 3. 생성한 에셋

### 프리팹 (`Prefabs/Combat/`)

| 프리팹 | 구성 | 주요 설정값 |
|---|---|---|
| `BasicArrow` | StraightProjectile + BoxCollider2D(Trigger) + Rigidbody2D(Kinematic) + SpriteRenderer + TrailRenderer | lifeTime 3, pierce 0, explosion 0 |
| `ExplosiveArrow` | 위와 동일 | explosionRadius 1.5, explosionDamageMultiplier 0.7 |
| `Pyeonjeon` | 위와 동일 | pierceCount 3 |
| `Zone_Elemental` | ElementalZone + Circle SpriteRenderer(반투명) | duration 3, radius 1, tickInterval 0.5 |
| `Skill_Jeoktoma` | JeoktomaDash (빈 오브젝트) | duration 4, speedMultiplier 1.6, trampleRadius 0.7, zoneSpacing 1.2, zoneDamageMultiplier 0.3 |

> `StraightProjectile.prefab`은 프리팹 분리 이전의 원본이며 현재 어떤 SkillData도 참조하지 않는다. 정리 대상.

### SkillData (`ScriptableObjects/Skills/`)

| 에셋 | 쿨다운 | 피해 | 사거리 | 투사체 속도 | 연결 프리팹 |
|---|---|---|---|---|---|
| `SD_BasicArrow` | 0.8 | 10 | 6 | 10 | BasicArrow |
| `SD_ExplosiveArrow` | 4 | 15 | 7 | 9 | ExplosiveArrow |
| `SD_Pyeonjeon` | 3 | 12 | 8 | 22 | Pyeonjeon |
| `SD_Jeoktoma` | 8 | 8 | 5 | 0 | Skill_Jeoktoma |

전부 임시 수치. 웨이브·적 체력이 확정되면 밸런스 조정 필요.

## 4. 테스트 결과 (Play 모드, 로컬 단독)

| 항목 | 결과 |
|---|---|
| 기본공격이 마지막 이동 방향으로 발사되고 명중 시 피해 | 통과 |
| 원소 폭발 화살 명중 시 주변 적 범위 피해 | 통과 |
| 편전이 여러 적을 관통하며 각각 피해 | 통과 |
| 적토마 이동 속도 증가 + 장판 생성 + 장판 틱 피해 | 통과 |
| 스킬 습득 시 랜덤 속성 부여 (Console 로그) | 통과 |
| 스프라이트·트레일이 부여된 속성 색으로 변경 | 통과 |

## 5. 2026-07-31 당시 알려진 제한

- **연계 판정 미구현**: 명중 시 속성 기록 → `SynergyResolver` 연동이 남음. 연결 지점은 `StraightProjectile.OnTriggerEnter2D`와 `ElementalZone.DamageEnemiesInside`의 `TODO(연계)` 주석
- **멀티플레이 동기화 미적용** — 로컬 기준 구현
- 적토마 중첩 시전 시 이속 배율이 곱해지지 않고 초기화됨 (MVP 허용)
- 스킬 습득 UI(특정 레벨 3택1, D-019) 미구현 — `AutoSkillCaster.EquipSkill()`이 연결 지점
- Object Pool 미적용 (투사체·장판 모두 Instantiate/Destroy)
- P1 스킬(궁사의 패러독스·화살비틀기), 필살기 화살비 미착수
- 기본공격 레벨업 강화(직선 관통·차지형) 미착수

## 6. 다음 작업

1. 적에게 속성 기록 컴포넌트 + `SynergyResolver` 구현 → 연계 4조합 (D-015)
2. `04_TECHNICAL_DESIGN.md`의 SkillTag 5종 구조를 7속성 구조로 갱신
3. 스킬 습득 UI (레벨 3택1 + 속성 미리 표시)
4. Git 커밋 및 PR (`feat/jumong-basic-attack-skills` → develop)

> 2026-08-09 정적 갱신: 네트워크 기본·폭발·편전·적토마 권한 연결과 감전·분쇄 연계 2종 코드는 이후 커밋되었다. 이 문서의 테스트 표는 7월 31일 로컬 결과이며 최신 통합본 또는 실제 2인 실행 완료를 뜻하지 않는다.
