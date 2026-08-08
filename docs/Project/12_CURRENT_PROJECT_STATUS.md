# Shura 현재 개발 현황

> 기준 시각: **2026-08-09 KST**
> 확인 범위: Git 상태·최근 커밋, 관련 스크립트·프리팹·씬·ScriptableObject, Build Scene List와 기존 테스트 기록
> 이번 갱신에서는 Unity Play Mode나 새 빌드를 실행하지 않았다. 코드·직렬화의 **정적 확인**과 기존 **실행 검증 기록**을 구분한다.

## 1. 기준 상태

| 구분 | 현재 값 | 의미 |
|---|---|---|
| 현재 브랜치 | `develop` @ `080a7f7` | 로컬 `develop`과 현재 로컬 `origin/develop` 참조가 같음. 원격 fetch는 수행하지 않음 |
| 최신 통합 커밋 | `080a7f7` | PR #51 메인 메뉴·정식 멀티 입장·캐릭터 선택 흐름 병합 |
| 최신 자동 검증 기록 | `d253283`, 2026-08-07 | 감전·분쇄 연계의 전용 Unity 검증기 통과 기록 |
| 최신 실제 2인 기록 | `83b8783`, 2026-08-02 | 레거시 `NetworkTest → Main`의 Windows Editor·실행 파일 Relay 기록 |
| 작업트리 확인 | 제한적 확인 | Git LFS 임시 폴더 권한 오류로 일반 `git status`가 실패함. LFS 필터 우회 결과, 문서 PNG 2개만 변경으로 보였고 코드·씬·설정의 미커밋 변경은 보이지 않았음 |

현재 장산범, `Main` 연결, 주몽 SkillData 이동, 네트워크 스킬·연계, 메뉴·로비 코드는 모두 커밋 이력에 포함된다. 이전 문서의 “장산범·SkillData 로컬 작업 중” 표기는 더 이상 현재 상태가 아니다.

## 2. 한눈에 보는 상태

| 영역 | 상태 | 현재 범위 | 남은 핵심 작업 |
|---|---|---|---|
| 프로젝트 기반 | 구현·정적 확인 | Unity `6000.3.20f1`, URP `17.3.0`, Input System `1.19.0`, NGO `2.13.1`, Multiplayer Services `2.3.0` | 버전 유지 |
| 메인 메뉴 | 부분 구현·실행 미검증 | `MainMenu`와 `StartMenuController`; 멀티 버튼은 `MultiPlayerEntry`로 연결 | 싱글·종료 버튼 연결, 실제 UI 회귀 |
| 싱글플레이 | 미구현 | `CharacterSelect` UI 초안은 있으나 Build Scene List 제외, 시작·뒤로 버튼 이벤트 없음 | 메뉴→선택→로컬 게임→결과 전체 흐름 |
| 정식 Relay 입장 | 코드상 구현·실행 검증 필요 | `MultiPlayerEntryUI`가 UGS 익명 로그인, 최대 2인 Relay 세션 생성·코드 참가·실패 정리. `NetworkSessionState`가 세션 보관 | 새 씬 경로에서 호스트·게스트 실제 접속, 실패·재시도·나가기 회귀 |
| 멀티 대기방 | 부분 구현·실행 검증 필요 | `MultiPlayerLobby`에서 참가 코드, 최대 2슬롯, 접속 인원, 캐릭터 표시, 호스트 시작·나가기 | 시작 최소 인원 `1→2` 정합성, 호스트 이탈·새 방·정식 복귀 검증 |
| 캐릭터 선택 | 부분 구현 | `CharacterData`·주몽 데이터 1개, 슬롯 UI, 서버 쓰기 캐릭터 ID와 양쪽 선택 표시 | 선택값을 `Main`의 플레이어 외형·스킬·스탯에 적용, 추가 캐릭터 |
| 게임 진입 | 부분 구현·새 경로 미검증 | 호스트가 NGO SceneManager로 `MultiPlayerLobby → Main` 전환. 코드상 호스트 전용 | `minimumPlayers=1` 수정 여부 결정, 실제 2인 동시 전환 검증 |
| 네트워크 플레이어 | 구현·기존 기본 검증 | 소유자 이동·카메라, 서버 HP·사망·EXP·레벨, HUD, 스킬 RPC | 선택 캐릭터 적용, `JumongVisual`·애니메이션 정식 프리팹 연결, 위치·입력 검증 강화 |
| 기본공격·주몽 스킬 | 부분 구현·자동 검증 | 기본·폭발·편전·적토마를 서버 판정본과 비서버 시각 복제본으로 분리, 서버 허용 목록·쿨다운·발동 위치 검증 | 실제 2인 투사체·장판·적토마 속도·속성 표시 회귀 |
| 속성 연계 | 구현·자동 검증 | 서로 다른 플레이어의 물+번개 감전, 얼음+흙 분쇄를 서버 판정. 시간 창·내부 쿨다운·범위 피해 검증 기록 | 실제 2인 발동·양쪽 RPC 피드백, 독늪·화염 폭풍 |
| 적·경험치 | 구현·기존 기본 검증 | 서버 적 생성·AI·체력·사망·Despawn, 서버 오브 생성과 소유권·거리 검증 후 EXP 반영 | 명중 요청 검증 강화, 자석 위치·경쟁 획득 2인 회귀 |
| 장산범 보스 | 코드·씬 연결, 실행 미검증 | HP 500·공격 30·이속 1.5, NetworkObject·NetworkTransform·서버 권한 구성, `StageTest`와 `Main` 참조 | 전용 패턴, 로컬/2인 처치·승리 검증 |
| 레벨업 선택 | 미구현 | EXP와 레벨 수치만 증가 | 능력치 3택1, 특정 레벨 스킬 3택1, 속성 미리 표시와 네트워크 동기화 |
| 아이템 | 부분 구현 | 회복·자석 픽업과 로컬 테스트 구조 | 드롭 규칙, 네트워크 생성·제거·자석 위치 |
| 결과 흐름 | 부분 구현·레거시 검증 | 패배와 과거 양쪽 `NetworkTest` 복귀 기록, 호스트 재시작·복귀 코드 | 승리·재시작 검증, 복귀 대상 `NetworkTest`를 정식 흐름과 통합 |
| Windows 빌드 | 과거 부분 검증 | 8월 2일 레거시 경로 통합 빌드·2인 실행 기록 | `080a7f7` 새 메뉴 경로 제출 후보 빌드·새 기기 검증 |
| Web 빌드 | 미구현·미검증 | 공개 빌드·호스팅·링크 없음 | 빌드, Relay·입력·UI·성능·호스팅 검증 |

## 3. 현재 실행 경로

정식으로 의도된 현재 경로:

```text
MainMenu
→ 멀티 버튼
→ MultiPlayerEntry
→ UGS 초기화·익명 로그인
→ 호스트 Relay 방 생성 / 게스트 코드 참가
→ 호스트 NGO SceneManager가 MultiPlayerLobby 동기화
→ 주몽 1종 선택 상태와 접속 인원 표시
→ 호스트 게임 시작
→ Main
→ NetworkStageBootstrap이 스테이지 런타임 구성
→ 서버가 적·보스·체력·EXP·승패 확정
```

정적 확인상 이 경로에는 다음 단절이 있다.

- `MultiPlayerEntry`의 `NetworkGameFlowController.minimumPlayers`는 `1`이므로 호스트 혼자도 시작 조건을 만족한다.
- 로비의 캐릭터 ID는 `NetworkLobbyState`와 UI까지만 사용되고 `Main` 플레이어 구성에는 전달되지 않는다.
- 결과 후 `NetworkGameResultActions`는 `MultiPlayerEntry`나 `MultiPlayerLobby`가 아니라 레거시 `NetworkTest`를 로드한다.

8월 2일 실제 검증 경로는 별도의 `NetworkTest → Main → NetworkTest`였다. 새 정식 경로는 코드상 존재하지만 실행 검증 필요다.

## 4. 씬과 주요 GameObject 구조

Build Scene List 활성 씬은 다음 5개다.

| 순서 | 씬 | 주요 GameObject·Component | 상태 |
|---:|---|---|---|
| 1 | `MainMenu` | `Canvas/StartMenuPanel`, `StartMenuController`, 싱글·멀티·종료 버튼 | 멀티만 연결 |
| 2 | `MultiPlayerEntry` | `NetworkManager`(`UnityTransport`, NGO, `NetworkGameFlowController`, `NetworkSessionState`, `NetworkLobbySceneLoader`), `MultiplayerEntryUI`, Relay 입력·버튼 | 코드상 구현, 실행 검증 필요 |
| 3 | `MultiPlayerLobby` | 씬 배치 `NetworkLobbyState`(`NetworkObject`), `CharacterListUI`, `MultiplayerLobbyUI`, `MultiplayerLobbyExitController`, 내/상대 슬롯 | 부분 구현, 실행 검증 필요 |
| 4 | `Main` | `Main Camera/CameraFollow`, `NetworkStageBootstrap` | 기존 전투 기본 검증, 새 입장 경로 미검증 |
| 5 | `Tests/NetworkTest` | 레거시 `NetworkManager`, `NetworkTestUI`, 시작 UI | 과거 2인 검증 경로 |

- `CharacterSelect`는 주몽 선택 UI 초안이지만 Build Scene List에 없고 싱글 시작·뒤로 버튼이 연결되지 않았다.
- `StageTest`, `PlayerTest`, `CombatTest`도 Build Scene List에 없다. `StageTest`는 로컬 기능 회귀용, `PlayerTest`는 플레이어 단위용이며 `CombatTest`는 현재 불완전하다.
- `Main`에는 정적 플레이어·스테이지 오브젝트가 없고 `NetworkStageBootstrap`이 런타임에 구성한다.

## 5. StageTest와 Main의 관계

| 설정 | `StageTest` | `Main` |
|---|---:|---:|
| 라운드 | 10초 | 60초 |
| 웨이브 전환 | 3초 / 4초 | 20초 / 40초 |
| 일반 적 | `Enemy.prefab` | `Enemy.prefab` |
| 보스 | `BossJangsanTiger.prefab` | `BossJangsanTiger.prefab` |

두 씬 모두 장산범을 참조하지만 라운드·웨이브 데이터는 분리되어 있다. 최종 15분 데이터와 공용 `StageConfig`는 미구현이다.

## 6. 네트워크 권한 경계

| 데이터·행동 | 현재 권한 |
|---|---|
| 플레이어 입력·이동 Transform | 소유 클라이언트 |
| 로비 슬롯·캐릭터 ID | 클라이언트 요청, 서버 `NetworkVariable` 기록 |
| 게임 시작·씬 전환 | 호스트/서버 |
| 기본공격·스킬 | 소유 클라이언트 요청, 서버가 허용 목록·쿨다운·발동 위치 확인 |
| 투사체·장판 피해 | 서버 판정본만 적용, 비서버 화면은 피해 없는 RPC 시각 복제본 |
| 속성 기록·감전·분쇄 | 서버 |
| 적 생성·AI·공격·Transform·체력·사망 | 서버 |
| 경험치 오브 생성·획득·제거 | 서버, 소유권·거리 검증 |
| 플레이어 체력·사망·EXP·레벨 | 서버 `NetworkVariable` |
| 승리·패배 | 서버 |
| HUD·결과 표시 | 각 소유 플레이어의 로컬 UI |

스킬 시각 오브젝트는 `NetworkObject`가 아니라 RPC 복제이므로 장시간 지연 보정이나 완전한 상태 복구를 제공하지 않는다. 캐릭터 선택은 서버 복제되지만 게임플레이 구성에는 아직 사용되지 않는다.

## 7. 검증 증거

실행 검증 완료로 인용할 수 있는 범위:

- 2026-08-02 `83b8783`: 레거시 `NetworkTest`에서 2인 Relay 참가, `Main` 동시 전환, 플레이어·적·HUD·웨이브, 사망·양쪽 패배, 양쪽 `NetworkTest` 복귀
- 2026-08-05 `ca61356`: 전용 검증기로 서버 투사체 1회 피해와 시각 복제본 무피해 확인
- 2026-08-07 `d253283`: 전용 검증기로 서로 다른 플레이어·시간 창·내부 쿨다운·감전 연쇄·분쇄 범위 피해 확인

코드상 존재하지만 실행 검증이 필요한 항목:

- `MainMenu → MultiPlayerEntry → MultiPlayerLobby → Main` 전체 2인 흐름
- 캐릭터 선택 동기화 화면과 선택값의 실제 플레이어 적용
- 편전·적토마의 실제 두 화면 표시·속도·장판
- 장산범 로컬/네트워크 전투·승리
- 결과 후 재시작과 정식 메뉴/입장 화면 복귀
- 로비 나가기, 호스트 강제 이탈, 새 방, 10분 지속
- 최신 Windows 제출 후보, 새 Clone, Web 빌드

세부 테스트 ID는 `10_TEST_PLAN.md`를 따른다.

## 8. 현재 TODO와 위험

1. 정식 로비의 `minimumPlayers=1`을 P0의 2인 시작 조건과 일치시킬지 결정하고 수정·검증한다.
2. `NetworkLobbyState`의 캐릭터 ID를 `Main`까지 보존해 플레이어 외형·스킬·스탯에 적용한다. 현재 주몽 데이터만 있고 실제 전투 적용 소비자가 없다.
3. `NetworkPlayer.prefab`의 빈 `CharacterVisualRoot`에 `JumongVisual` 애니메이션을 연결할지 포함해 고정 스프라이트 구조를 정리한다.
4. 결과 후 `NetworkTest` 복귀를 정식 `MultiPlayerEntry`/`MultiPlayerLobby` 흐름과 통합한다.
5. 싱글·종료 버튼과 독립 `CharacterSelect`의 시작·뒤로 이벤트가 연결되지 않았다.
6. 장산범은 전용 수치·외형만 있고 전용 행동 패턴은 확인되지 않았다. `Main` 연결은 완료됐지만 승리 실행은 미검증이다.
7. `StageTest`와 `Main`의 라운드 데이터가 분리되어 최종 15분 설정이 어긋날 위험이 있다.
8. 클라이언트 적 피해 요청의 피해량·명중 위치 검증은 프로토타입 수준이다.
9. EditMode·PlayMode 테스트 스위트는 없고 전용 Editor 검증기와 수동 기록에 의존한다.
10. Git LFS 임시 폴더 권한 오류 때문에 일반 `git status/diff`가 실패한다. 제출 전 LFS 상태와 주몽 PNG 원본을 정상 환경에서 확인해야 한다.

## 9. 다음 작업 순서

1. 새 정식 메뉴·입장·로비 경로를 두 인스턴스로 실행해 생성·참가·선택·시작을 검증한다.
2. 2인 시작 조건, 캐릭터 선택의 전투 적용, 결과 후 복귀 경로의 세 가지 단절을 먼저 해결한다.
3. 장산범 승리·재시작과 실제 2인 스킬·연계 피드백을 회귀한다.
4. 싱글플레이를 제출 범위에 포함할지 결정하고, 포함 시 독립 흐름을 연결한다.
5. 공용 15분 스테이지 데이터, 레벨업 선택, 조작 안내를 완성한다.
6. Git LFS 상태를 정상화한 뒤 새 Clone·최신 Windows·Web 빌드와 외부 Relay를 검증한다.
7. 제출 후보 커밋·태그·빌드·영상·PDF·공개 링크를 고정한다.
