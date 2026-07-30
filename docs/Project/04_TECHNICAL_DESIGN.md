# Shura 기술 설계서

## 1. 기술 목표

현재 단계의 목표는 대규모 확장성이 아니라 초보 개발자 세 명이 해커톤 마감 전까지 이해하고 수정할 수 있는 구조를 만드는 것이다. 지나친 추상화와 범용 프레임워크를 피하고, 캐릭터·무기·공격·아이템·적 수치를 코드 수정 없이 바꿀 수 있는 정도만 데이터화한다.

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
| 네트워크 | Netcode for GameObjects + Unity Multiplayer Services/Relay |

## 3. 권장 런타임 구조

이 절에는 현재 구현된 클래스와 앞으로 만들 계획인 클래스가 함께 있다. 실제 코드 작업의 기준은 아래 **현재 구현 코드 계약**이며, 아직 파일이 없는 이름을 구현 완료로 간주하지 않는다.

### Core

- `GameManager`: 게임 상태, 시간, 승패
- `GameState`: Ready, Playing, Paused, Result
- `SceneLoader`: 씬 전환

### Player

- `PlayerController`: 입력과 이동
- `PlayerHealth`: 체력, 피해, 사망
- `PlayerStats`: 이동 속도, 공격력, 쿨다운 등의 현재 수치
- `PlayerExperience`: 경험치와 레벨
- `CharacterLoadout`: 고유 무기, 특수공격, 아이템, 속성 보유 상태
- `ActiveSkillController`: 캐릭터 고유 액티브 입력, 충전 또는 쿨다운

### Combat

- `BasicAttackController`: 고유 무기의 기본공격 대상과 자동 발동 주기
- `SpecialAttackRunner`: 보유 특수공격의 조건과 쿨다운 실행
- `SkillRunner`: 현재 투사체형 공격 프리팹 생성과 피해 수치 전달
- `Projectile`: 이동과 충돌
- `IDamageable`: 피해를 받을 수 있는 대상의 공통 규약
- `AttributeRoller`: 선택지에 무작위 속성 부여
- `StatusEffectController`: 불, 얼음, 독 등의 속성 상태를 한 컴포넌트에서 관리
- `SynergyResolver`: 최근 속성을 확인하고 긍정적인 시너지 반응 결정
- `SupportItemController`: 보유 아이템의 지속·주기·조건부 효과 실행

### Enemy

- `EnemyController`: 추적과 행동
- `EnemyHealth`: 체력과 사망
- `EnemyAttack`: 접촉 또는 투사체 공격
- `EnemySpawner`: 적 생성
- `WaveManager`: 시간에 따른 웨이브 진행

### UI

- `HudController`: 체력, 시간, 처치 수, 보유 공격·아이템·속성
- `LevelUpPanel`: 특수공격·아이템·기본공격 강화·능력치 선택
- `LevelUpChoiceGenerator`: 현재 레벨 구간과 슬롯 상태에 맞는 선택지 생성
- `ResultPanel`: 결과와 재시작
- `SynergyPopup`: 속성 시너지 발동 표시

## 4. 현재 구현 코드 계약

이 절은 `Assets/_Project/Scripts`와 관련 프리팹·ScriptableObject를 기준으로 확인한 현재 연결 규칙이다. 새 기능을 만들거나 AI에 코드 작업을 요청할 때 먼저 읽는다. 구조를 의도적으로 바꾸는 작업이 아니라면 같은 책임을 가진 코드를 새로 만들지 않고 이 흐름을 확장한다.

### 4.1 2026년 7월 31일 구현 현황

상태 표시는 다음 기준을 사용한다.

- **구현:** 현재 스크립트·프리팹 또는 테스트 씬에서 핵심 동작을 확인할 수 있음
- **부분 구현:** 단독 기능은 있으나 실제 한 판이나 네트워크 흐름에 연결되지 않음
- **원격 반영:** 팀 원격 브랜치에는 있으나 현재 작업 브랜치에 아직 통합되지 않음
- **로컬 작업 중:** 커밋되지 않은 작업트리 변경으로 검증·통합 완료 전임
- **미구현:** 해당 책임의 프로젝트 스크립트 또는 실제 연결이 없음

| 기능 영역 | 상태 | 현재 확인된 범위 | 다음 작업·담당 |
|---|---|---|---|
| 프로젝트 기반 | 구현 | Unity `6000.3.20f1`, Universal 2D, Input System, NGO와 Multiplayer Services 패키지 설정 | 세 명 모두 같은 버전 유지 |
| 게임 실행·메뉴·입장 | 미구현 | 실제 `Boot`, `MainMenu`, 게임용 `Main` 씬 흐름 없음. Build Settings에는 `NetworkTest`만 활성화 | 진미리 |
| Relay 방 생성·참가 | 부분 구현 | `NetworkTestUI`에 UGS 초기화, 익명 로그인, 2인 Relay 세션 생성·코드 참가·퇴장·재접속 API가 있음 | 실제 메뉴와 게임 입장에 연결: 진미리 |
| 네트워크 플레이어 이동 | 부분 구현 | `NetworkPlayerMovement`가 소유자 입력과 위치 동기화 검증을 담당 | 일반 플레이어·카메라·체력·스킬과 통합: 진미리 |
| 일반 플레이어 | 부분 구현 | 이동, 카메라 추적, 체력, 사망 상태, 경험치 누적과 레벨 증가 구현 | 네트워크 플레이어 통합: 진미리 / 성장 선택: 이재준 / 종료 연결: 문성웅 |
| 기본 전투 | 구현 | 가장 가까운 적 자동 탐색, `SkillRunner` 생성, 유도 투사체, 재탐색, `IDamageable` 피해 구현 | 실제 네트워크 게임에서 회귀 테스트 필요 |
| 스킬 데이터 | 부분 구현 | `SkillData`와 `HomingShotData` 1개, 투사체형 기본공격 실행 가능 | 특수공격·캐릭터 액티브·추가 스킬·효과: 이재준 |
| 스킬 연계 | 미구현 | 기획 문서만 있고 `SkillTag`, 상태 효과, `SynergyResolver` 코드 없음 | 이재준, 네트워크 연결은 진미리 협업 |
| 기본 적 | 부분 구현 | 플레이어 추적, 접촉 공격, 체력, 사망, 경험치 구체 드롭과 플레이어 주변 생성 구현 | 적 종류·스폰 안전성·밸런스: 문성웅 / 네트워크: 진미리 |
| 경험치 | 부분 구현 | 적 사망 시 구체 생성, Trigger 습득, 누적 경험치와 복수 레벨업 구현 | 레벨업 선택: 이재준 / 자석 아이템: 문성웅 |
| 맵·웨이브·15분 타이머 | 부분 구현 | 현재 기능 브랜치에 `StageTest`, `EnemySpawner`, 60초 3단계 `WaveManager`가 있음. 15분 데이터·다양한 적은 없음 | 문성웅 |
| 보스·클리어·패배 | 부분 구현 | `GameManager`에 플레이어 사망 패배, 라운드 종료 후 보스 생성, 보스 제거 승리, 정지·재시작 코드가 있음 | 전용 보스·결과 UI·통합 Play 검증: 문성웅 |
| 회복·자석 아이템 | 로컬 작업 중 | `HealthPickup`, `MagnetPickup`, 경험치 오브 유도와 프리팹·`StageTest` 배치가 작업트리에 있음 | Play 검증, 드롭·스폰, 커밋: 문성웅 |
| HUD·결과 패널 | 원격 반영 | `origin/develop`에 HP·레벨·EXP와 사망 결과 패널이 있으나 현재 기능 브랜치에는 없음 | HUD 수정 브랜치 반영 후 통합: 진미리 |
| 레벨업·스킬·타이머 UI | 미구현 | 선택지, 스킬 쿨다운, 라운드 타이머와 최종 결과 통계 UI 없음 | 이재준·문성웅·진미리 |
| Windows 기술 검증 빌드 | 부분 구현 | 로컬 `Builds`에 Relay 테스트용 Windows 빌드 2종 존재 | 최신 코드로 재빌드·두 PC 회귀 테스트 |
| Windows/Web 최종 빌드 | 미구현 | 최종 게임 씬 빌드와 Web 링크를 확인하지 못함 | 진미리 통합 후 팀 전체 검증 |

현재 구현률을 숫자 하나로 표현하지 않는다. 테스트용 단독 기능이 있어도 게임 실행부터 15분 라운드 종료까지 이어지지 않으면 전체 게임 기능은 완료가 아니다.

역할과 통합 책임은 `03_TEAM_ROLES_AND_WORKFLOW.md`를 기준으로 한다. 기존 파일의 과거 작성자와 현재 기능 담당자는 다를 수 있으며, 이 표의 담당은 지금부터의 유지·확장 책임을 의미한다.

현재 상태를 읽을 때 브랜치를 함께 확인한다. `feat/map_generate`에는 맵·웨이브 코드가, `origin/develop`에는 HUD가 각각 존재하며 둘은 아직 하나의 통합 상태가 아니다. 회복·자석 아이템은 로컬 작업 중이므로 최종 구현으로 주장하지 않는다.

최신 로컬 Unity 로그에는 로컬 아이템 스크립트 반영 후 어셈블리 재로드와 레벨업·회복·라운드 종료·임시 보스·승패 실행 흔적이 있다. 이는 일부 동작 증거일 뿐 전체 체크리스트 통과나 최종 빌드 검증을 뜻하지 않는다.

### 4.2 현재 구현 파일과 책임

| 영역 | 파일 | 현재 책임 |
|---|---|---|
| 플레이어 | `Player/PlayerController.cs` | Input System의 `OnMove` 입력을 받아 `Rigidbody2D.linearVelocity`로 이동 |
| 플레이어 | `Player/PlayerAutoAttack.cs` | 가장 가까운 적 탐색, 공격 가능 여부 확인, 공격 성공 후 쿨다운 갱신 |
| 플레이어 | `Player/PlayerHealth.cs` | 플레이어 체력·사망 상태와 `onDeath`, `IDamageable` 구현 |
| 플레이어 | `Player/PlayerExperience.cs` | 경험치 구체 습득, 경험치 누적과 복수 레벨업 처리 |
| 전투 | `Combat/EnemyTargetFinder.cs` | 지정 위치·범위·레이어 안에서 `EnemyHealth`가 있는 가장 가까운 적 탐색 |
| 전투 | `Combat/SkillRunner.cs` | `SkillData` 검증, 공격 프리팹 생성, `Projectile.Initialize` 호출 |
| 전투 | `Combat/Projectile.cs` | 표적 추적·재탐색, 이동, 충돌 대상 검증, `IDamageable.TakeDamage` 호출, 수명 관리 |
| 전투 | `Combat/IDamageable.cs` | 피해를 받을 수 있는 오브젝트의 공통 공개 API인 `TakeDamage(float)` 정의 |
| 데이터 | `Data/SkillData.cs` | 스킬 ID·표시 정보·쿨다운·피해·사거리·투사체 속도·프리팹 보관 |
| 적 | `Enemy/EnemyController.cs` | `Player` 태그 대상 탐색과 `Rigidbody2D` 추적 이동 |
| 적 | `Enemy/EnemyAttack.cs` | 플레이어와 접촉 중 공격 간격에 따라 `IDamageable` 피해 적용 |
| 적 | `Enemy/EnemyHealth.cs` | 적 체력·중복 사망 방지·경험치 구체 생성, `IDamageable` 구현 |
| 스테이지 | `Stage/EnemySpawner.cs` | 플레이어 주변 임의 거리에서 적 생성, 생성 간격과 최대 생존 수 적용 |
| 스테이지 | `Stage/WaveManager.cs` | 테스트 라운드 시간 누적, 3단계 웨이브 수치 적용, 종료 시 스폰 정지 |
| 게임 흐름 | `Core/GameManager.cs` | Playing·Result 상태, 플레이어 사망 패배, 라운드 후 보스 생성, 보스 제거 승리, 재시작 |
| 경험치·아이템 | `Item/ExperienceOrb.cs` | 경험치 양 보관과 자석 획득용 목표 추적 이동. 현재 로컬 폴더 이동 작업 중 |
| 아이템 | `Item/HealthPickup.cs` | 플레이어 체력을 회복하고 실제 회복 성공 시에만 픽업 제거. 현재 로컬 작업 중 |
| 아이템 | `Item/MagnerPickup.cs` | 씬의 경험치 구체를 플레이어에게 유도. 파일명의 `Magner` 오탈자는 정리 필요 |
| 카메라 | `Camera/CameraFollow.cs` | `LateUpdate`에서 지정 대상을 보간 추적 |
| 네트워크 테스트 | `Network/Tests/NetworkPlayerMovement.cs` | 소유자만 입력·물리를 처리하는 네트워크 이동 검증 |
| 네트워크 테스트 | `Network/Tests/networkTestUI.cs` | UGS 초기화, 익명 로그인, Relay 세션 생성·참가·퇴장·재접속 API 검증 |
| UI | `UI/HUDController.cs` | HP·레벨·EXP와 사망 결과 패널. 현재 `origin/develop`에만 있으며 별도 수정 브랜치 존재 |

현재 작업 브랜치에는 `Core`와 `Stage` 구현이 있으며 `UI` 구현은 원격 develop에 있다. `SynergyResolver`, `StatusEffectController`, `LevelUpChoiceGenerator` 등 실제 파일이 없는 이름은 여전히 계획이다.

`Item` 폴더의 세 파일과 픽업 프리팹은 현재 로컬 작업트리 기준이다. 커밋 전 이름·`.meta`·참조·Play 동작을 검증하고, 검증 전에는 다른 브랜치에서 재사용할 안정 API로 보지 않는다.

### 4.3 스킬 생성·실행 규칙

새 기본공격이나 투사체형 스킬은 다음 경로를 사용한다.

```text
PlayerAutoAttack 또는 다른 발동 조건
→ EnemyTargetFinder.FindNearestEnemy(...)
→ SkillRunner.TryRun(SkillData, target)
→ SkillData.SkillPrefab 생성
→ Projectile.Initialize(target, projectileSpeed, damage)
→ 충돌 시 IDamageable.TakeDamage(damage)
```

- 스킬의 수치와 프리팹 참조는 `SkillData`에 둔다. 현재 공개 값은 `SkillId`, `DisplayName`, `Description`, `Cooldown`, `Damage`, `Range`, `ProjectileSpeed`, `SkillPrefab`이다.
- 공격을 발동하는 컴포넌트는 **언제 쓸지와 누구를 노릴지**만 결정한다. 프리팹을 직접 `Instantiate`하거나 `Projectile.Initialize`를 중복 호출하지 않고 `SkillRunner.TryRun`을 사용한다.
- `SkillRunner`는 **공격 오브젝트 생성과 초기화**만 담당한다. 대상 탐색이나 쿨다운 계산을 넣지 않는다.
- `TryRun`이 `true`를 반환한 뒤에만 발동 측 쿨다운을 갱신한다. 데이터·대상·프리팹·`Projectile`이 없어서 실패한 경우에는 쿨다운을 소비하지 않는다.
- `SkillRunner.firePoint`가 연결되어 있으면 그 위치에서, 없으면 `SkillRunner`가 붙은 오브젝트 위치에서 생성한다.
- 현재 `SkillRunner`가 실행하는 프리팹의 루트에는 `Projectile` 컴포넌트가 있어야 한다. 다른 실행 방식의 스킬을 추가할 때 기존 `TryRun`의 의미를 몰래 바꾸지 말고 실행 타입과 호환 방식을 먼저 설계한다.
- 현재 데이터 에셋 예시는 `ScriptableObjects/Skills/HomingShotData.asset`, 실행 프리팹은 `Prefabs/Combat/Projectile.prefab`이다. 새 에셋도 같은 책임 분리를 따른다.

### 4.4 피해 처리 규칙

- 피해를 받는 플레이어·적·파괴 가능 오브젝트는 `IDamageable`을 구현하고 `public void TakeDamage(float damage)`를 제공한다.
- 공격 코드는 구체 클래스인 `EnemyHealth`나 `PlayerHealth`를 직접 호출하지 않고 `IDamageable`을 통해 피해를 전달한다.
- 체력 감소, 사망 여부, 사망 이벤트와 드롭은 피해를 받는 구현체가 책임진다. `Projectile`이나 `EnemyAttack`이 상대 체력 필드를 직접 바꾸지 않는다.
- 현재 `Projectile`은 충돌 Collider의 부모에서 `IDamageable`을 찾는다. 자식 Collider를 쓰는 적도 루트나 부모에 `IDamageable` 구현체가 있으면 피해를 받을 수 있다.
- 현재 `EnemyAttack`은 `Player` 태그를 먼저 확인한 뒤 충돌한 **같은 GameObject**에서 `IDamageable`을 찾는다. 따라서 플레이어의 Collider와 `PlayerHealth`는 같은 오브젝트에 두는 것이 현재 프리팹 계약이다.
- 호출자는 0 이상의 피해값을 전달한다. `SkillData`의 `[Min]`은 Inspector 입력을 돕는 장치일 뿐 런타임 검증을 완전히 대신하지 않는다.

### 4.5 탐색·레이어·태그 규칙

- 적 루트는 `Enemy` 레이어(현재 Layer 8)를 사용한다.
- `PlayerAutoAttack.enemyLayer`와 `Projectile.enemyLayer`에는 같은 `Enemy` 레이어 마스크를 연결한다.
- `EnemyTargetFinder`는 `Physics2D.OverlapCircleAll` 결과에서 부모의 `EnemyHealth`를 찾고, 제곱 거리를 비교해 가장 가까운 `EnemyHealth.transform`을 반환한다. 단순히 레이어만 맞고 `EnemyHealth`가 없는 Collider는 표적이 아니다.
- `Projectile`은 충돌한 Collider의 Rigidbody 루트 또는 Transform 루트가 `enemyLayer`에 포함될 때만 피해 처리를 진행한다. 적 프리팹의 루트 레이어와 Collider 계층을 바꾸면 자동 공격 탐색과 실제 명중을 함께 재검증한다.
- 적의 근접 공격과 자동 추적은 현재 `Player` 태그를 사용한다. `Player.prefab` 루트의 태그를 유지하고, 네트워크 플레이어 프리팹을 실제 전투에 합칠 때도 태그·`IDamageable`·Collider 배치를 함께 맞춘다.

### 4.6 현재 프리팹 연결 계약

`Player.prefab` 루트에는 현재 `Rigidbody2D`, Collider, `PlayerInput`, `PlayerController`, `PlayerAutoAttack`, `SkillRunner`, `PlayerHealth`, `PlayerExperience`가 함께 있다.

- `PlayerAutoAttack.basicSkill`: 사용할 `SkillData`
- `PlayerAutoAttack.enemyLayer`: `Enemy` 레이어
- `PlayerAutoAttack.skillRunner`: 비어 있어도 `Awake`에서 같은 오브젝트의 `SkillRunner`를 찾음
- `SkillRunner.firePoint`: 현재 플레이어 자식 발사 위치
- `PlayerHealth.onDeath`: 사망 시 실행할 UnityEvent

`Enemy.prefab` 루트에는 현재 `Rigidbody2D`, Collider, `EnemyController`, `EnemyHealth`, `EnemyAttack`이 있다.

- `EnemyHealth.experienceOrbPrefab`: 사망 시 생성할 `ExperienceOrb.prefab`
- `EnemyHealth.experienceReward`: 생성 직후 구체에 전달할 경험치
- `EnemyController.target`: 비어 있으면 런타임에 `Player` 태그를 다시 탐색

`Projectile.prefab`은 Trigger Collider와 `Projectile`이 필요하며, `ExperienceOrb.prefab`은 Trigger Collider와 `ExperienceOrb`가 같은 오브젝트에 있어야 한다. `PlayerExperience`가 충돌한 같은 오브젝트에서 `ExperienceOrb`를 찾기 때문이다.

현재 로컬 작업 중인 `HealthPickup.prefab`과 `MagnetPickup.prefab`도 Trigger Collider와 해당 픽업 컴포넌트가 같은 오브젝트에 있다.

- `HealthPickup`은 `Player` 태그를 확인하고 같은 오브젝트의 `PlayerHealth.Heal`을 호출한다. 체력이 실제로 증가한 경우에만 픽업을 제거한다.
- `MagnetPickup`은 습득 시 현재 씬의 `ExperienceOrb`를 찾아 플레이어 Transform을 유도 목표로 지정한다.
- 두 픽업은 현재 `StageTest`에 수동 배치되어 있을 뿐, 적 드롭이나 라운드 스폰 규칙에는 아직 연결되지 않았다.

`StageTest.unity`에는 현재 일반 플레이어, 카메라, `EnemySpawner`, `WaveManager`, `GameManager`가 연결되어 있다.

- 테스트 라운드: 60초
- 웨이브별 기본 수치: 2초/15마리, 1초/25마리, 0.5초/40마리
- 일반 적 스폰: 플레이어로부터 7~10 거리의 임의 방향
- 라운드 종료: 일반 적 생성을 중지하고 `GameManager`가 보스 프리팹 생성
- 현재 보스 프리팹: 전용 보스가 아닌 일반 `Enemy.prefab` 임시 재사용
- 현재 씬 저장값: 2웨이브와 3웨이브 시작 시간이 모두 20초이므로 2웨이브가 건너뛰어지는지 검증·수정 필요

### 4.7 현재 구현 시 주의점

- 일반 전투용 `Player.prefab`과 `NetworkPlayer.prefab`은 아직 별도 구조다. `Network/Tests` 코드는 기술 검증용이므로 일반 전투가 네트워크 동기화되었다고 가정하지 않는다.
- 일반 플레이어 이동은 Input Actions의 `OnMove` 콜백을 사용하지만 네트워크 테스트 이동은 `Keyboard.current`를 직접 읽는다. 테스트 코드를 본 게임 입력 표준으로 복사하지 않는다.
- `PlayerController`, `PlayerHealth`, `CameraFollow`만 각각 `Shura.Player`, `Shura.Camera` 네임스페이스에 있고 나머지 현재 스크립트 다수는 전역 네임스페이스다. 클래스 위치를 추측하지 말고 실제 선언을 확인한다.
- `EnemyController`는 `Start`와 `FixedUpdate`에서 플레이어를 찾는 현재 코드 흐름이 서로 다르므로 추적 로직을 수정할 때 두 경로를 함께 확인한다.
- 로컬 작업트리에서는 `Enemy.prefab` 루트에 잘못 붙어 있던 `ExperienceOrb` 컴포넌트를 제거했다. 공식 드롭 흐름은 계속 `EnemyHealth`가 별도 `ExperienceOrb.prefab`을 생성하고 `Initialize`하는 경로다.
- `ExperienceOrb.cs`를 `Scripts/Experience`에서 `Scripts/Item`으로 이동하는 로컬 작업이 있으므로 Unity에서 `.meta` GUID와 프리팹 참조 보존을 확인한 뒤 커밋한다.
- `GameManager`는 현재 `PlayerHealth.IsDead`를 매 프레임 확인한다. `PlayerHealth.onDeath` 이벤트 기반으로 바꾸는 경우 관련 담당자와 공개 계약을 함께 갱신한다.
- `HUDController`의 원격 develop 버전은 `PlayerHealth` 네임스페이스와 사망 판정 수정이 필요했고 `origin/fix/hud-namespace-compile-error`에 수정이 존재한다. 통합할 때 원본과 수정 브랜치를 함께 확인한다.
- `GameManager`의 보스 사망 판정은 생성된 GameObject가 제거되었는지를 본다. 일반 적 프리팹 임시 연결은 흐름 검증용이며 보스 패턴 완료를 뜻하지 않는다.
- 현재 `PlayerTest`에는 `Player.prefab`이 가진 `PlayerExperience` 외에 같은 컴포넌트가 씬 오버라이드로 한 번 더 추가된 정황이 있어 중복 경험치 처리를 회귀 테스트해야 한다.
- 현재 `CombatTest`는 사실상 카메라만 있는 상태이므로 전투 회귀의 정본 씬으로 사용하기 전에 구성을 복구하거나 `PlayerTest`·`StageTest`로 테스트 기준을 통일한다.
- `PlayerHealthDebugTester`는 Space 키 피해 확인용 테스트 컴포넌트다. 실제 공격 시스템의 필수 구성요소로 사용하지 않는다.

### 4.8 스테이지·게임 종료 흐름

```text
StageTest 시작
→ GameManager.StartGame
→ WaveManager가 경과 시간에 따라 EnemySpawner 설정 변경
→ EnemySpawner가 플레이어 주변에 적 생성
→ 테스트 라운드 종료 시 일반 스폰 중지
→ GameManager가 보스 프리팹 생성
→ 보스 GameObject 제거 시 승리

별도 경로:
PlayerHealth.IsDead == true
→ 패배
→ Result 상태
→ Time.timeScale = 0
→ RestartGame 호출 시 현재 씬 재로드
```

- `WaveManager.RoundFinished`는 “보스까지 처치했다”가 아니라 “일반 라운드 시간이 끝났다”는 뜻이다.
- `GameManager.CurrentState`가 `Result`가 되면 승패 중복 처리를 막는다.
- 현재 스폰 목록과 게임 상태는 네트워크 변수가 아니다. 멀티플레이에서는 호스트가 스폰·웨이브·승패를 확정하도록 별도 통합이 필요하다.
- 60초와 세 웨이브는 개발 속도를 위한 테스트 값이다. 최종 15분 라운드는 `WaveData` 또는 동등한 데이터 구조로 옮기기 전까지 계획 상태다.
- 현재 코드에서 `GameManager`는 결과를 로그와 상태값으로만 남긴다. 실제 화면은 HUD 수정본과 연결해야 한다.

### 4.9 브랜치별 확인 기준

| 기준 | 포함된 핵심 변경 | 주의 |
|---|---|---|
| 현재 `feat/map_generate` | `StageTest`, 스포너, 웨이브, `GameManager` | HUD·최신 서사 문서가 없음 |
| `origin/develop` | HUD·결과 패널, 환경·캐릭터 서사 문서 | 현재 맵·웨이브 코드가 없음 |
| `origin/fix/hud-namespace-compile-error` | HUD 네임스페이스와 사망 판정 수정 | develop 반영 여부 확인 필요 |
| 현재 로컬 작업트리 | 회복·자석 픽업, 경험치 오브 이동, 회복 API | 미커밋·미검증이므로 완료 처리 금지 |

통합 전에는 어느 기준의 파일을 보고 있는지 확인한다. 특히 문서만 먼저 합친 상태와 실제 코드·프리팹까지 합친 상태를 구분한다.

## 5. 데이터 구조

게임 데이터는 ScriptableObject를 사용한다. 이것은 데이터베이스 ERD가 아니라 Unity 에디터에서 수정할 수 있는 설정 파일 구조다.

### CharacterData

```text
id
displayName
maxHealth
moveSpeed
uniqueWeapon
activeSkill
characterPrefab
portrait
```

### WeaponData

```text
id
displayName
description
basicAttack
specialAttackPool[]
weaponIcon
```

### AttackData

기본공격과 특수공격이 함께 사용하는 공통 데이터다. `attackType`으로 둘을 구분한다.

```text
id
displayName
description
attackType
cooldown
damage
range
projectileSpeed
activationCondition
allowedAttributes[]
attackPrefab
icon
```

### ActiveSkillData

```text
id
displayName
description
cooldownOrCharge
damage
effectPrefab
icon
```

### SupportItemData

```text
id
displayName
description
effectType
triggerCondition
duration
cooldown
attributePolicy
fixedAttribute
effectPrefab
icon
```

`attributePolicy`는 `Random`, `Fixed`, `None` 중 하나다. 공격속도 제한 해제·체력 회복은 `None`, 번개처럼 성격이 명확한 아이템은 `Fixed`를 사용할 수 있다.

### AttributeData

```text
id
displayName
color
statusEffect
effectPrefab
icon
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
requiredFirstAttribute
requiredSecondAttribute
triggerWindowSeconds
effectType
damageMultiplierOrValue
effectPrefab
internalCooldown
```

부정적인 결과를 만드는 시너지 데이터는 등록하지 않는다.

### WaveData

```text
startTime
endTime
enemyType
spawnInterval
maxAlive
healthMultiplier
```

## 6. 공격 선택과 시너지 처리 흐름

### 레벨업 선택

1. `PlayerExperience`가 레벨 상승을 알린다.
2. `LevelUpChoiceGenerator`가 현재 레벨 구간을 확인한다.
3. 특정 구간이면 특수공격 또는 아이템 후보를, 그 외 구간이면 기본공격 강화 또는 능력치 후보를 만든다.
4. 공격형 후보에는 `AttributeRoller`가 허용된 속성 중 하나를 부여한다.
5. 플레이어 선택 결과를 `CharacterLoadout`에 적용하고 HUD를 갱신한다.

무작위 속성은 공격이 발동할 때마다 다시 뽑지 않는다. 기본공격은 게임 시작 시, 특수공격·아이템은 선택지가 만들어질 때 결정하고 그 판의 빌드 데이터로 유지한다.

### 속성 시너지

1. 기본공격·특수공격·아이템 효과가 적 또는 영역에 적중한다.
2. 공격의 속성, 플레이어 ID, 적용 시간을 `StatusEffectController`에 기록한다.
3. `SynergyResolver`가 기존 속성과 새 속성이 호환되는지 확인한다.
4. 제한 시간 안의 유효한 조합이면 시너지 효과를 실행한다.
5. 피해·제어·범위 확대와 시각·음향 피드백을 발생시킨다.
6. 사용한 속성을 소비하거나 내부 쿨다운을 적용한다.

핵심 판정은 한 곳에서만 수행한다. 각 공격 코드에 시너지 조합을 직접 작성하지 않는다.

## 7. 멀티플레이 권한 원칙

네트워크 솔루션과 관계없이 다음 원칙을 목표로 한다.

- 자신의 플레이어 이동은 해당 플레이어가 입력한다.
- 적 생성, 웨이브, 적 체력, 승패는 호스트가 최종 결정한다.
- 기본공격·특수공격·액티브 사용 요청은 소유 플레이어가 보내고 실제 피해 결과는 호스트가 확정한다.
- 레벨업 선택지와 무작위 속성 결과는 모든 클라이언트가 같은 결과를 보도록 호스트가 결정하거나 결과값을 동기화한다.
- 모든 프레임의 모든 정보를 보내지 않는다.
- 위치, 체력, 생성·사망, 공격 발동, 빌드 선택, 시너지 결과처럼 판정에 필요한 상태만 동기화한다.

### 네트워크 기술 검증 최소 장면

`NetworkTest.unity`에는 다음만 둔다.

- 방 생성 버튼
- 참가 버튼
- 색이 다른 플레이어 사각형 2개
- 이동
- 적 사각형 1개
- 적 체력

전투 전체를 만들기 전에 이 장면으로 두 PC 연결과 Web 가능성을 검증한다.

현재 Relay 기반 외부 접속과 정상 퇴장 후 동일 코드 재접속은 확인했다. 강제 종료 후 재접속을 실행하는 UI는 아직 연결하지 않았으므로 현재 제한으로 기록한다.

## 8. 네트워크 솔루션 결정

네트워크는 **Netcode for GameObjects + Unity Multiplayer Services/Relay** 조합을 사용한다.

- 호스트가 세션을 생성하고 참가 코드를 공유한다.
- 클라이언트는 참가 코드를 입력해 세션에 접속한다.
- 적·웨이브·피해·시너지 판정은 호스트 권한을 기준으로 한다.
- 한 프로젝트에 다른 네트워크 패키지를 추가로 설치하지 않는다.
- Windows 두 대와 Web 빌드 가능성은 별도의 통합 테스트로 계속 검증한다.

## 9. 씬 구성

| 씬 | 목적 |
|---|---|
| `Boot.unity` | 초기 설정과 다음 씬 진입 |
| `MainMenu.unity` | 시작, 방 생성·참가 |
| `Main.unity` | 실제 게임 |
| `Result.unity` 또는 결과 패널 | 결과 표시 |
| `Tests/PlayerTest.unity` | 플레이어 기능 |
| `Tests/CombatTest.unity` | 전투와 적 |
| `Tests/NetworkTest.unity` | 멀티플레이 기술 검증 |
| `Tests/StageTest.unity` | 스폰·60초 웨이브·임시 보스·승패·픽업 통합 검증 |
| `Tests/ContentTest.unity` | UI·아트·데이터 |

MVP에서는 씬 수를 줄이기 위해 `Main` 안에 결과 패널을 넣어도 된다.

2026년 7월 31일 현재 실제로 존재하는 테스트 씬은 `PlayerTest`, `CombatTest`, `NetworkTest`, `StageTest`다. `Boot`, `MainMenu`, 게임용 `Main`, `Result`, `ContentTest`는 계획 이름이며 현재 구현된 진입 흐름이 아니다. Build Settings에는 `NetworkTest`만 활성화되어 있다.

## 10. 성능 원칙

- 총알과 적이 많이 생성되므로 Object Pool을 우선 적용한다.
- 매 프레임 `FindObjectOfType` 또는 전체 검색을 사용하지 않는다.
- 적의 추적 대상은 일정 주기로 갱신하거나 캐시한다.
- 적마다 속성별 컴포넌트를 여러 개 붙이지 않고 하나의 `StatusEffectController`에서 활성 상태만 관리한다.
- 시너지 판정은 속성이 새로 적용될 때만 수행하며 매 프레임 전체 조합을 검색하지 않는다.
- 물리 레이어로 불필요한 충돌을 막는다.
- Web 빌드는 Windows보다 일찍 테스트한다.
- 최적화는 실제 문제가 확인된 부분부터 진행한다.

## 11. 폴더 책임

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

## 12. 기술적 비목표

- 자체 ECS 프레임워크
- 범용 의존성 주입 프레임워크
- 자체 네트워크 서버
- 복잡한 세이브 암호화
- 데이터베이스
- 과도한 디자인 패턴 적용

현재는 읽기 쉬운 작은 컴포넌트와 명확한 데이터 흐름이 더 중요하다.
