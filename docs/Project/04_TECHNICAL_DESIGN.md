# Shura 기술 설계서

> **2026-07-31 갱신 안내:** 3장 Combat 구조와 4장 SkillData, 5장 연계 흐름은
> 실제 구현(7속성 기반)에 맞게 수정됐다. 기존 `SkillTag` 5종(Burn/Freeze/Impact/Wind/Shock) 구조는 D-010으로 폐기됐다.
> 구현 상세는 `IMPL_2026-07-31_JUMONG_SKILLS.md` 참고.

## 1. 기술 목표

현재 단계의 목표는 대규모 확장성이 아니라 초보 개발자 세 명이 해커톤 마감 전까지 이해하고 수정할 수 있는 구조를 만드는 것이다. 지나친 추상화와 범용 프레임워크를 피하고, 캐릭터·스킬·적 수치를 코드 수정 없이 바꿀 수 있는 정도만 데이터화한다.

## 2. 현재 기술 기준

| 항목 | 기준 |
|---|---|
| 엔진 | 팀 저장소 `ProjectVersion.txt`에 기록된 동일한 Unity 6.3 LTS 패치 |
| 렌더링 | Universal 2D |
| 언어 | C# |
| 입력 | Unity Input System |
| 주요 개발 빌드 | Windows |
| 심사 빌드 | Web |
| 버전 관리 | GitHub, Visible Meta Files, Force Text |
| 네트워크 | 7월 27일까지 기술 검증 후 하나만 선택 |

## 3. 권장 런타임 구조

### Core

- `GameManager`: 게임 상태, 시간, 승패
- `GameState`: Ready, Playing, Paused, Result
- `SceneLoader`: 씬 전환

### Player

- `PlayerController`: 입력과 이동
- `PlayerHealth`: 체력, 피해, 사망
- `PlayerStats`: 이동 속도, 공격력, 쿨다운 등의 현재 수치
- `PlayerExperience`: 경험치와 레벨

### Combat (2026-07-31 구현 반영)

- `IDamageable`: 피해를 받을 수 있는 대상의 공통 규약 — 구현됨
- `ISkillBehaviour` / `SkillCastContext`: 스킬 프리팹 공통 규약. 캐스터가 시전 정보를 넘긴다 — 구현됨
- `StraightProjectile`: 직선 투사체. 관통 수·폭발 반경을 프리팹 설정으로 전환 — 구현됨
- `ElementalZone`: 원소 장판 공용 시스템 — 구현됨
- `AutoDestroyEffect`: 명중·폭발 이펙트 수명 관리 — 구현됨
- `ElementType` / `ElementVisuals`: 7속성 정의와 속성별 색 적용 — 구현됨
- `JeoktomaDash`: 캐릭터 전용 스킬 예시(버프+장판형) — 구현됨
- `Projectile`(유도형), `SkillRunner`: 초기 구조. 현재 주몽에는 미사용하며 재사용 대비 보존
- `ElementApplier`: 명중한 적에게 (속성, 플레이어 ID, 시간) 기록 — **미구현**
- `SynergyResolver`: 기록된 속성을 확인해 연계 반응 결정 — **미구현**

### Player

- `PlayerAimDirection`: 마지막 이동 방향 추적 — 구현됨
- `DirectionalAutoAttack`: 기본공격 자동 발사 — 구현됨
- `AutoSkillCaster`: 보유 스킬 쿨다운 자동 시전 + 랜덤 속성 부여 — 구현됨

### Enemy

- `EnemyController`: 추적과 행동
- `EnemyHealth`: 체력과 사망
- `EnemyAttack`: 접촉 또는 투사체 공격
- `EnemySpawner`: 적 생성
- `WaveManager`: 시간에 따른 웨이브 진행

### UI

- `HudController`: 체력, 시간, 처치 수
- `LevelUpPanel`: 성장 선택
- `ResultPanel`: 결과와 재시작
- `SynergyPopup`: 연계 발동 표시

## 4. 데이터 구조

게임 데이터는 ScriptableObject를 사용한다. 이것은 데이터베이스 ERD가 아니라 Unity 에디터에서 수정할 수 있는 설정 파일 구조다.

### CharacterData

```text
id
displayName
maxHealth
moveSpeed
startingSkill
characterPrefab
portrait
```

### SkillData (구현 반영)

```text
skillId
displayName
description
cooldown
damage
range
projectileSpeed
skillPrefab
```

속성은 SkillData에 고정하지 않는다. 습득 시점에 랜덤으로 부여되어(D-010)
`AutoSkillCaster`가 보유 스킬별로 들고 있다가 `SkillCastContext.Element`로 전달한다.
관통 수·폭발 반경·장판 지속시간처럼 스킬 고유 특성은 프리팹의 컴포넌트 설정값으로 둔다.

### EnemyData

```text
id
displayName
maxHealth
moveSpeed
damage
experienceReward
enemyPrefab
```

### SynergyData (미구현, 7속성 기준으로 갱신)

```text
id
displayName
requiredFirstElement    // ElementType
requiredSecondElement   // ElementType
triggerWindowSeconds
damageMultiplier
effectPrefab
internalCooldown
```

조합은 순서를 구분하지 않는다. 확정된 4조합은 D-015 참고.

### WaveData

```text
startTime
endTime
enemyType
spawnInterval
maxAlive
healthMultiplier
```

## 5. 연계 처리 흐름

1. 스킬(투사체·폭발·장판)이 적에게 피해를 준다.
2. 명중한 스킬의 **속성**, 플레이어 ID, 시간을 적의 속성 기록 컴포넌트에 남긴다.
3. `SynergyResolver`가 기존 속성과 새 속성이 확정 조합에 해당하는지 확인한다.
4. **서로 다른 플레이어**가 제한 시간 안에 적용했다면 반응을 실행한다.
   동속성 중첩은 반응하지 않는다(D-017). 조합에 없으면 무반응이며 디버프도 없다(D-012).
5. 피해, 상태 효과, 시각·음향 효과를 발생시킨다.
6. 사용한 속성 기록을 소비하거나 내부 쿨다운을 적용한다.

핵심 판정은 한 곳에서만 수행한다. 각 스킬 코드에 연계 조합을 직접 작성하지 않는다.
현재 연동 지점은 `StraightProjectile.OnTriggerEnter2D`와 `ElementalZone.DamageEnemiesInside`의
`TODO(연계)` 주석 위치다.

## 6. 멀티플레이 권한 원칙

네트워크 솔루션과 관계없이 다음 원칙을 목표로 한다.

- 자신의 플레이어 이동은 해당 플레이어가 입력한다.
- 적 생성, 웨이브, 적 체력, 승패는 호스트가 최종 결정한다.
- 스킬 사용 요청은 플레이어가 보내고 실제 피해 결과는 호스트가 확정한다.
- 모든 프레임의 모든 정보를 보내지 않는다.
- 위치, 체력, 생성·사망, 스킬 발동처럼 결과에 필요한 상태만 동기화한다.

### 네트워크 기술 검증 최소 장면

`NetworkTest.unity`에는 다음만 둔다.

- 방 생성 버튼
- 참가 버튼
- 색이 다른 플레이어 사각형 2개
- 이동
- 적 사각형 1개
- 적 체력

전투 전체를 만들기 전에 이 장면으로 두 PC 연결과 Web 가능성을 검증한다.

## 7. 네트워크 솔루션 선택 기준

한 프로젝트에 여러 네트워크 패키지를 동시에 설치하지 않는다. 아래 조건을 비교한 뒤 하나만 선택한다.

- Unity 6.3 LTS와 호환되는가?
- Windows 두 대에서 방 생성·참가가 되는가?
- Web 빌드를 지원하거나 현실적인 제출 폴백이 있는가?
- 무료 범위가 심사와 테스트에 충분한가?
- 초보자가 참고할 공식 문서와 예제가 충분한가?
- Lobby/Relay 또는 방 코드 방식이 쉬운가?

## 8. 씬 구성

| 씬 | 목적 |
|---|---|
| `Boot.unity` | 초기 설정과 다음 씬 진입 |
| `MainMenu.unity` | 시작, 방 생성·참가 |
| `Main.unity` | 실제 게임 |
| `Result.unity` 또는 결과 패널 | 결과 표시 |
| `Tests/PlayerTest.unity` | 플레이어 기능 |
| `Tests/CombatTest.unity` | 전투와 적 |
| `Tests/NetworkTest.unity` | 멀티플레이 기술 검증 |
| `Tests/ContentTest.unity` | UI·아트·데이터 |

MVP에서는 씬 수를 줄이기 위해 `Main` 안에 결과 패널을 넣어도 된다.

## 9. 성능 원칙

- 총알과 적이 많이 생성되므로 Object Pool을 우선 적용한다.
- 매 프레임 `FindObjectOfType` 또는 전체 검색을 사용하지 않는다.
- 적의 추적 대상은 일정 주기로 갱신하거나 캐시한다.
- 물리 레이어로 불필요한 충돌을 막는다.
- Web 빌드는 Windows보다 일찍 테스트한다.
- 최적화는 실제 문제가 확인된 부분부터 진행한다.

## 10. 폴더 책임

```text
Assets/_Project/
├─ Art
├─ Audio
├─ Prefabs
├─ Scenes
├─ Scripts
├─ ScriptableObjects
└─ Settings
```

외부 패키지나 에셋은 가능하면 `Assets/ThirdParty`에 분리하고 자체 파일과 섞지 않는다.

## 11. 기술적 비목표

- 자체 ECS 프레임워크
- 범용 의존성 주입 프레임워크
- 자체 네트워크 서버
- 복잡한 세이브 암호화
- 데이터베이스
- 과도한 디자인 패턴 적용

현재는 읽기 쉬운 작은 컴포넌트와 명확한 데이터 흐름이 더 중요하다.
