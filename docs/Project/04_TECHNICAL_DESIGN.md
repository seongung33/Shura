# Shura 기술 설계서

> 최종 구조 정리: **2026-08-09 KST**
> 이 문서는 코드 계약과 런타임 구조의 정본이다. 현재 구현·브랜치 상태는 `12_CURRENT_PROJECT_STATUS.md`, 검증 결과는 `10_TEST_PLAN.md`를 따른다.

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

### Combat (2026-07-31 구현 반영)

- `BasicAttackController`: 고유 무기의 기본공격 대상과 자동 발동 주기
- `SpecialAttackRunner`: 보유 특수공격의 조건과 쿨다운 실행
- `AttributeRoller`: 선택지에 무작위 속성 부여
- `StatusEffectController`: 불, 얼음, 독 등의 속성 상태를 한 컴포넌트에서 관리
- `SupportItemController`: 보유 아이템의 지속·주기·조건부 효과 실행
- `IDamageable`: 피해를 받을 수 있는 대상의 공통 규약 — 구현됨
- `ISkillBehaviour` / `SkillCastContext`: 스킬 프리팹 공통 규약. 캐스터가 시전 정보를 넘긴다 — 구현됨
- `StraightProjectile`: 직선 투사체. 관통 수·폭발 반경을 프리팹 설정으로 전환 — 구현됨
- `ElementalZone`: 원소 장판 공용 시스템 — 구현됨
- `AutoDestroyEffect`: 명중·폭발 이펙트 수명 관리 — 구현됨
- `ElementType` / `ElementVisuals`: 7속성 정의와 속성별 색 적용 — 구현됨
- `JeoktomaDash`: 캐릭터 전용 스킬 예시(버프+장판형) — 구현됨
- `Projectile`(유도형), `SkillRunner`: 초기 구조. 현재 주몽에는 미사용하며 재사용 대비 보존
- `IElementReceiver` / `ElementalStatusController`: 명중한 적에게 속성·플레이어 ID·시간 기록 — 구현됨
- `SynergyResolver`: 물+번개의 감전, 얼음+흙의 분쇄 조합 판정 — 구현됨

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

- `HudController`: 체력, 시간, 처치 수, 보유 공격·아이템·속성
- `LevelUpPanel`: 특수공격·아이템·기본공격 강화·능력치 선택
- `LevelUpChoiceGenerator`: 현재 레벨 구간과 슬롯 상태에 맞는 선택지 생성
- `ResultPanel`: 결과와 재시작
- `SynergyPopup`: 속성 시너지 발동 표시

## 4. 현재 구현 코드 계약

이 절은 `Assets/_Project/Scripts`와 관련 프리팹·ScriptableObject를 기준으로 확인한 현재 연결 규칙이다. 새 기능을 만들거나 AI에 코드 작업을 요청할 때 먼저 읽는다. 구조를 의도적으로 바꾸는 작업이 아니라면 같은 책임을 가진 코드를 새로 만들지 않고 이 흐름을 확장한다.

### 4.1 상태 문서와의 경계

이 문서에는 날짜별 구현 현황표나 브랜치별 진행 상황을 복제하지 않는다.

- 현재 구현·작업트리·위험·다음 순서: `12_CURRENT_PROJECT_STATUS.md`
- 테스트 절차와 통과 증거: `10_TEST_PLAN.md`
- 담당과 통합 책임: `03_TEAM_ROLES_AND_WORKFLOW.md`
- 날짜별 이력: `05_DAILY_PLAN_2026-07-25_to_08-10.md`

아래 절은 상태가 아니라 새 코드를 연결할 때 유지해야 하는 현재 계약을 설명한다.

### 4.2 현재 구현 파일과 책임

| 영역 | 파일 | 현재 책임 |
|---|---|---|
| 플레이어 | `Player/PlayerController.cs` | Input System의 `OnMove` 입력을 받아 `Rigidbody2D.linearVelocity`로 이동 |
| 플레이어 | `Player/DirectionalAutoAttack.cs` | 주몽 기본공격을 마지막 이동 방향으로 자동 발사 |
| 플레이어 | `Player/AutoSkillCaster.cs` | 장착 스킬의 랜덤 속성과 쿨다운·자동 시전 관리 |
| 플레이어 | `Player/PlayerHealth.cs` | 플레이어 체력·회복·사망 상태와 `onDeath`, `IDamageable` 구현. 네트워크 플레이어에서는 `NetworkPlayerHealth`의 동기화 상태 적용 |
| 플레이어 | `Player/PlayerExperience.cs` | 경험치 구체 습득, 경험치 누적과 복수 레벨업 처리. 네트워크 플레이어에서는 `NetworkPlayerExperience`가 동기화 값을 적용 |
| 전투 | `Combat/EnemyTargetFinder.cs` | 지정 위치·범위·레이어 안에서 `EnemyHealth`가 있는 가장 가까운 적 탐색 |
| 전투 | `Combat/SkillRunner.cs`, `Combat/Projectile.cs` | 초기 유도 투사체 생성·초기화·재탐색 경로. 주몽에는 현재 미사용 |
| 전투 | `Combat/ISkillBehaviour.cs` | 직선·장판·버프형 스킬 프리팹에 시전 정보를 전달하는 공통 계약 |
| 전투 | `Combat/StraightProjectile.cs` | 직선 이동, 관통·폭발, 속성 색과 `IDamageable` 피해 |
| 전투 | `Combat/ElementalZone.cs` | 반경·지속시간·틱 간격 기반 장판 피해 |
| 전투 | `Combat/IDamageable.cs` | 피해를 받을 수 있는 오브젝트의 공통 공개 API인 `TakeDamage(float)` 정의 |
| 데이터 | `Data/SkillData.cs` | 스킬 ID·표시 정보·쿨다운·피해·사거리·투사체 속도·프리팹 보관 |
| 적 | `Enemy/EnemyController.cs` | `Player` 태그 대상 탐색과 `Rigidbody2D` 추적 이동 |
| 적 | `Enemy/EnemyAttack.cs` | 플레이어와 접촉 중 공격 간격에 따라 `IDamageable` 피해 적용 |
| 적 | `Enemy/EnemyHealth.cs` | 적 체력·중복 사망 방지·경험치 구체 생성, `IDamageable` 구현. NGO 적은 클라이언트 명중 요청을 서버에서 적용하고 `NetworkVariable` 체력과 서버 `Despawn` 사용 |
| 스테이지 | `Stage/EnemySpawner.cs` | 플레이어 주변 임의 거리에서 적 생성, 생성 간격과 최대 생존 수 적용. NGO 세션 중에는 서버만 생성하고 `NetworkObject.Spawn` 실행 |
| 스테이지 | `Stage/WaveManager.cs` | 테스트 라운드 시간 누적, 3단계 웨이브 수치 적용, 종료 시 스폰 정지 |
| 게임 흐름 | `Core/GameManager.cs` | Playing·Result 상태, 플레이어 사망 패배, 라운드 후 보스 생성, 보스 제거 승리, 재시작 |
| 경험치·아이템 | `Item/ExperienceOrb.cs` | 경험치 양 보관과 자석 획득용 목표 추적 이동. NGO 세션에서는 서버 생성·소유권/거리 검증·서버 제거 사용 |
| 아이템 | `Item/HealthPickup.cs` | 플레이어 체력을 회복하고 실제 회복 성공 시에만 픽업 제거 |
| 아이템 | `Item/MagnerPickup.cs` | 씬의 경험치 구체를 플레이어에게 유도. 파일명의 `Magner` 오탈자는 정리 필요 |
| 카메라 | `Camera/CameraFollow.cs` | `LateUpdate`에서 지정 대상을 보간 추적 |
| 메뉴 | `UI/StartMenuController.cs` | `MainMenu`에서 `MultiPlayerEntry`로, 입장 화면에서 `MainMenu`로 일반 씬 전환. 싱글플레이 연결은 없음 |
| 캐릭터 데이터·UI | `Data/CharacterData.cs`, `UI/CharacterListUI.cs`, `UI/CharacterSlotUI.cs`, `UI/CharacterInfoUI.cs`, `UI/MyPlayerSlotUI.cs` | 캐릭터 이름·역할·설명·초상화 데이터와 슬롯 생성·선택 표시. 현재 데이터는 주몽 1개 |
| 레거시 네트워크 로비 | `Network/Lobby/NetworkTestUI.cs` | `Tests/NetworkTest` 전용 UGS·Relay 생성·참가·퇴장·재접속. 정식 메뉴 경로와 별도 유지 |
| 멀티 입장 | `Network/Player/MultiplayerEntryUI.cs` | `MultiPlayerEntry`의 UGS 초기화, Relay 2인 세션 생성·코드 참가·퇴장과 실패 정리 |
| 네트워크 세션 | `Network/Lobby/NetworkSessionState.cs` | 정식 멀티 흐름의 `ISession`과 참가 코드를 `NetworkManager`에 보관하고 네트워크 상태 이벤트 중계 |
| 네트워크 로비 전환 | `Network/Lobby/NetworkLobbySceneLoader.cs` | 호스트 네트워크·참가 코드 준비 후 NGO로 `MultiPlayerLobby` 로드, PlayerObject 보존 |
| 네트워크 로비 상태 | `Network/Lobby/NetworkLobbyState.cs` | 최대 2개 슬롯의 클라이언트 ID·캐릭터 ID를 서버 쓰기 `NetworkVariable`로 복제 |
| 네트워크 로비 UI | `Network/Lobby/MultiplayerLobbyUI.cs`, `Network/Lobby/MultiplayerLobbyExitController.cs` | 참가 코드·접속 인원·호스트 시작 UI 바인딩, 세션 종료 후 `MultiPlayerEntry` 복귀 |
| 네트워크 게임 진입 | `Network/Lobby/NetworkGameFlowController.cs` | 호스트·2인 조건과 NGO `Main` 씬 전환. 코드와 정식·레거시 씬 모두 최소 2명·최대 2명으로 통일 |
| 네트워크 플레이어 | `Network/Player/NetworkPlayerMovement.cs` | 실제 네트워크 프리팹에서 사용하는 소유자 이동 |
| 네트워크 플레이어 | `Network/Player/NetworkPlayerOwnerSetup.cs` | 비소유 입력·공격·스킬 비활성화와 소유자 카메라 연결 |
| 네트워크 플레이어 | `Network/Player/NetworkPlayerCharacter.cs` | 서버가 로비 선택 ID를 플레이어 `NetworkVariable`에 보존하고 모든 클라이언트에서 외형·체력·이동속도·기본공격·시작 스킬 적용 |
| 네트워크 스킬 | `Network/Player/NetworkSkillCastRelay.cs` | 소유자의 주몽 스킬 요청을 서버에서 허용 목록·쿨다운·발동 위치로 검증하고 서버 판정본과 비서버 시각 복제본 생성 |
| 네트워크 플레이어 | `Network/Player/NetworkPlayerHealth.cs`, `Network/Player/NetworkPlayerExperience.cs` | 서버 HP·사망·EXP·레벨 상태 복제 |
| 네트워크 적 | `Network/Enemy/NetworkEnemyAuthoritySetup.cs` | 적 AI·공격·물리를 서버로 제한 |
| 네트워크 스테이지 | `Network/Stage/NetworkStageBootstrap.cs` | `Main`에서 플레이어 보정과 스테이지 컴포넌트 런타임 조립 |
| 네트워크 결과 | `Network/Result/NetworkGameResultState.cs` | 참가자 전원의 승리·패배 상태 복제 |
| 네트워크 결과 UI | `Network/Player/NetworkPlayerHudPresenter.cs`, `Network/Result/NetworkGameResultPresenter.cs` | 소유 플레이어의 HUD·결과 오버레이 런타임 생성 |
| 네트워크 결과 전환 | `Network/Result/NetworkGameResultActions.cs` | 호스트 재시작과 모든 참가자의 로비 복귀 |
| 로컬 UI | `UI/HUDController.cs` | `StageTest` 등 로컬 씬의 HP·레벨·EXP와 결과 표시 |

`SynergyResolver`는 `Combat/Element/SynergyResolver.cs`로 구현되어 있다. `StatusEffectController`, `LevelUpChoiceGenerator` 등 실제 파일이 없는 이름은 계획이다.

### 4.3 스킬 생성·실행 규칙

현재 공격에는 두 실행 경로가 있다.

주몽 기본공격·P0 스킬:

```text
DirectionalAutoAttack 또는 AutoSkillCaster
→ SkillData.SkillPrefab 생성
→ ISkillBehaviour.Cast(SkillCastContext)
→ StraightProjectile / JeoktomaDash / ElementalZone
→ IDamageable.TakeDamage
```

보존 중인 초기 유도 투사체:

```text
PlayerAutoAttack
→ EnemyTargetFinder.FindNearestEnemy(...)
→ SkillRunner.TryRun(SkillData, target)
→ SkillData.SkillPrefab 생성
→ Projectile.Initialize(target, projectileSpeed, damage)
→ 충돌 시 IDamageable.TakeDamage(damage)
```

- 스킬의 수치와 프리팹 참조는 `SkillData`에 둔다. 현재 공개 값은 `SkillId`, `DisplayName`, `Description`, `Cooldown`, `Damage`, `Range`, `ProjectileSpeed`, `SkillPrefab`이다.
- 새 주몽 스킬은 `ISkillBehaviour`를 구현하고 `SkillCastContext`로 캐스터·위치·방향·피해·속성·레이어를 받는다.
- 초기 유도 투사체 변형만 `SkillRunner.TryRun`을 재사용한다.
- `SkillRunner`는 **공격 오브젝트 생성과 초기화**만 담당한다. 대상 탐색이나 쿨다운 계산을 넣지 않는다.
- `TryRun`이 `true`를 반환한 뒤에만 발동 측 쿨다운을 갱신한다. 데이터·대상·프리팹·`Projectile`이 없어서 실패한 경우에는 쿨다운을 소비하지 않는다.
- `SkillRunner.firePoint`가 연결되어 있으면 그 위치에서, 없으면 `SkillRunner`가 붙은 오브젝트 위치에서 생성한다.
- 현재 `SkillRunner`가 실행하는 프리팹의 루트에는 `Projectile` 컴포넌트가 있어야 한다. 다른 실행 방식의 스킬을 추가할 때 기존 `TryRun`의 의미를 몰래 바꾸지 말고 실행 타입과 호환 방식을 먼저 설계한다.
- 주몽 SkillData 4개는 `ScriptableObjects/Skills/Jumong` 경로에 커밋되어 있으며 기존 `.meta` GUID와 프리팹 참조를 유지한다.

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

`Player.prefab` 루트에는 현재 `Rigidbody2D`, Collider, `PlayerInput`, `PlayerController`, `PlayerHealth`, `PlayerAimDirection`, `DirectionalAutoAttack`, `AutoSkillCaster`가 있다. 초기 유도형 `PlayerAutoAttack`은 비활성 상태로 보존돼 있고 `StageTest`는 씬 오버라이드로 `PlayerExperience`를 추가한다.

- `DirectionalAutoAttack.basicSkill`: 주몽 기본 화살 `SkillData`
- `DirectionalAutoAttack.enemyLayer`: `Enemy` 레이어
- `AutoSkillCaster.equippedSkills`: 테스트 시작 시 보유할 주몽 스킬 데이터
- `PlayerHealth.onDeath`: 사망 시 실행할 UnityEvent

`Enemy.prefab` 루트에는 현재 `Rigidbody2D`, Collider, `EnemyController`, `EnemyHealth`, `EnemyAttack`, `NetworkObject`, 서버 권한 `NetworkTransform`, `NetworkEnemyAuthoritySetup`이 있다. 로컬 `StageTest`에서는 기존 시뮬레이션을 유지하고, NGO 세션에서는 서버만 추적·공격·물리를 실행하며 클라이언트는 동기화된 위치를 표시한다.

- `EnemyHealth.experienceOrbPrefab`: 사망 시 생성할 `ExperienceOrb.prefab`
- `EnemyHealth.experienceReward`: 생성 직후 구체에 전달할 경험치
- `EnemyController.target`: 비어 있으면 런타임에 `Player` 태그를 다시 탐색

`Projectile.prefab`은 Trigger Collider와 `Projectile`이 필요하며, `ExperienceOrb.prefab`은 Trigger Collider와 `ExperienceOrb`가 같은 오브젝트에 있어야 한다. `PlayerExperience`가 충돌한 같은 오브젝트에서 `ExperienceOrb`를 찾기 때문이다.

`HealthPickup.prefab`과 `MagnetPickup.prefab`도 Trigger Collider와 해당 픽업 컴포넌트가 같은 오브젝트에 있다.

- `HealthPickup`은 `Player` 태그를 확인하고 같은 오브젝트의 `PlayerHealth.Heal`을 호출한다. 체력이 실제로 증가한 경우에만 픽업을 제거한다.
- `MagnetPickup`은 습득 시 현재 씬의 `ExperienceOrb`를 찾아 플레이어 Transform을 유도 목표로 지정한다.
- 두 픽업은 현재 `StageTest`에 수동 배치되어 있을 뿐, 적 드롭이나 라운드 스폰 규칙에는 아직 연결되지 않았다.

`StageTest.unity`에는 현재 일반 플레이어, 카메라, `EnemySpawner`, `WaveManager`, `GameManager`가 연결되어 있다.

- 현재 로컬 테스트 라운드: 10초, 2·3웨이브 시작 3초·4초
- 웨이브별 기본 수치: 2초/15마리, 1초/25마리, 0.5초/40마리
- 일반 적 스폰: 플레이어로부터 7~10 거리의 임의 방향
- 라운드 종료: 일반 적 생성을 중지하고 `GameManager`가 보스 프리팹 생성
- 현재 로컬 보스 참조: `BossJangsanTiger.prefab`

`Main.unity`에는 정적 플레이어·스테이지 오브젝트를 두지 않는다. `NetworkStageBootstrap`이 로비에서 넘어온 `NetworkPlayer`를 유지하고 60초·20초·40초 설정으로 스테이지 컴포넌트를 조립한다. 일반 적은 `Enemy.prefab`, 보스는 `BossJangsanTiger.prefab`을 참조한다.

### 4.7 현재 구현 시 주의점

- 일반 전투용 `Player.prefab`과 `NetworkPlayer.prefab`은 별도 프리팹이다. 네트워크 프리팹에는 충돌·체력·경험치·기본 공격·주몽 P0 스킬이 이관됐고 투사체·장판·속성·연계는 서버 판정/RPC 시각 복제 경로가 있다. 다만 스킬 오브젝트 자체의 상태 복제, 지연 보정과 실제 2인 회귀가 남았으므로 일반 전투 전체가 완전 동기화되었다고 가정하지 않는다.
- `MultiPlayerLobby`의 슬롯별 선택 ID는 서버가 각 `NetworkPlayerCharacter`의 `NetworkVariable`로 옮겨 씬 전환 후에도 보존한다. 모든 클라이언트는 같은 `CharacterData` 순서로 ID를 해석해 외형·체력·이동속도·기본공격·시작 스킬을 적용한다.
- `NetworkPlayer.prefab`의 `CharacterVisualRoot`는 `JumongData`의 `JumongVisual.prefab`을 런타임에 생성한다. 현재 선택지는 주몽 1종이며, 추가 캐릭터는 모든 클라이언트에서 동일한 `CharacterData` 목록 순서를 유지해야 한다.
- 일반 플레이어와 네트워크 플레이어 이동은 모두 Input Actions의 `OnMove` 콜백을 사용한다. `NetworkPlayerOwnerSetup`은 소유 플레이어에서만 입력·방향 추적·주몽 기본공격·자동 스킬·경험치 획득을 활성화하고 카메라 대상을 연결한다. 새 방향 공격이 있으면 기존 유도형 `PlayerAutoAttack`은 중복 실행하지 않는다.
- `NetworkPlayerHealth`는 서버 쓰기 체력·사망 상태를 모든 클라이언트에 반영하고 기존 `PlayerHealth.CurrentHealth`, `MaxHealth`, `IsDead`, `onDeath` 계약을 유지한다. 적 접촉 피해는 서버에서 적용하며 소유 플레이어의 회복 요청은 서버 RPC를 거친다. 회복 픽업 자체의 생성·제거와 전체 승패 상태 동기화는 후속 작업이다.
- `Enemy.prefab`은 기본 네트워크 프리팹 목록에 등록되어 있다. 네트워크 세션에서는 `EnemySpawner`와 적 이동·공격 시뮬레이션을 서버로 제한한다. 클라이언트 적은 Kinematic 충돌 대상으로 유지해 소유자 로컬 투사체가 명중 요청을 보낼 수 있고, 서버가 체력 감소와 제거를 최종 처리한다. 현재 요청은 피해량·명중 위치를 엄격히 재검증하지 않는 프로토타입 경계이며, 경험치 드롭과 투사체 자체의 네트워크 동기화는 후속 작업이다.
- `ExperienceOrb.prefab`도 기본 네트워크 프리팹 목록에 등록되어 있다. 네트워크 적 사망 시 서버가 구체를 생성하고, 소유 플레이어가 접촉하면 서버가 요청자의 플레이어 소유권과 거리를 확인한 뒤 `NetworkPlayerExperience`에 경험치를 반영하고 구체를 제거한다. 로컬 `PlayerTest`와 `StageTest`는 기존 `PlayerExperience` 경로를 유지한다. 자석으로 움직이는 구체 위치의 네트워크 동기화와 실제 2인 획득 회귀는 아직 후속 작업이다.
- `PlayerController`, `PlayerHealth`, `CameraFollow`만 각각 `Shura.Player`, `Shura.Camera` 네임스페이스에 있고 나머지 현재 스크립트 다수는 전역 네임스페이스다. 클래스 위치를 추측하지 말고 실제 선언을 확인한다.
- `EnemyController`는 `Start`와 `FixedUpdate`에서 플레이어를 찾는 현재 코드 흐름이 서로 다르므로 추적 로직을 수정할 때 두 경로를 함께 확인한다.
- `GameManager`는 현재 `PlayerHealth.IsDead`를 매 프레임 확인한다. `PlayerHealth.onDeath` 이벤트 기반으로 바꾸는 경우 관련 담당자와 공개 계약을 함께 갱신한다.
- `GameManager`의 보스 사망 판정은 생성된 GameObject가 제거되었는지를 본다. 네트워크 세션에서는 서버만 보스를 생성·판정하고 `NetworkGameResultState`에 승리를 전달한다.
- `NetworkGameResultActions`와 `NetworkPlayer.prefab`의 결과 후 로비 이름은 아직 `NetworkTest`다. 새 정식 흐름의 `MultiPlayerEntry` 또는 `MultiPlayerLobby`로 돌아가지 않으므로 두 로비 체계를 통합해야 한다.
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
- 로컬 씬에서는 `GameManager`가 `Time.timeScale`을 멈추고 현재 씬을 재로드한다.
- 네트워크 씬에서는 서버가 적·보스·승패를 확정하고 `NetworkGameResultState`가 참가자 결과를 복제한다. NGO 메시지와 버튼 입력을 위해 `Time.timeScale`은 멈추지 않는다.
- 짧은 세 웨이브는 개발 속도를 위한 테스트 값이다. 최종 15분 라운드는 `WaveData`, `StageConfig` 또는 동등한 공용 데이터 구조가 필요하다.

### 4.9 씬 책임

| 씬 | 책임 | 주의 |
|---|---|---|
| `MainMenu.unity` | 정식 첫 화면과 멀티 진입 | 멀티 버튼만 연결. 싱글·종료 버튼은 실행 이벤트 없음 |
| `MultiPlayerEntry.unity` | UGS·Relay 생성/참가, `NetworkManager` 생성 | 비활성 레거시 `NetworkTestUI` 오브젝트가 남아 있음 |
| `MultiPlayerLobby.unity` | 참가 코드·2슬롯·주몽 선택·호스트 시작·나가기 | 선택은 전투에 미적용, 시작 최소 인원 직렬화값 1 |
| `Main.unity` | 실제 네트워크 한 판 | 정적 Player를 추가하지 않음 |
| `CharacterSelect.unity` | 독립 캐릭터 선택 UI 초안 | Build Scene List 제외, 시작·뒤로 버튼 미연결, 싱글 흐름 없음 |
| `Tests/NetworkTest.unity` | 레거시 Relay 로비와 2인 회귀 | 정식 메뉴 흐름의 완료 판정에 사용하지 않음 |
| `Tests/StageTest.unity` | 네트워크 없는 스테이지·스킬·아이템 회귀 | 최종 게임 씬이나 Build 진입점이 아님 |
| `Tests/PlayerTest.unity` | 플레이어 단위 기능 | 통합 흐름 완료 판정에 사용하지 않음 |
| `Tests/CombatTest.unity` | 현재 구성이 불완전한 과거 테스트 씬 | 복구 전 정본 테스트로 사용하지 않음 |

## 5. 데이터 구조

게임 데이터는 ScriptableObject를 사용한다. 이것은 데이터베이스 ERD가 아니라 Unity 에디터에서 수정할 수 있는 설정 파일 구조다.

### 구현된 SkillData

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

현재 `SkillData`는 기본공격과 특수공격을 별도 타입으로 구분하지 않는다. 어떤 실행 컴포넌트가 이 데이터를 장착하는지와 `skillPrefab`의 `ISkillBehaviour` 구현으로 동작이 결정된다.

### 구현된 속성 런타임 데이터

```text
ElementType: None, Poison, Water, Lightning, Ice, Earth, Wind, Fire
AutoSkillCaster.EquippedSkill:
  data: SkillData
  element: ElementType
  nextCastTime: runtime only
```

속성은 `SkillData`에 고정하지 않는다. 습득 시점에 랜덤으로 부여되어 `AutoSkillCaster`가 보유 스킬별로 들고 `SkillCastContext.Element`로 전달한다. 관통 수·폭발 반경·장판 지속시간처럼 스킬 고유 특성은 프리팹 컴포넌트 설정값으로 둔다.

### 아직 구현되지 않은 데이터 후보

| 후보 | 목적 | 구현 전 확인 |
|---|---|---|
| `CharacterData` | 영웅 기본 능력·프리팹·초상화 | 영웅 2호와 선택 화면 범위 |
| `SynergyData` | 두 속성, 제한 시간, 효과, 내부 쿨다운 | 최소 연계 2개 서버 판정 구조 |
| `StageConfig` 또는 `WaveData` | StageTest와 Main이 공유할 웨이브·적·보스 값 | 15분 구간과 적 종류 |
| `SupportItemData` | 장기 보유 아이템 효과·속성 정책 | 현재 필드 픽업과 구분 |

파일이 생기기 전에는 위 이름을 구현된 API처럼 참조하지 않는다.

## 6. 공격 선택과 시너지 처리 흐름

### 레벨업 선택

1. `PlayerExperience`가 레벨 상승을 알린다.
2. `LevelUpChoiceGenerator`가 현재 레벨 구간을 확인한다.
3. 특정 구간이면 특수공격 또는 아이템 후보를, 그 외 구간이면 기본공격 강화 또는 능력치 후보를 만든다.
4. 공격형 후보에는 `AttributeRoller`가 허용된 속성 중 하나를 부여한다.
5. 플레이어 선택 결과를 `CharacterLoadout`에 적용하고 HUD를 갱신한다.

위 클래스들은 계획 이름이며 아직 구현되지 않았다. 현재는 EXP·레벨 수치만 존재한다.

### 속성 시너지

1. 기본공격·특수공격·아이템 효과가 적 또는 영역에 적중한다.
2. 공격의 속성, 플레이어 ID, 적용 시간을 `StatusEffectController`에 기록한다.
3. `SynergyResolver`가 기존 속성과 새 속성이 호환되는지 확인한다.
4. 제한 시간 안의 유효한 조합이면 시너지 효과를 실행한다.
5. 피해·제어·범위 확대와 시각·음향 피드백을 발생시킨다.
6. 사용한 속성을 소비하거나 내부 쿨다운을 적용한다.

핵심 판정은 한 곳에서만 수행한다. 각 공격 코드에 시너지 조합을 직접 작성하지 않는다.
`StraightProjectile`, `ElementalZone`, `JeoktomaDash`는 조합을 직접 알지 않고 `IElementReceiver`에 속성·서버가 확인한 플레이어 ID만 전달한다. `ElementalStatusController`가 서로 다른 플레이어와 제한 시간·내부 쿨다운을 확인하고 `SynergyResolver`가 감전·분쇄를 판정한다. 무작위 속성은 공격마다 다시 뽑지 않고 첫 서버 시전에서 스킬별 속성을 고정해 그 판 동안 유지한다.

## 7. 멀티플레이 권한 원칙

네트워크 솔루션과 관계없이 다음 원칙을 목표로 한다.

- 자신의 플레이어 이동은 해당 플레이어가 입력한다.
- 적 생성, 웨이브, 적 체력, 승패는 호스트가 최종 결정한다.
- 기본공격·특수공격·액티브 사용 요청은 소유 플레이어가 보내고 실제 피해 결과는 호스트가 확정한다.
- 레벨업 선택지와 무작위 속성 결과는 모든 클라이언트가 같은 결과를 보도록 호스트가 결정하거나 결과값을 동기화한다.
- 모든 프레임의 모든 정보를 보내지 않는다.
- 위치, 체력, 생성·사망, 공격 발동, 빌드 선택, 시너지 결과처럼 판정에 필요한 상태만 동기화한다.

### 네트워크 로비와 진입 장면

정식 씬 흐름은 다음 책임으로 분리되어 있다.

```text
MainMenu
→ MultiPlayerEntry: UGS 초기화·익명 로그인, Relay 방 생성/코드 참가
→ MultiPlayerLobby: 참가 코드·2슬롯 캐릭터 선택·호스트 시작
→ Main: 네트워크 한 판
```

- `MultiPlayerEntry`의 `NetworkManager`는 `UnityTransport`, NGO, `NetworkGameFlowController`, `NetworkSessionState`, `NetworkLobbySceneLoader`를 가진다.
- 호스트는 세션과 네트워크 준비 후 NGO SceneManager로 `MultiPlayerLobby`를 로드하고, 게스트는 호스트의 현재 씬에 동기화된다.
- `MultiPlayerLobby`의 씬 배치 `NetworkLobbyState`가 최대 2개 슬롯과 캐릭터 ID를 서버 권한으로 관리한다. 현재 선택 가능한 데이터는 주몽 1개다.
- `MultiplayerLobbyUI`가 런타임에 `NetworkGameFlowController`와 시작 버튼을 바인딩한다. 호스트이면서 2명이 접속한 경우에만 시작 버튼이 활성화되며 런타임에도 최소 인원을 2명 이상으로 보정한다.
- `MultiplayerLobbyExitController`는 세션과 `NetworkManager`를 종료하고 `MultiPlayerEntry`로 돌아간다.

`Tests/NetworkTest.unity`와 `NetworkTestUI`는 8월 2일 2인 검증에 사용된 레거시 경로다. 해당 경로의 Relay 외부 접속, `Main` 동시 전환과 `NetworkTest` 복귀 기록은 있지만 새 정식 4씬 흐름의 실행 증거로 대체할 수 없다. 새 흐름의 생성·참가·선택·시작·나가기·결과 후 복귀, 강제 이탈·새 방·Web은 별도 회귀가 필요하다.

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
| `MainMenu.unity` | Build Scene List의 첫 활성 씬. 멀티 진입만 연결 |
| `MultiPlayerEntry.unity` | 정식 Relay 방 생성·코드 참가와 `NetworkManager` 소유 |
| `MultiPlayerLobby.unity` | 네트워크 캐릭터 선택·접속 인원·호스트 시작 |
| `Main.unity` | 실제 네트워크 게임. 런타임 스테이지·HUD·결과 사용 |
| `CharacterSelect.unity` | 싱글용으로 보이는 독립 선택 UI 초안. Build Scene List 제외 |
| `Tests/PlayerTest.unity` | 플레이어 기능 |
| `Tests/CombatTest.unity` | 과거 전투 테스트. 현재 구성 복구 필요 |
| `Tests/NetworkTest.unity` | 레거시 Relay 로비와 회귀 비교 |
| `Tests/StageTest.unity` | 로컬 스폰·웨이브·보스·승패·스킬·픽업 통합 검증 |

Build Scene List에는 `MainMenu`, `MultiPlayerEntry`, `MultiPlayerLobby`, `Main`, 레거시 `NetworkTest`가 활성화되어 있다. `SampleScene`은 비활성이고 `CharacterSelect`, `PlayerTest`, `CombatTest`, `StageTest`는 목록에 없다. 별도 `Boot`, `Result`, `ContentTest` 씬은 없다.

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

## 13. 네트워크 게임 결과 상태

- `NetworkPlayerHealth`의 사망 상태는 서버만 변경하고 모든 클라이언트에 복제한다.
- `NetworkGameResultState`는 서버에서 복제된 플레이어 사망을 감지해 참가자 전원의 결과를 `Defeat`로 동기화한다.
- 보스 처치처럼 서버가 승리를 확정한 시스템은 `SetVictoryServer()`를 호출해 동일한 결과 흐름을 사용한다.
- 결과 UI는 이 상태를 구독하며 재시작과 로비 복귀는 호스트 권한으로 실행한다.

### 네트워크 결과 표시

- `NetworkGameResultPresenter`는 소유 플레이어에서만 결과 UI를 생성해 화면마다 오버레이가 하나만 존재하도록 한다.
- 결과 패널은 화면 중앙 앵커와 `CanvasScaler`를 사용해 창 크기와 화면 비율이 달라도 중앙 배치를 유지한다.
- 로비에서 생성된 플레이어가 `Main` 씬으로 이동해도 표시가 유지되도록 결과 Canvas를 씬 전환에서 보존한다.
- `Playing` 동안 패널을 숨기고 `Victory` 또는 `Defeat`가 복제되었을 때만 표시한다.

### 결과 이후 전환

- 결과 패널의 `다시 시작`과 `로비로` 버튼은 호스트 화면에만 표시한다.
- 다시 시작은 NGO SceneManager로 `Main` 씬을 전원에게 다시 로드하고 결과·체력·사망·경험치·레벨 상태를 초기화한다.
- 로비 복귀는 서버 RPC로 모든 참가자에게 종료를 알린 뒤 네트워크를 닫고 현재 직렬화된 `NetworkTest` 씬을 새로 연다. 정식 `MultiPlayerEntry` 흐름과 아직 통합되지 않았다.
- 결과 UI Canvas에는 `GraphicRaycaster`를 추가하고, 씬 전환 후 EventSystem이 없을 때 Input System용 EventSystem을 생성한다.

### 네트워크 보스 승리

- 네트워크 세션에서 `GameManager`의 보스 생성은 서버만 수행하며 생성 직후 `NetworkObject.Spawn()`으로 참가자에게 복제한다.
- 클라이언트는 보스를 별도로 생성하거나 제거 여부를 판정하지 않는다.
- 서버가 보스 NetworkObject의 제거를 확인하면 `NetworkGameResultState.SetVictoryServer()`로 참가자 전원의 결과를 `Victory`로 변경한다.
- 네트워크 결과에서는 `Time.timeScale`을 멈추지 않아 NGO 메시지와 결과 UI 버튼 입력이 계속 처리되도록 한다.

### Main 네트워크 스테이지 통합

- `Main` 씬의 `NetworkStageBootstrap`이 60초 테스트 라운드용 `EnemySpawner`, `WaveManager`, `GameManager`를 런타임에 구성한다.
- 일반 적은 `Enemy.prefab`, 보스는 네트워크 프리팹 목록에 등록된 `BossJangsanTiger.prefab`을 사용하며 실제 생성은 서버 권한 컴포넌트가 담당한다.
- 로비에서 이미 생성된 소유 플레이어는 `Main` 진입 시 다시 Spawn되지 않으므로 부트스트랩이 새 `CameraFollow`에 로컬 플레이어를 재연결한다.
- 런타임 설정 API를 통해 `StageTest`의 직렬화 설정은 보존하고 `Main`만 별도 라운드 설정을 사용한다.

### Main 네트워크 HUD

- `NetworkPlayerHudPresenter`는 소유 플레이어에서만 HUD Canvas를 생성해 각 화면에 자신의 상태만 표시한다.
- HUD는 `Main` 씬에서만 활성화되며 로비와 결과 이후 로비 복귀 화면에는 표시하지 않는다.
- 좌측 상단 앵커와 `CanvasScaler`를 사용해 창 크기와 화면 비율 변경에도 일정한 여백을 유지한다.
- HP는 서버 동기화 체력, 레벨과 EXP는 서버 동기화 성장 상태가 반영된 로컬 플레이어 컴포넌트에서 읽는다.
