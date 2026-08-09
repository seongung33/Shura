# Shura AI 활용 기술 문서

> 마지막 정리: **2026-08-09 KST**
> 이 문서는 AI 활용과 사람의 판단·검증 기록만 관리한다. 현재 구현 상태는 `12`, 코드 구조는 `04`, 테스트 결과는 `10`을 따른다.
> 기록 원칙: AI의 기여와 팀원의 판단·통합·검증을 구분하고, 확인되지 않은 도구명·프롬프트·테스트를 만들어내지 않는다.

## 1. 문서 목적

이 문서는 NAN 2026 제출 자료를 위해 프로젝트에서 사용한 생성형 AI 도구, 요청 목적, 산출물, 팀원의 검토·Unity 통합·테스트와 한계를 기록한다.

팀 확인 기준으로 현재까지 작성된 C# 구현은 생성형 AI 코딩 도구가 제시한 초안을 기반으로 진행되었다. 이 사실을 숨기거나 사람이 모든 코드를 직접 타이핑한 것처럼 표현하지 않는다. 동시에 “AI가 전부 만들었고 팀은 한 일이 없다”는 식으로도 축약하지 않는다.

Shura의 개발 과정에서 팀원이 맡은 일은 다음과 같다.

- 게임의 핵심 경험, 기능 우선순위와 완료 조건 결정
- 기존 코드와 프리팹 구조를 AI에 제공하고 변경 범위·금지 사항 정의
- 여러 구현안 중 프로젝트에 맞는 안을 선택하고 공개 API·코드 계약 검토
- Unity 씬, GameObject, Collider, Rigidbody2D, 레이어·태그, Input Action과 Inspector 참조 연결
- 프리팹, ScriptableObject와 테스트 씬 제작·수치 조정
- Play Mode, Windows 빌드와 Relay 환경에서 실제 동작 확인
- 오류 재현, 수정 방향 결정, 브랜치 통합, Git 커밋·PR과 최종 채택

따라서 AI는 구현 속도를 높이는 **개발 도구**이고, 기획 의도·품질 기준·통합과 결과에 대한 책임은 팀에 있다.

## 2. AI와 팀원의 역할 구분

| 단계 | AI 활용 | 팀원이 수행·결정한 일 |
|---|---|---|
| 문제 정의 | 요구사항 정리 보조, 누락 질문 제안 | 플레이 목표, 범위, 우선순위, 완료 조건 확정 |
| 구조 설계 | 클래스·데이터 흐름과 대안 제안 | 기존 코드 계약과 팀 역량에 맞는 구조 선택 |
| 구현 | C# 초안·수정안·에디터 작업 순서 생성 | 채택 여부 판단, 프로젝트 파일·씬·프리팹·데이터에 통합 |
| 오류 대응 | 로그 해석과 원인 후보·수정안 제안 | 실제 재현, 원인 범위 축소, 수정 선택과 회귀 범위 결정 |
| 검증 | 테스트 항목과 체크리스트 제안 | Unity Play Mode, 빌드, 두 인스턴스·두 PC 테스트 수행 |
| 문서화 | 초안·표·요약 생성 | 사실 대조, 계획과 구현 구분, 역할·결정·한계 최종 확정 |
| 버전 관리 | 명령·커밋 메시지·PR 설명 보조 | 브랜치 운용, 충돌 해결, 커밋·PR·병합 승인 |

AI가 제안한 코드를 수정 없이 채택한 작업도 있다. 이 경우 “사람이 코드를 수정했다”고 꾸미지 않고, 팀원이 수행한 요구사항 정의, 구조 확인, Unity 연결, Play 검증과 채택 판단을 기록한다.

## 3. 사용한 도구와 프로젝트 내 AI

### 개발 과정에서 확인된 도구

- **ChatGPT/Codex:** 초기 프로젝트 문서와 구조 정리, 2026-07-31 저장소·문서 감사와 갱신
- **Claude Code:** 플레이어 이동, 카메라 추적, 체력·사망, 경험치·레벨업, HUD 작업의 상세 기록이 Git 이력에 남아 있음
- **생성형 AI 코딩 도구(세부 도구 미기록):** 전투·스킬 기반, Relay, 맵·웨이브·게임 종료, 아이템 코드 초안. 팀 진술로 AI 활용은 확인되지만 당시 도구명과 원문 프롬프트는 기록 보완이 필요함

### 런타임 AI 사용 여부

`Packages/manifest.json`에는 Unity Assistant와 Unity AI Inference 패키지가 설치되어 있다. 그러나 2026년 7월 31일 코드 감사에서는 AI 동료, AI 디렉터, 생성 모델 추론 등 **게임 플레이 중 실행되는 AI 기능은 확인되지 않았다.**

개발 도구로 AI를 활용한 것과 게임 기능으로 AI를 탑재한 것을 구분한다.

## 4. 표준 활용 흐름

```text
팀원이 목표·기존 구조·완료 조건 정의
→ 관련 코드·프리팹·씬과 수정 금지 범위를 AI에 제공
→ AI가 구현 초안·설명·테스트 절차 제안
→ 팀원이 기존 계약·프로젝트 컨벤션과 비교해 채택 여부 결정
→ Unity 에디터에서 오브젝트·프리팹·데이터·참조 연결
→ 컴파일과 Play Mode·빌드·네트워크 검증
→ 발견한 문제를 수정·재검증
→ 기능 브랜치 커밋·PR
→ AI 활용과 사람의 검토·통합 내역 기록
```

## 5. 프롬프트 작성 기준

AI에 코드만 요청하지 않고 다음 맥락을 함께 제공한다.

- Unity 버전, 렌더링·입력·네트워크 패키지
- 실제 관련 파일과 현재 브랜치 상태
- 만들려는 기능과 플레이어가 보게 될 결과
- 수정 가능한 파일과 수정하면 안 되는 영역
- 기존 공개 API, 레이어·태그·Collider·프리팹 계약
- 네트워크 권한과 데이터 동기화 기준
- 완료 조건과 재현 가능한 테스트 방법
- 불확실한 항목은 임의로 확정하지 말고 보고하라는 지시

### 코드 요청 템플릿

```text
Unity 6000.3.20f1 Universal 2D 프로젝트다.

목표:
[구현할 기능과 플레이 결과]

현재 구조:
[관련 스크립트·프리팹·씬·데이터]

제약:
- 먼저 docs/Project/04_TECHNICAL_DESIGN.md와 실제 파일을 확인한다.
- 데미지는 구체 Health 클래스를 직접 호출하지 않고 IDamageable.TakeDamage로 전달한다.
- 현재 투사체형 공격은 SkillData → SkillRunner → Projectile 흐름을 우선 재사용한다.
- Enemy 레이어, Player 태그, Collider·컴포넌트 계층 계약을 유지한다.
- 문서에만 있는 계획 클래스를 구현 완료로 가정하지 않는다.
- 기존 공개 API를 불필요하게 바꾸거나 병렬 시스템을 만들지 않는다.
- 외부 패키지를 임의로 추가하지 않는다.
- 수정 파일과 이유를 먼저 설명한다.

완료 조건:
- Unity 컴파일 오류가 없다.
- [테스트 씬]에서 [구체 동작]을 재현한다.
- 정상·실패·회귀 테스트 방법을 보고한다.
```

### 버그 분석 요청 템플릿

```text
기대 동작:
[정상 결과]

실제 동작:
[문제]

재현 순서와 환경:
[브랜치·커밋·씬·단계]

Console 로그:
[오류]

관련 파일:
[경로]

먼저 원인을 진단하고 근거 없이 여러 파일을 동시에 수정하지 않는다.
수정 후 재현 테스트와 영향받는 기존 기능의 회귀 테스트를 작성한다.
```

## 6. 실제 AI 활용 내역

아래 기록은 현재 문서, Git 커밋 이력과 팀이 제공한 AI 활용 사실을 대조해 복원했다. 당시 상세 기록이 없는 행은 그 한계를 명시했다.

| 날짜 | 도구 | 담당 | 목적·요청 요약 | AI 산출물 | 팀원의 검토·통합·검증 | 근거·상태 |
|---|---|---|---|---|---|---|
| 2026-07-25 | ChatGPT/Codex | 팀(세부 담당 보완 필요) | 2D 협동 생존 게임의 폴더, GDD, MVP, 일정, 위험과 제출 문서 구성 | 초기 README·프로젝트 문서 초안 | 팀 상황에 맞춰 2인 MVP, 온라인 폴백, 역할과 마감 범위를 검토·확정 | `3a1757e`; 실제 담당자명 보완 필요 |
| 2026-07-26 | 생성형 AI 코딩 도구(세부 미기록) | 문성웅 | 적 추적·접촉 공격·체력, 자동 공격, 투사체, 경험치 드롭과 공통 피해 구조 | 초기 전투 C# 구현 초안 | 적·플레이어·투사체 프리팹, `Enemy` 레이어와 `Player` 태그, Collider와 테스트 씬을 연결하고 기능별 브랜치로 통합 | `a1246f6`, `e3437d8`; 원문 프롬프트·도구명 보완 필요 |
| 2026-07-26 | 생성형 AI 코딩 도구(세부 미기록) | 문성웅 | 스킬 수치를 데이터화하고 최근접 적을 추적·재탐색하는 기본 공격 구성 | `SkillData`, `SkillRunner`, `EnemyTargetFinder`, 유도 `Projectile` 초안 | `HomingShotData`와 `Projectile.prefab`을 연결하고, 중복 자동공격 파일을 정리해 현재 실행 계약을 확정 | `f73c46f`, `abeea10`; 세부 기록 보완 필요 |
| 2026-07-26 | Claude Code | 진미리 | Input System 기반 WASD 이동, 대각선 정규화, Inspector 속도 조정과 `Player.prefab` 구성 | `PlayerController.cs`와 Unity 에디터 구성 가이드 | 구현을 검토해 채택하고 씬·Rigidbody2D·Collider2D·Player Input을 직접 구성했다. 저장 경로를 `Prefabs/Players` 컨벤션에 맞추고 Play Mode에서 이동을 확인 | `3408a2b`, `b27ada9`, `0fbd494`, PR #7 |
| 2026-07-26 | Claude Code | 진미리 | `LateUpdate`와 보간을 이용한 플레이어 카메라 추적 | `CameraFollow.cs`와 컴포넌트 연결 가이드 | 카메라에 컴포넌트·대상을 연결하고 Play Mode에서 Transform 변화와 추적을 확인 | `2b0b39a`, `dfbaf61`, PR #10 |
| 2026-07-26~27 | 생성형 AI 코딩 도구(세부 미기록) | 문성웅(초기 구현), 진미리(현재 통합 담당) | NGO·Unity Multiplayer Services로 2인 Relay 방 생성·코드 참가·이동·퇴장 흐름 구성 | 네트워크 테스트 C#과 UI 이벤트 처리 초안 | UGS·NetworkManager·UI·네트워크 프리팹을 연결하고 Windows 테스트 빌드에서 외부 참가와 정상 퇴장 흐름을 확인했다. 강제 종료 후 재접속은 제한으로 남김 | `a5a9c30`, `c215d77`, `46a32f6`, `f83feae`; 도구·프롬프트 보완 필요 |
| 2026-07-27 | Claude Code | 진미리 | 체력 감소, `IDamageable`, 사망 상태·UnityEvent와 임시 피해 테스트 | `PlayerHealth.cs`, `PlayerHealthDebugTester.cs` | Inspector에서 상태값이 보이지 않는 문제를 찾아 직렬화 필드를 보완하고, Play Mode에서 피해와 0 체력 사망을 확인 | `897b94c`, `c393f7e`, PR #16 |
| 2026-07-27 | Claude Code | 진미리 | 경험치 구체 습득, 누적, 임계값 증가와 잉여 경험치 보존 레벨업 | `PlayerExperience.cs` | 기존 팀의 `PlayerHealth`·네임스페이스 구조와 충돌하지 않도록 방향을 조정하고 ContextMenu와 Play Mode로 레벨 증가를 확인 | `106001d`, `cd9bc72`, PR #15 |
| 2026-07-28 | Claude Code | 진미리 | HP·레벨·EXP HUD와 사망 결과 패널 | `HUDController.cs`와 Canvas·TMP·Panel 구성 가이드 | UI 배치·Canvas Scaler·폰트 크기와 여백을 에디터에서 조정하고 Play Mode로 갱신을 확인했다. 이후 네임스페이스·사망 판정 문제를 별도 수정 브랜치에서 보완 | `c3446a2`, `b3efab0`, `a235369`; 현재 맵 브랜치 미통합 |
| 2026-07-30 | 생성형 AI 코딩 도구(세부 미기록) | 문성웅 | 플레이어 주변 적 생성, 60초 3웨이브, 라운드 종료, 임시 보스, 승패와 재시작 | `EnemySpawner`, `WaveManager`, `GameManager` 초안 | `StageTest`와 프리팹 참조를 연결하고 스폰 거리·간격·최대 생존 수를 입력했다. Unity 로그에서 종료·보스 등장·승패 실행 흔적은 확인되며, 현재 웨이브 시작값과 전용 보스는 추가 검증 대상 | `f47b592`, `784607a`; 도구·원문 프롬프트 보완 필요 |
| 2026-07-30~31 | 생성형 AI 코딩 도구(세부 미기록) | 문성웅 | 체력 회복 픽업, 전체 경험치 오브 자석 효과와 오브 유도 이동 | `PlayerHealth.Heal`, `HealthPickup`, `MagnetPickup`, `ExperienceOrb.AttractTo` 초안 | `ExperienceOrb`의 `.meta`를 유지해 폴더를 옮기고 픽업 프리팹·Trigger·`StageTest` 위치를 연결했다. Unity 로그에 컴파일·회복 동작 흔적이 있으나 정식 회귀 테스트는 남아 있음 | 당시 로컬 작업이었으나 현재 파일은 커밋됨; 도구·프롬프트·회귀 증거 보완 필요 |
| 2026-07-31 | Codex | 문서 갱신 담당 | 코드는 수정하지 않고 저장소·브랜치·Git 이력·프리팹·씬·문서를 대조해 현재 상태와 AI 활용 내역 갱신 | 현황표, 기술 계약, 일정, 테스트·출처·AI 기록 정리 | 완료·부분·원격·로컬 상태를 구분하도록 요구했고, 과장 없이 AI를 도구로 활용한 사람의 역할을 드러내도록 최종 범위를 정함 | 본 문서 갱신; 최종 팀 검토 필요 |
| 날짜 | 도구 | 담당자 | 목적 | 주요 프롬프트 요약 | 생성 결과 | 사람의 검토·수정 | 관련 파일·커밋 |
|---|---|---|---|---|---|---|---|
| 2026-07-25 | ChatGPT/Codex | TBD | 프로젝트 초기 구조와 문서 작성 | Unity 2D 협동 생존 게임의 폴더·문서·일정 구성 | 프로젝트 문서 초안 | 팀 상황에 맞춰 MVP, 역할, 폴백 범위 검토 | 초기 문서 커밋 |
| 2026-07-26 | Claude Code | 미리 (개발자 A) | 플레이어 기본 이동 구현 (feat/player-movement) | PlayerTest 테스트 씬 생성, Rigidbody2D/Collider2D 기반 WASD 이동, Input System(Player Input, Send Messages)으로 구현, 속도 Inspector 노출, 대각선 이동 정규화, Player.prefab 저장 | `PlayerController.cs` 전체 코드 작성 + Unity 에디터 내 GameObject/컴포넌트 구성, 씬 생성, PR 작성 단계별 가이드 | 코드는 그대로 채택. Play 모드에서 WASD 이동·대각선 속도 동일 여부·Inspector 속도 변경·다른 씬에서의 Prefab 동작을 직접 테스트로 확인. Prefab 저장 경로는 지시받은 `Prefabs/Player` 대신 기존 컨벤션인 `Prefabs/Players`로 조정 | `Assets/_Project/Scripts/Player/PlayerController.cs`, 커밋 3408a2b·b27ada9, PR #7 |
| 2026-07-26 | Claude Code | 미리 (개발자 A) | 카메라 플레이어 추적 구현 (feat/camera-follow) | Main Camera가 LateUpdate에서 Vector3.Lerp로 Player를 부드럽게 추적, followSpeed·offset Inspector 노출 | `CameraFollow.cs` 전체 코드 작성 + Unity 에디터 내 컴포넌트 연결 가이드 | 코드는 그대로 채택. Play 모드에서 Main Camera Transform 좌표 변화로 실제 추적 동작 확인 | `Assets/_Project/Scripts/Camera/CameraFollow.cs`, 커밋 2b0b39a |
| 2026-07-28 | Claude Code | 미리 (개발자 A) | 플레이어 HUD 및 결과 화면 구현 (feat/player-hud) | Canvas 기반 체력/레벨·경험치 텍스트 표시, PlayerHealth 참조가 null이 되면(사망) 결과 패널 자동 표시 | `HUDController.cs` 작성 + Unity 에디터에서 Canvas/TextMeshPro/Panel 구성 단계별 가이드 | 코드는 그대로 채택. Play 모드에서 데미지 시 HP 텍스트 실시간 갱신, 사망 시 Game Over 패널 표시 확인. UI 배치(텍스트 겹침, Canvas Scaler 모드, 폰트 크기/여백)는 여러 차례 시행착오 끝에 사람이 직접 값 조정 | `Assets/_Project/Scripts/UI/HUDController.cs`, 커밋 c3446a2 |
| 2026-07-30 | Codex | 진미리 | 일반 플레이어와 네트워크 플레이어 구조 통합 1단계 | 진미리 담당 문서와 전체 코드·프리팹 상태를 대조하고, 이미 별도 브랜치에 구현된 HUD 수정은 제외한 뒤 네트워크 플레이어에 Input Actions·충돌·체력·경험치·기본 공격·소유자 카메라 연결을 통합 | `NetworkPlayerOwnerSetup.cs` 추가, `NetworkPlayerMovement` 입력 표준화, `NetworkPlayer.prefab` 구성 통합, `CameraFollow` 런타임 대상 설정 API 추가 | 소유자가 아닌 플레이어의 입력·자동 공격·경험치 획득을 비활성화하도록 검토. 체력·경험치·공격 결과의 네트워크 동기화와 실제 Main 씬 전환은 후속 작업으로 명시 | `Assets/_Project/Scripts/Network/Player/NetworkPlayerOwnerSetup.cs`, `Assets/_Project/Prefabs/Players/NetworkPlayer.prefab` |
| 2026-07-30 | Codex | 진미리 | 멀티플레이 게임 시작 조건과 씬 전환 게이트 | 실제 게임용 Main 씬이 아직 없는 상태에서 잘못된 자동 전환을 만들지 않고, NGO 접속 인원과 호스트 권한을 기준으로 안전하게 씬 전환을 시작하는 구조 구현 | `NetworkGameFlowController.cs` 작성 및 `NetworkTest.unity`의 NetworkManager에 연결 | 최소 2명, 호스트 권한, 씬 이름, Build Settings 포함 여부를 모두 검증하도록 검토. 실제 Main 씬 이름과 UI 버튼 연결은 씬 제작 후 수행하도록 제한 기록 | `Assets/_Project/Scripts/Network/Lobby/NetworkGameFlowController.cs`, `Assets/_Project/Scenes/Tests/NetworkTest.unity` |
| 2026-07-30 | Codex | 진미리 | Relay 세션 실패·퇴장·재시도 수명주기 보강 | 방 생성·참가 중 예외가 발생한 뒤 세션 참조가 남아 재시도가 막히는 경로와 이벤트 구독 해제 시점을 점검 | 세션 연결·해제를 `AttachSession`·`DetachSession`으로 통일하고 실패 세션 정리, 참가 코드 초기화, 서비스 초기화 재시도, 파괴 후 콜백 방지 추가 | 정상 퇴장 실패 시 재시도할 수 있도록 현재 세션을 유지하고, 생성·참가 실패 시에만 부분 세션을 정리하도록 검토 | `Assets/_Project/Scripts/Network/Lobby/NetworkTestUI.cs` |
| 2026-07-31 | Codex | 진미리 | 네트워크 입장 이후 게임용 Main 씬 기반 연결 | 카메라만 있는 빈 CombatTest 구성을 확인한 뒤 정적 플레이어 중복 없이 게임 통합용 Main 씬을 생성하고 기존 시작 게이트와 Build Settings에 연결 | Unity Editor API로 `Main.unity` 생성, `NetworkGameFlowController.gameplaySceneName` 설정, Build Settings 등록 | Unity 6000.3.20f1 배치 모드에서 전체 에셋 임포트와 스크립트 컴파일 성공 확인. Main의 실제 전투 콘텐츠와 시작 버튼 연결은 후속 작업으로 분리 | `Assets/_Project/Scenes/Main.unity`, `Assets/_Project/Scenes/Tests/NetworkTest.unity`, `ProjectSettings/EditorBuildSettings.asset` |
| 2026-07-31 | Codex | 진미리 | 멀티 방 접속 인원과 호스트 게임 시작 UI | 기존 방 UI에 실제 시작 조건을 표시하고 잘못된 클라이언트 시작 요청을 UI 단계에서도 막는 연결 구현 | Unity Editor API로 `게임 시작` 버튼과 `접속 인원` 텍스트 생성, 버튼 `OnClick`과 컨트롤러 연결, 호스트·2인 조건에 따른 활성화 갱신 | 생성된 씬 직렬화 참조와 버튼 이벤트 대상을 확인하고 Unity에서 2인 Relay 접속 후 양쪽 `Main` 씬 전환을 검증. Canvas 해상도 대응과 접속 인원 배치를 추가 조정 | `Assets/_Project/Scripts/Network/Lobby/NetworkGameFlowController.cs`, `Assets/_Project/Scenes/Tests/NetworkTest.unity` |
| 2026-08-01 | Codex | 진미리 | 주몽 기본공격·자동 스킬의 네트워크 플레이어 구성 이관 | 일반 플레이어에 새로 머지된 방향 기본공격과 자동 스킬 구성을 네트워크 프리팹에 옮기고 비소유 캐릭터의 로컬 실행을 차단 | `NetworkPlayer.prefab`에 `PlayerAimDirection`, `DirectionalAutoAttack`, `AutoSkillCaster`와 SkillData 참조 추가, `NetworkPlayerOwnerSetup` 소유권 제어 확장 | Unity 6000.3.20f1 배치 모드 스크립트 컴파일과 프리팹 직렬화 참조를 확인. 이 단계는 소유자 로컬 실행 연결이며 투사체·피해·속성의 네트워크 동기화는 후속 작업으로 제한 | `Assets/_Project/Prefabs/Players/NetworkPlayer.prefab`, `Assets/_Project/Scripts/Network/Player/NetworkPlayerOwnerSetup.cs` |
| 2026-08-01 | Codex | 진미리 | 적 생성과 이동의 서버 권한 네트워크 기반 연결 | 로컬 `StageTest` 동작은 유지하면서 NGO 세션에서는 호스트만 적을 생성·시뮬레이션하고 클라이언트에는 위치를 동기화 | `EnemySpawner` 서버 권한 분기, `Enemy.prefab`의 `NetworkObject`·서버 권한 `NetworkTransform`·`NetworkEnemyAuthoritySetup`, 기본 네트워크 프리팹 등록 | Unity 6000.3.20f1 배치 모드에서 전체 에셋 임포트와 스크립트 컴파일 성공(종료 코드 0). 적 피해·사망·경험치 드롭과 투사체 판정 동기화는 후속 범위로 명시 | `Assets/_Project/Scripts/Stage/EnemySpawner.cs`, `Assets/_Project/Scripts/Network/Enemy/NetworkEnemyAuthoritySetup.cs`, `Assets/_Project/Prefabs/Enemy/Enemy.prefab` |
| 2026-08-01 | Codex | 진미리 | 적 체력·피해·사망의 서버 권한 동기화 | 게스트의 로컬 투사체 명중 요청을 서버가 적용하고 두 클라이언트에서 동일한 체력·적 제거 결과를 보도록 기반 연결 | `EnemyHealth`를 `NetworkBehaviour`로 전환해 서버 쓰기 `NetworkVariable` 체력과 서버 대상 피해 RPC·서버 `Despawn` 추가, 클라이언트 적을 Kinematic 충돌 대상으로 유지 | Unity 6000.3.20f1 배치 모드에서 NGO RPC 코드 생성과 전체 스크립트 컴파일 성공(종료 코드 0). 실제 2인 명중 회귀, 피해 요청 검증 강화, 경험치 드롭 동기화는 후속 범위 | `Assets/_Project/Scripts/Enemy/EnemyHealth.cs`, `Assets/_Project/Scripts/Network/Enemy/NetworkEnemyAuthoritySetup.cs` |
| 2026-08-01 | Codex | 진미리 | 경험치 구체 생성·획득·성장 수치 네트워크 동기화 | 네트워크 적 사망 후 구체를 양쪽에 표시하고 소유 플레이어의 접촉을 서버가 검증해 경험치와 레벨 수치를 동일하게 유지 | `ExperienceOrb` 네트워크 프리팹·서버 획득 RPC, `NetworkPlayerExperience` 서버 쓰기 성장 수치, `EnemyHealth` 서버 구체 생성, 기존 로컬 `PlayerExperience` 상태 적용 경로 | Unity 6000.3.20f1 배치 모드에서 RPC 코드 생성, 네트워크 프리팹 3종 임포트와 전체 스크립트 컴파일 성공(종료 코드 0). 실제 2인 획득과 자석 이동 동기화는 후속 검증 | `Assets/_Project/Scripts/Item/ExperienceOrb.cs`, `Assets/_Project/Scripts/Network/Player/NetworkPlayerExperience.cs`, `Assets/_Project/Scripts/Player/PlayerExperience.cs` |
| 2026-08-01 | Codex | 진미리 | 플레이어 체력·회복·사망 상태의 서버 권한 동기화 | 기존 로컬 `PlayerHealth` API와 테스트 씬을 유지하면서 네트워크 플레이어 체력과 사망 이벤트를 양쪽에 동일하게 반영 | `NetworkPlayerHealth` 서버 쓰기 체력·사망 상태와 소유자 회복 RPC 추가, `PlayerHealth`에 네트워크 상태 적용 경로 추가, `NetworkPlayer.prefab` 연결 | Unity 6000.3.20f1 배치 모드에서 RPC 코드 생성, NetworkPlayer 프리팹 임포트와 전체 스크립트 컴파일 성공(종료 코드 0). 실제 2인 피해·회복·사망 회귀와 승패 동기화는 후속 검증 | `Assets/_Project/Scripts/Network/Player/NetworkPlayerHealth.cs`, `Assets/_Project/Scripts/Player/PlayerHealth.cs`, `Assets/_Project/Prefabs/Players/NetworkPlayer.prefab` |
| 2026-08-02 | Codex | 진미리 | 멀티 로비에서 Main 전환 후 플레이어가 사라지는 문제와 로비 UX 점검 | 2인 Relay 실기기 검증에서 빈 Main 화면, Player Prefab 누락 로그, 참가자에게 불필요한 시작 버튼과 참가 후에도 남는 로비 조작을 확인하고 원인 분석·수정 | `NetworkPlayer.prefab` 중복 fileID 복구, 씬 전환 시 PlayerObject 보존과 누락 플레이어 서버 재생성, 세션 상태 기반 로비 버튼·입력 필드 전환, 게스트 호스트 대기 안내, 적 초기 타깃 탐색 조건 수정 | 프리팹 component 참조 22개 모두 정의 존재·중복 0 확인, `git diff --check` 통과, Unity 6000.3.20f1 Windows 빌드 성공. 같은 빌드의 Editor·실행 파일로 2인 Relay 참가, Main 동시 전환, 플레이어·적·HUD·웨이브·패배 결과와 양쪽 로비 복귀까지 확인 | `Assets/_Project/Prefabs/Players/NetworkPlayer.prefab`, `Assets/_Project/Scenes/Main.unity`, `Assets/_Project/Scenes/Tests/NetworkTest.unity`, `Assets/_Project/Scripts/Network`, `Assets/_Project/Scripts/Enemy/EnemyController.cs` |
| 2026-08-05 | Codex | 진미리 | 기본·폭발 화살의 클라이언트 중복 피해 요청을 제거하고 상대 화면에 같은 투사체를 표시 | 소유자가 스킬 인덱스·발사 위치·방향·속성을 서버에 요청하고, 서버가 프리팹·쿨다운·위치를 검증해 실제 피해 투사체를 생성하며 비서버 화면에는 피해 없는 시각 복제본만 전달하는 중계 구조 | `NetworkSkillCastRelay`, `SkillCastContext.VisualOnly`, `StraightProjectile` 피해 권한 분리와 `NetworkPlayer.prefab` 허용 스킬 목록 연결 | 프리팹 component 정의 누락·중복 0, `git diff --check`, Unity 6000.3.20f1 NGO RPC 코드 생성·Windows 빌드·올바른 프로젝트 간 2인 Relay 게임 진입 성공. 배치 검증기로 서버 투사체 HP 100→90 1회, 시각 복제본 HP 100 유지와 종료 코드 0 확인 | `Assets/_Project/Scripts/Network/Player/NetworkSkillCastRelay.cs`, `Assets/_Project/Scripts/Combat/StraightProjectile.cs`, `Assets/_Project/Editor/NetworkProjectileAuthorityValidator.cs`, `Assets/_Project/Prefabs/Players/NetworkPlayer.prefab` |
| 2026-08-09 | Codex | 진미리 | 로비 캐릭터 선택값을 Main 네트워크 플레이어 전투 구성에 연결 | 씬 오브젝트에만 있던 선택 ID를 서버가 플레이어별 `NetworkVariable`로 옮겨 씬 전환 후에도 보존하고, 모든 클라이언트가 같은 `CharacterData`를 적용 | `NetworkPlayerCharacter`, 주몽 전투 외형·체력·이동속도·기본공격·시작 스킬 데이터화, `CharacterVisualRoot` 런타임 연결, 구성 API와 전용 Editor 검증기 추가 | C# 런타임·Editor 전체 빌드 경고 0·오류 0, `git diff --check` 통과. Unity 검증기 PASS, Editor·Windows 실행 파일 2인 로비 2/2와 Main 동시 진입, 주몽 외형·HP 100·스킬 및 보스 전투 확인 | `Assets/_Project/Scripts/Network/Player/NetworkPlayerCharacter.cs`, `Assets/_Project/Scripts/Data/CharacterData.cs`, `Assets/_Project/Scripts/Data/Characters/JumongData.asset`, `Assets/_Project/Prefabs/Players/NetworkPlayer.prefab` |
| 2026-08-09 | Codex | 진미리 | 멀티 로비 게임 시작 조건을 2명으로 통일 | 씬의 `minimumPlayers=1` 때문에 호스트 혼자 시작 가능한 정적 불일치를 제거하고 직렬화 누락에도 런타임에서 최소 2명을 강제 | 코드 기본값·Inspector 최소값·런타임 보정과 `MultiPlayerEntry`·`NetworkTest` 씬 값을 모두 2명으로 통일 | 최신 PR #54 포함 C# 런타임·Editor 빌드 경고 0·오류 0, `git diff --check`, 최신 Editor·Windows 빌드 2인 참가와 호스트 시작 후 양쪽 Main 진입 확인 | `Assets/_Project/Scripts/Network/Lobby/NetworkGameFlowController.cs`, `Assets/_Project/Scenes/MultiPlayerEntry.unity`, `Assets/_Project/Scenes/Tests/NetworkTest.unity` |
| 2026-08-09 | Codex | 진미리 | 싱글 메뉴에서 캐릭터 선택 후 선택 구성을 적용해 게임을 시작하는 흐름 연결 | 기존 멀티 캐릭터 선택 UI를 오프라인에서도 재사용하고 선택 결과를 씬 전환 이후까지 보존 | `MainMenu`→`CharacterSelect`→`Main`, 시작·뒤로 버튼, 오프라인 외형·체력·속도·기본공격·초기 스킬 적용 | C# 런타임·Editor 전체 빌드 경고 0·오류 0, 씬·버튼 직렬화와 Build Scene List 확인. Editor에서 뒤로가기와 주몽 선택 후 Main 전투 진입 확인 | `Assets/_Project/Scripts/UI`, `Assets/_Project/Scripts/Network/Stage/NetworkStageBootstrap.cs`, `Assets/_Project/Scenes/CharacterSelect.unity` |
| 2026-08-09 | Codex | 진미리 | 인게임 HUD 정보 구조와 싱글 결과 UX 개편 | 팀장 요구에 맞춰 중앙 정보 과밀을 줄이고 플레이어 상태·경험치·보스 체력을 역할별 영역으로 분리 | `StageHudPresenter` 공통 HUD, 기존 네트워크 HUD 중복 억제, 보스 플래그, TMP 다중 아틀라스, 싱글 결과 오버레이 | 런타임·Editor C# 빌드 경고 0·오류 0, `git diff --check` 통과. 싱글 실제 플레이에서 초상화·HP 감소·EXP 증가·한글 표시·패배 오버레이 확인. 보스·2인 HUD는 후속 회귀로 명시 | `Assets/_Project/Scripts/UI/StageHudPresenter.cs`, `Assets/_Project/Scripts/Network/Player/NetworkPlayerHudPresenter.cs`, `Assets/_Project/Scripts/Enemy/EnemyHealth.cs`, `BossJangsanTiger.prefab` |
| 2026-08-09 | Codex | 진미리 | 인게임 일시정지와 안전한 종료 동선 | 키보드만이 아니라 화면 버튼으로 접근 가능한 메뉴를 만들고 싱글·멀티의 시간 처리와 세션 종료 차이를 분리 | `GameplayPauseMenu`, Input System EventSystem 자동 생성, Relay Leave·NGO Shutdown 후 메인 메뉴 복귀, Editor·빌드별 종료 | C# 빌드 경고 0·오류 0. 싱글 Editor에서 메뉴 버튼·ESC·계속하기·메인 메뉴·게임 종료를 실제 확인. 멀티 비정지·세션 이탈은 후속 2인 회귀로 명시 | `Assets/_Project/Scripts/UI/GameplayPauseMenu.cs`, `Assets/_Project/Scripts/Network/Stage/NetworkStageBootstrap.cs` |
| 2026-08-07 | Codex | 진미리 | 최소 속성 연계 2개의 서버 판정·네트워크 피드백 연결 | 공격별 분기 대신 적의 중앙 상태가 속성·플레이어 ID·시간을 기록하고 순수 판정기가 감전·분쇄 조합을 결정하도록 분리 | `IElementReceiver`, `ElementalStatusController`, `SynergyResolver`, 서버 감전 연쇄·분쇄 범위 피해, RPC 월드 피드백, 스킬별 서버 속성 고정 | C# 빌드 경고 0·오류 0, `git diff --check`, Unity `NetworkElementSynergyValidator`에서 서로 다른 플레이어·시간 창·내부 쿨다운·감전 연쇄·분쇄 범위 피해 PASS 및 Console 코드 오류 0 확인. Unity Account API 경고는 기능과 무관하며 실제 Relay 2인 양쪽 피드백은 후속 회귀로 명시 | `Assets/_Project/Scripts/Combat/Element`, `Assets/_Project/Scripts/Enemy/EnemyHealth.cs`, `Assets/_Project/Editor/NetworkElementSynergyValidator.cs` |
| 2026-08-03 | OpenAI 이미지 생성 | 담당자 확인 불가 | 2179년 사이버펑크 장산범 본체와 흰 털 피격 FX 제작 | 투명 배경 보스 도트와 4프레임 피격 FX 원본 | 크로마키 제거, Point 축소, 프레임 슬라이스 후 애니메이션·프리팹으로 연결. `StageTest`와 `Main` 참조는 정적으로 확인했으나 실행 검증은 별도 필요 | `Boss/README_JANGSANBEOM_ASSETS.md`, `Assets/_Project/Art/Enemies/Jangsantiger`, `BossJangsanTiger.prefab`; 담당자 보완 필요 |
| 2026-07-27 | Claude Code | 미리 (개발자 A) | 플레이어 체력·사망 구현 (feat/player-health) | 체력 추적·TakeDamage·사망 시 UnityEvent 발동 구조, 실제 적이 없어 Space 키로 데미지를 주는 임시 디버그 스크립트 포함 | `PlayerHealth.cs`, `PlayerHealthDebugTester.cs` 작성 | 코드는 그대로 채택. 최초 버전에서 currentHealth/isDead가 Inspector에 안 보이는 실수가 있어 SerializeField 추가로 수정 후, Play 모드에서 Space로 데미지 주며 Current Health가 정확히 감소(100→70 등)하고 0에서 Is Dead가 체크되는 것을 확인 | `Assets/_Project/Scripts/Player/PlayerHealth.cs`, `PlayerHealthDebugTester.cs`, 커밋 897b94c |
| 2026-07-27 | Claude Code | 미리 (개발자 A) | 플레이어 경험치 수집·레벨업 구현 (feat/player-experience) | ExperienceOrb 트리거 충돌 시 경험치 획득, 누적 경험치가 임계값 넘으면 레벨업·다음 레벨 임계값 증가 | `PlayerExperience.cs` 작성 (팀 코드 컨벤션 확인 후 namespace 생략, `[ContextMenu]` 테스트 방식 적용) | 코드는 그대로 채택. develop에 이미 merge된 팀 버전 `PlayerHealth.cs`(IDamageable 구현)를 확인해 기존 자체 버전은 폐기하고 실제 구조에 맞춰 작업 방향 조정. Play 모드에서 ContextMenu로 경험치 5씩 추가·10에서 레벨업(레벨 2, 다음 임계값 15)까지 확인 | `Assets/_Project/Scripts/Player/PlayerExperience.cs`, 커밋 106001d |
| 2026-07-30 | Claude (Cowork) | 아침호랑이 | 미결 기획 5건 확정 | 연계 조합 범위·랜덤 속성 보정·동속성 중첩·기본공격 방향·스킬 습득 방식을 선택지 형태로 검토 | 결정안 5건과 문서 반영 초안 | 제시된 선택지 중 팀 상황(마감·구현량)에 맞는 안을 직접 선택. 7속성 커버 우선순위는 사람이 판단 | `11_CONCEPT_AND_HERO_DESIGN.md`, `08_DECISION_LOG.md`(D-015~D-019), `HANDOFF_2026-07-30.md` |
| 2026-07-31 | Claude (Cowork) | 아침호랑이 | 주몽 기본공격·P0 스킬 3종·이펙트 구현 | 7속성 시스템, 직선/관통/폭발 투사체, 원소 장판(공용), 적토마, 자동 시전. 기존 유도 Projectile은 수정하지 않고 신규 클래스 분리 | 신규 스크립트 10개, `PlayerController.SpeedMultiplier` 추가, 세팅 가이드 문서 | 프리팹·SkillData·컴포넌트 세팅은 사람이 직접 수행. CombatTest 씬 Play 테스트로 기본공격 방향·폭발·관통·적토마 장판·랜덤 속성·속성 색 변화 6항목 모두 정상 확인. 진행 중 Player 프리팹의 기존 Missing Script(PlayerHealth GUID) 발견해 복구 | `Scripts/Combat/*`, `Scripts/Player/*`, `Prefabs/Combat/*`, `ScriptableObjects/Skills/Jumong/`, `IMPL_2026-07-31_JUMONG_SKILLS.md`; 현재 커밋됨 |
| YYYY-MM-DD |  |  |  |  |  |  |  |
| 2026-08-09 | Codex·OpenAI 이미지 생성 | 진미리 | 실행 화면의 흰 사각형과 Player Missing Script 경고 제거 | 삭제된 공용 임시 스프라이트 GUID와 잘린 `PlayerExperience` 프리팹 직렬화를 진단하고, 적·투사체용 픽셀 에셋을 생성 | 크로마키 제거 후 Point 필터 Unity 스프라이트로 연결, 일반 적 3종·화살 4종 참조 교체, `PlayerExperience` 컴포넌트 복구 | `git diff --check` 및 Unity Play Mode에서 Missing Script·흰 사각형·공격·EXP 증가 재확인 필요 | `Assets/_Project/Art/Enemies/Generated`, `Assets/_Project/Art/Combat/Generated`, 관련 프리팹 |
| 2026-08-09 | Codex | 진미리 | 팀 제공 도시 배경·MUGUNG 로고를 메인 메뉴에 충돌 적게 적용 | 팀원이 같은 씬을 수정 중인 상황을 고려해 씬 YAML 대신 기존 `StartMenuController`의 런타임 테마 구조를 확장 | Resources 배경·로고 로드, 전체 화면 배경·명암 오버레이·반투명 메뉴 패널·버튼 재배치, 로고 누락 시 텍스트 폴백 | `git diff --check` 통과. Unity에서 16:9·Free Aspect 화면과 버튼 클릭 회귀 확인 필요 | `Assets/_Project/Scripts/UI/StartMenuController.cs`, `Assets/Resources/UI/MainMenu` |
| 2026-08-09 | Codex | 진미리 | 현재 전투 구조에 맞는 MVP 타격·위험·보스 연출 추가 | 장문의 상용 게임 연출 제안을 현재 구현·마감 일정과 비교해 일반 피격, 사망, 저체력, 보스 등장만 P0로 선택 | 공통 `CombatFeedbackPresenter`, 데미지 숫자·스프라이트 점멸·강도별 카메라 흔들림·저체력 가장자리·보스 경고를 기존 피해 권한 구조에 연결 | 정적 참조·`git diff --check` 후 Unity 싱글 및 2인에서 중복 피드백 여부 확인 필요 | `Assets/_Project/Scripts/UI/CombatFeedbackPresenter.cs`, `EnemyHealth.cs`, `CameraFollow.cs`, `GameManager.cs` |
| 2026-08-09 | Codex·웹 검색 | 진미리 | 제출 빌드용 메뉴·전투 BGM과 최소 효과음 구조 추가 | 라이선스가 명확한 CC0 후보를 먼저 조사하고 세계관에 맞는 어두운 도시·사이버 전투 루프를 선택 | OpenGameArt OGG 2종 직접 연결, 씬별 음악 전환, 버튼·피격·보스용 교체 가능한 효과음 채널과 임시 합성음 | 출처·라이선스 페이지 확인, `git diff --check`; Unity 볼륨·루프·씬 전환 수동 확인 필요 | `GameAudioController.cs`, `Assets/Resources/Audio/Music` |
| 2026-08-09 | Codex·OpenAI 이미지 생성 | 진미리 | 도시 배경을 가리던 로고의 검은 사각 배경 제거와 메인 메뉴 UX 개선 | 기존 로고의 글자·무궁화·적청 문양은 유지하고 단색 크로마키 배경만 생성해 로컬 후처리로 투명화 | 투명 로고 PNG, 첫 실행 로고 페이드 연출, 화면 비율별 메뉴·버튼 축소, 배경이 보이는 반투명 패널 | 투명 픽셀·로고 가장자리 확인, Unity Free Aspect·16:9에서 잘림·가독성 수동 회귀 필요 | `Assets/Resources/UI/MainMenu/mugung_logo_transparent.png`, `StartMenuController.cs` |
| 2026-08-09 | Codex | 진미리 | 준비 중 알림뿐이던 설정 버튼을 제출 가능한 최소 설정 화면으로 완성 | 별도 씬·프리팹 수정 없이 런타임 패널을 만들고 지속 저장 API를 기존 오디오 관리자에 연결 | BGM·효과음 슬라이더, 전체 화면 전환, ESC·닫기 동선, PlayerPrefs 저장, 열기·닫기 애니메이션 | Unity 컴파일·설정 열기·음량 즉시 반영·재실행 유지·화면 모드 수동 확인 필요 | `GameAudioController.cs`, `MainMenuSettingsPanel.cs`, `StartMenuController.cs` |
| 2026-08-09 | Codex | 진미리 | 메뉴·캐릭터 선택·게임 씬이 즉시 튀어나오는 전환 완화 | 네트워크 SceneManager를 가로채지 않고 `sceneLoaded` 이후 화면 표현만 담당하도록 권한 경계를 유지 | DDOL 전환 캔버스, 0.32초 암전 해제, 전환 중 짧은 입력 차단, 첫 메인 로고 연출과 중복 방지 | Unity 컴파일·싱글·멀티 씬 전환에서 입력 지연과 중복 캔버스 수동 확인 필요 | `SceneFadePresenter.cs` |

## 7. 확인된 검토·수정 사례

AI 활용의 품질은 “코드를 몇 줄 직접 타이핑했는가”보다 결과를 어떻게 판단하고 프로젝트에 맞췄는가로 설명한다.

### 프로젝트 규칙에 맞춘 조정

- 이동 프리팹 경로를 AI 가이드의 단수형 폴더가 아닌 실제 `Prefabs/Players`로 변경했다.
- 경험치 코드는 이미 병합된 팀 구조와 충돌하지 않도록 기존 `PlayerHealth`와 네임스페이스를 다시 확인한 뒤 연결했다.
- 전투 코드는 구체 Health 클래스가 아닌 `IDamageable` 계약으로 통합했다.
- 투사체형 공격 수치와 프리팹을 `SkillData`로 분리해 Inspector에서 변경할 수 있게 했다.

### 검증에서 발견한 문제

- `PlayerHealth` 디버그 상태가 Inspector에 보이지 않는 문제를 찾아 직렬화 설정을 보완했다.
- HUD가 네임스페이스를 참조하지 못하고 사망을 null 참조로 판단하던 문제에 대해 별도 수정 브랜치를 만들었다.
- 현재 `StageTest`에서 2·3웨이브 시작 시간이 같아 2웨이브가 생략될 가능성을 문서 감사에서 식별했다.
- 일반 적 프리팹을 보스로 재사용하는 상태를 완성된 보스 구현과 구분했다.
- 아이템 폴더 이동 시 `.meta` GUID와 프리팹 참조 보존이 필요함을 통합 조건으로 남겼다.

## 8. 검증 기준

AI 산출물은 다음 확인을 거쳐야 완료로 처리한다.

| 항목 | 완료 기준 |
|---|---|
| 이해 | 기능 담당자가 주요 흐름, 공개 API와 실패 조건을 설명할 수 있음 |
| 컴파일 | 목표 브랜치에서 새 C# 오류가 없음 |
| Unity 연결 | 씬·프리팹·Collider·레이어·태그·Inspector 참조가 저장됨 |
| Play 검증 | 정상 동작과 대표 실패 조건을 재현함 |
| 회귀 | 기존 이동·공격·피해·경험치 등 영향받는 기능이 유지됨 |
| 멀티플레이 | 네트워크 기능은 두 인스턴스 또는 두 PC에서 결과를 비교함 |
| 의존성 | 불필요한 패키지와 출처 불명 코드를 추가하지 않음 |
| 기록 | 도구, 목적, 산출물, 팀원의 통합·검증, 커밋을 남김 |

현재 프로젝트에는 자동화된 EditMode·PlayMode 테스트가 없다. 따라서 수동 Play 검증 기록과 브랜치·커밋 정보를 함께 남기는 것이 중요하다.

## 9. 한계와 개선 계획

- 일부 초기 코드 작업은 AI 사용 사실만 확인되고 정확한 도구명·원문 프롬프트가 남아 있지 않다. 제출 PDF 전에 팀 대화·개인 기록을 확인해 보완한다.
- AI는 오래된 문서나 다른 브랜치를 기준으로 중복 구조를 제안할 수 있다. 요청 전 실제 파일과 Git 상태를 제공한다.
- Unity의 프리팹·씬 직렬화와 Inspector 연결은 코드만 읽어서는 완전히 검증할 수 없다. 항상 에디터와 Play Mode 확인을 병행한다.
- “컴파일 성공”과 “게임 한 판 통과”를 같은 것으로 보지 않는다.
- 네트워크 코드는 단일 에디터 테스트로 완료 처리하지 않는다.
- AI가 수정 없이 제안한 코드도 팀이 이해·채택·검증한 근거를 남긴다.

## 10. 제출 PDF용 요약 문안

> Shura 팀은 생성형 AI를 빠른 프로토타이핑과 기술 탐색을 위한 개발 도구로 적극 활용했습니다. 팀원이 게임 규칙, 기능 목표, 코드 계약과 완료 조건을 정의하면 AI가 C# 구현안과 오류 분석, 테스트 절차의 초안을 제안했습니다. 팀원은 결과를 기존 구조와 비교해 채택 여부를 결정하고, Unity 씬·프리팹·ScriptableObject·Inspector 연결, 수치 조정, Play Mode·네트워크 검증, 오류 수정 방향과 Git 통합을 담당했습니다. AI가 완성된 코드 형태를 제안한 경우도 숨기지 않되, 실제 프로젝트에 적용하기 위한 설계 판단·통합·검증과 최종 책임은 개발팀이 수행했습니다.

최종 PDF에는 대표 프롬프트 3~5개, AI 초안과 통합 과정의 화면, Play 검증 결과, 해결하지 못한 한계와 외부 에셋·패키지 출처를 함께 첨부한다.

## 11. 네트워크 통합에서의 대표 AI 제안과 사람의 판단

세부 구현·현재 상태를 여기에서 반복하지 않고, AI 제안을 팀이 어떤 기준으로 채택했는지만 남긴다.

| 주제 | AI 제안 | 사람이 선택·검증한 기준 |
|---|---|---|
| 승패 | 로컬 체력이 아니라 서버 체력에서 파생한 결과를 복제 | 기존 로컬 `StageTest`를 유지하고 네트워크 플레이어에 어댑터를 추가해 충돌 범위를 줄임 |
| 결과 UI | 소유 플레이어가 자신의 오버레이를 런타임 생성 | 씬의 특정 Player 참조를 피하고 화면당 HUD·결과 UI를 하나만 유지 |
| 결과 이후 전환 | 호스트만 NGO 씬 전환·RPC를 결정 | 클라이언트 임의 전환을 막고 NetworkManager 종료 후 로비를 로드하도록 순서 검토 |
| 스테이지 | `StageTest`를 복사하지 않고 `Main` 부트스트랩에서 공용 컴포넌트 조립 | 정적 로컬 Player와 NetworkPlayer 중복을 피하고 두 씬의 테스트 목적을 분리 |
| 적·보스 | 기존 `GameManager`에 네트워크 세션 권한 분기 추가 | 로컬 테스트 경로를 보존하고 서버만 생성·승리를 확정하도록 채택 |
| HUD | 동기화 상태를 읽는 네트워크 전용 Presenter 사용 | 기존 `HUDController`는 로컬 회귀용으로 유지하고 역할을 분리 |

실제 변경 목록과 검증 증거는 6장의 날짜별 AI 활용 표, `04_TECHNICAL_DESIGN.md`, `10_TEST_PLAN.md`에서 확인한다.
