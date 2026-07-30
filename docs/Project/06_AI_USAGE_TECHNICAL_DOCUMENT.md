# Shura AI 활용 기술 문서

## 1. 문서 목적

이 문서는 NAN 2026 제출 요건에 맞춰 프로젝트에서 사용한 AI 도구, 주요 지시 사항, 산출물, 검증과 수정 내역을 기록한다. AI가 생성한 결과를 그대로 사용한 것으로 작성하지 않고, 팀원이 어떤 기준으로 검토하고 수정했는지 함께 남긴다.

## 2. 현재 계획된 AI 활용 영역

### 기획

- 게임 핵심 루프와 MVP 범위 정리
- 스킬 연계 조합 아이디어 발산
- 일정과 위험 요소 점검
- 회의 결정 사항 문서화

### 개발

- Unity C# 코드 초안
- 코드 구조 설명과 리팩터링 제안
- 컴파일 오류와 런타임 오류 분석
- 테스트 코드와 재현 절차 작성
- Git 명령과 협업 규칙 작성

### 콘텐츠

- 캐릭터·스킬 명칭 후보
- UI 문구와 튜토리얼 문장
- 밸런스 표 초안
- 에셋 제작용 콘셉트 프롬프트

### QA·제출

- 테스트 케이스 생성
- 버그 보고서 정리
- 게임 소개서·AI 기술서·팀원 역할서 초안
- 영상 구성안과 제출 체크리스트 작성

## 3. AI 사용 원칙

1. AI가 생성한 코드는 사람이 읽고 작동 원리를 설명할 수 있어야 한다.
2. 실행과 테스트 없이 AI 코드를 완료 처리하지 않는다.
3. 외부 코드나 에셋이 포함될 가능성이 있으면 출처와 라이선스를 확인한다.
4. 비밀키, 계정 정보, 개인정보를 프롬프트에 입력하지 않는다.
5. 게임 플레이 영상에는 AI 합성 장면을 사용하지 않는다.
6. 중요한 설계 결정은 AI 제안을 참고하되 팀원이 최종 결정한다.
7. 사용 기록은 작업 당일 작성한다.

## 4. 기술적 활용 흐름

```text
팀원이 작업 목표 정의
→ AI에 현재 구조·제약·완료 조건 제공
→ AI가 초안 또는 수정안 생성
→ 팀원이 코드·문서 검토
→ Unity에서 컴파일·플레이 테스트
→ 문제 수정 및 결과 확인
→ Git 커밋
→ AI 사용 내역 기록
```

## 5. 주요 프롬프트 작성 구조

좋은 프롬프트에는 다음 항목을 포함한다.

- 현재 Unity 버전과 프로젝트 구조
- 만들려는 기능
- 입력과 출력
- 수정 가능한 파일
- 수정하면 안 되는 영역
- 네트워크 권한 규칙
- 완료 조건
- 테스트 방법
- 불확실할 때 임의로 결정하지 말고 보고하라는 지시

### 코드 요청 템플릿

```text
Unity 6.3 LTS Universal 2D 프로젝트다.

목표:
[구현할 기능]

현재 구조:
[관련 스크립트와 데이터 구조]

제약:
- 먼저 `docs/Project/04_TECHNICAL_DESIGN.md`의 현재 구현 코드 계약과 실제 관련 스크립트·프리팹 연결을 확인한다.
- 공격 생성은 기존 `SkillRunner.TryRun` 흐름을 사용하고, 발동 측은 대상과 쿨다운만 관리한다.
- 피해는 구체 Health 클래스를 직접 호출하지 않고 `IDamageable.TakeDamage`로 전달한다.
- 적 탐색과 명중 판정에 사용하는 `Enemy` 레이어, `Player` 태그, Collider·컴포넌트 계층 계약을 유지한다.
- 기존 공개 API를 불필요하게 변경하지 않는다.
- 에디터 전용 코드를 런타임 코드에 넣지 않는다.
- 매 프레임 전체 오브젝트 검색을 사용하지 않는다.
- 새로운 외부 패키지를 설치하지 않는다.
- 문서에만 있는 계획 클래스가 이미 구현됐다고 가정하지 않는다.

완료 조건:
- Unity 컴파일 오류가 없다.
- [테스트 씬]에서 [동작]을 재현할 수 있다.
- 변경 파일과 테스트 방법을 보고한다.

먼저 관련 파일을 읽고 구조를 설명한 뒤 수정하라.
새 병렬 시스템을 만들기 전에 기존 확장 지점을 사용할 수 있는지 확인하라.
```

### 버그 분석 요청 템플릿

```text
다음 Unity 버그를 진단해라.

기대 동작:
[정상 동작]

실제 동작:
[문제]

재현 순서:
[단계]

Console 로그:
[오류]

관련 파일:
[경로]

우선 원인을 확인하고, 근거 없이 여러 파일을 동시에 수정하지 마라.
수정 후 재현 테스트와 회귀 테스트 방법을 작성하라.
```

## 6. AI 사용 내역

아래 표는 예시 행을 지우지 말고 실제 사용 기록으로 계속 추가한다.

| 날짜 | 도구 | 담당자 | 목적 | 주요 프롬프트 요약 | 생성 결과 | 사람의 검토·수정 | 관련 파일·커밋 |
|---|---|---|---|---|---|---|---|
| 2026-07-25 | ChatGPT/Codex | TBD | 프로젝트 초기 구조와 문서 작성 | Unity 2D 협동 생존 게임의 폴더·문서·일정 구성 | 프로젝트 문서 초안 | 팀 상황에 맞춰 MVP, 역할, 폴백 범위 검토 | 초기 문서 커밋 |
| 2026-07-26 | Claude Code | 미리 (개발자 A) | 플레이어 기본 이동 구현 (feat/player-movement) | PlayerTest 테스트 씬 생성, Rigidbody2D/Collider2D 기반 WASD 이동, Input System(Player Input, Send Messages)으로 구현, 속도 Inspector 노출, 대각선 이동 정규화, Player.prefab 저장 | `PlayerController.cs` 전체 코드 작성 + Unity 에디터 내 GameObject/컴포넌트 구성, 씬 생성, PR 작성 단계별 가이드 | 코드는 그대로 채택. Play 모드에서 WASD 이동·대각선 속도 동일 여부·Inspector 속도 변경·다른 씬에서의 Prefab 동작을 직접 테스트로 확인. Prefab 저장 경로는 지시받은 `Prefabs/Player` 대신 기존 컨벤션인 `Prefabs/Players`로 조정 | `Assets/_Project/Scripts/Player/PlayerController.cs`, 커밋 3408a2b·b27ada9, PR #7 |
| 2026-07-26 | Claude Code | 미리 (개발자 A) | 카메라 플레이어 추적 구현 (feat/camera-follow) | Main Camera가 LateUpdate에서 Vector3.Lerp로 Player를 부드럽게 추적, followSpeed·offset Inspector 노출 | `CameraFollow.cs` 전체 코드 작성 + Unity 에디터 내 컴포넌트 연결 가이드 | 코드는 그대로 채택. Play 모드에서 Main Camera Transform 좌표 변화로 실제 추적 동작 확인 | `Assets/_Project/Scripts/Camera/CameraFollow.cs`, 커밋 2b0b39a |
| 2026-07-28 | Claude Code | 미리 (개발자 A) | 플레이어 HUD 및 결과 화면 구현 (feat/player-hud) | Canvas 기반 체력/레벨·경험치 텍스트 표시, PlayerHealth 참조가 null이 되면(사망) 결과 패널 자동 표시 | `HUDController.cs` 작성 + Unity 에디터에서 Canvas/TextMeshPro/Panel 구성 단계별 가이드 | 코드는 그대로 채택. Play 모드에서 데미지 시 HP 텍스트 실시간 갱신, 사망 시 Game Over 패널 표시 확인. UI 배치(텍스트 겹침, Canvas Scaler 모드, 폰트 크기/여백)는 여러 차례 시행착오 끝에 사람이 직접 값 조정 | `Assets/_Project/Scripts/UI/HUDController.cs`, 커밋 c3446a2 |
| 2026-07-30 | Codex | 진미리 | 일반 플레이어와 네트워크 플레이어 구조 통합 1단계 | 진미리 담당 문서와 전체 코드·프리팹 상태를 대조하고, 이미 별도 브랜치에 구현된 HUD 수정은 제외한 뒤 네트워크 플레이어에 Input Actions·충돌·체력·경험치·기본 공격·소유자 카메라 연결을 통합 | `NetworkPlayerOwnerSetup.cs` 추가, `NetworkPlayerMovement` 입력 표준화, `NetworkPlayer.prefab` 구성 통합, `CameraFollow` 런타임 대상 설정 API 추가 | 소유자가 아닌 플레이어의 입력·자동 공격·경험치 획득을 비활성화하도록 검토. 체력·경험치·공격 결과의 네트워크 동기화와 실제 Main 씬 전환은 후속 작업으로 명시 | `Assets/_Project/Scripts/Network/NetworkPlayerOwnerSetup.cs`, `Assets/_Project/Prefabs/Players/NetworkPlayer.prefab` |
| 2026-07-30 | Codex | 진미리 | 멀티플레이 게임 시작 조건과 씬 전환 게이트 | 실제 게임용 Main 씬이 아직 없는 상태에서 잘못된 자동 전환을 만들지 않고, NGO 접속 인원과 호스트 권한을 기준으로 안전하게 씬 전환을 시작하는 구조 구현 | `NetworkGameFlowController.cs` 작성 및 `NetworkTest.unity`의 NetworkManager에 연결 | 최소 2명, 호스트 권한, 씬 이름, Build Settings 포함 여부를 모두 검증하도록 검토. 실제 Main 씬 이름과 UI 버튼 연결은 씬 제작 후 수행하도록 제한 기록 | `Assets/_Project/Scripts/Network/NetworkGameFlowController.cs`, `Assets/_Project/Scenes/Tests/NetworkTest.unity` |
| 2026-07-30 | Codex | 진미리 | Relay 세션 실패·퇴장·재시도 수명주기 보강 | 방 생성·참가 중 예외가 발생한 뒤 세션 참조가 남아 재시도가 막히는 경로와 이벤트 구독 해제 시점을 점검 | 세션 연결·해제를 `AttachSession`·`DetachSession`으로 통일하고 실패 세션 정리, 참가 코드 초기화, 서비스 초기화 재시도, 파괴 후 콜백 방지 추가 | 정상 퇴장 실패 시 재시도할 수 있도록 현재 세션을 유지하고, 생성·참가 실패 시에만 부분 세션을 정리하도록 검토 | `Assets/_Project/Scripts/Network/Tests/networkTestUI.cs` |
| YYYY-MM-DD |  |  |  |  |  |  |  |

## 7. AI 생성 코드 검증표

| 항목 | 확인 |
|---|---|
| 코드를 담당자가 설명할 수 있는가? | ☐ |
| Unity 컴파일 오류가 없는가? | ☐ |
| Play Mode에서 직접 테스트했는가? | ☐ |
| 멀티플레이 기능은 두 인스턴스에서 테스트했는가? | ☐ |
| 기존 기능이 깨지지 않았는가? | ☐ |
| 불필요한 패키지나 의존성이 추가되지 않았는가? | ☐ |
| 출처 확인이 필요한 코드·에셋이 없는가? | ☐ |
| 사용 프롬프트와 수정 내용을 기록했는가? | ☐ |

## 8. 최종 PDF에 추가할 내용

- 실제 사용한 AI 도구명과 버전 또는 서비스명
- AI가 관여한 전체 개발 흐름 그림
- 대표 프롬프트 3~5개
- AI 초안과 사람이 수정한 결과 비교
- AI 사용으로 단축된 작업과 한계
- 외부 에셋·오픈소스 목록
- AI가 게임 플레이 자체에 사용되었다면 런타임 구조
