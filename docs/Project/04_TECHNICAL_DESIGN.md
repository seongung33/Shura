# Shura 기술 설계서

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

### Combat

- `AutoAttackController`: 자동 공격 대상과 발동 주기
- `SkillRunner`: 보유 스킬 실행
- `Projectile`: 이동과 충돌
- `Damageable`: 피해를 받을 수 있는 대상의 공통 규약
- `StatusEffectController`: Burn, Freeze 등의 상태 효과
- `SynergyResolver`: 최근 태그를 확인하고 연계 반응 결정

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

### SkillData

```text
id
displayName
description
cooldown
damage
range
projectileSpeed
tags[]
skillPrefab
```

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

### SynergyData

```text
id
displayName
requiredFirstTag
requiredSecondTag
triggerWindowSeconds
damageMultiplier
effectPrefab
internalCooldown
```

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

1. 스킬이 적에게 명중한다.
2. 명중한 스킬의 태그, 플레이어 ID, 시간을 적의 상태 효과 컴포넌트에 기록한다.
3. `SynergyResolver`가 기존 태그와 새 태그가 호환되는지 확인한다.
4. 서로 다른 플레이어가 제한 시간 안에 적용했다면 반응을 실행한다.
5. 피해, 상태 효과, 시각·음향 효과를 발생시킨다.
6. 사용한 태그를 소비하거나 내부 쿨다운을 적용한다.

핵심 판정은 한 곳에서만 수행한다. 각 스킬 코드에 연계 조합을 직접 작성하지 않는다.

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
