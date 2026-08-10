# Shura 프로젝트 문서 안내

> 마지막 저장소 대조: **2026-08-10 KST** (`origin/develop` `e20c03c`)

## 프로젝트 개요

- 프로젝트 코드명: **Shura**
- 장르: 2D 탑다운 2인 협동 생존 액션
- 핵심 경험: 서로 다른 플레이어의 속성 스킬을 연결해 일반 공격과 다른 연계 반응을 만드는 전투
- 단기 목표: 2026년 8월 10일까지 NAN 2026 사전 과제로 제출 가능한 빌드 완성
- 장기 목표: 최대 4인 온라인 협동과 Steam 출시
- 엔진: Unity `6000.3.20f1`, Universal 2D

## 문서별 정본 역할

같은 내용을 여러 문서에서 상태표로 반복하지 않는다. 현재 상태는 `12`, 코드 계약은 `04`, 검증 결과는 `10`을 기준으로 한다.

| 문서 | 정본 역할 |
|---|---|
| `00_DOCS_INDEX.md` | 문서 구조와 운영 규칙 |
| `01_GAME_DESIGN_DOCUMENT.md` | 핵심 경험, 게임 루프, 규칙과 콘텐츠 방향 |
| `02_MVP_SCOPE_AND_ROADMAP.md` | 제출 범위, 성공 기준, 장기 로드맵 |
| `03_TEAM_ROLES_AND_WORKFLOW.md` | 역할, Git·Unity 협업 방식, 완료 정의 |
| `04_TECHNICAL_DESIGN.md` | 런타임 구조, 코드 계약, 네트워크 권한과 씬 책임 |
| `05_DAILY_PLAN_2026-07-25_to_08-10.md` | 날짜별 계획과 실제 진행 이력 |
| `06_AI_USAGE_TECHNICAL_DOCUMENT.md` | AI 사용 내역과 사람의 검토·통합·검증 기록 |
| `07_ASSET_SOURCES.md` | 외부 에셋·서체·패키지 출처와 이용 조건 |
| `08_DECISION_LOG.md` | 확정 결정, 대체 관계와 미결 결정 |
| `09_HACKATHON_SUBMISSION_CHECKLIST.md` | 제출물과 링크 준비 상태 |
| `10_TEST_PLAN.md` | 테스트 절차와 현재 검증 증거 |
| `11_CONCEPT_AND_HERO_DESIGN.md` | 7속성, 성장 규칙, 주몽과 영웅 설계 |
| `12_CURRENT_PROJECT_STATUS.md` | **현재 브랜치·작업트리·구현·위험·다음 순서의 단일 정본** |
| `13_GAME_BALANCE_AND_STAGE_DESIGN.md` | 15분 런·팀 성장·웨이브·성능 기준 |

보조 문서:

| 문서 | 용도 |
|---|---|
| `GUIDE_JUMONG_SKILLS_SETUP.md` | 주몽 스킬 Unity 세팅 절차 |
| `IMPL_2026-07-31_JUMONG_SKILLS.md` | 7월 31일 주몽 스킬 구현·테스트 기록 |
| `HANDOFF_2026-07-30.md` | 7월 30일 시점의 보관용 인수인계 기록 |
| `11_CHARACTER_SKILL_SYSTEM_CONCEPT.md` | 폐기된 중복 문서의 안내 파일. 내용 정본은 `11_CONCEPT_AND_HERO_DESIGN.md` |
| `Playable_character/cheok_jungyeong/13_CHEOK_JUNGYEONG_CHARACTER_SKILLS(1).md` | 척준경 상세 설계 복원 기록 |
| `Boss/README_JANGSANBEOM_ASSETS.md` | 장산범 에셋 제작·임포트 기록 |
| `ConceptArt/Jinmiri_FinalPolish/README.md` | 픽셀 콘셉트·변환 원본과 적용 기록 |

`docs` 루트의 `game-design.md`, `decisions.md`, `asset-sources.md`, `ai-usage-log.md`는 정본 위치만 안내하는 호환용 파일이다.

## 상태 표기

- **구현:** 코드·데이터·프리팹 또는 씬에 핵심 흐름이 존재한다.
- **검증:** 명시된 커밋·빌드·환경에서 절차와 결과를 확인했다.
- **부분 구현:** 단독 기능은 있으나 목표 흐름, 콘텐츠 또는 네트워크 연결이 남았다.
- **로컬 작업 중:** 커밋되지 않은 작업트리 변경이다.
- **미구현:** 현재 프로젝트 파일에서 해당 책임을 확인하지 못했다.

“구현”과 “검증”은 다르다. 기능 상태는 `12_CURRENT_PROJECT_STATUS.md`, 실제 테스트 결과는 `10_TEST_PLAN.md`에서 관리한다.

## 운영 규칙

1. 현재 진행 상황과 다음 우선순위는 `12_CURRENT_PROJECT_STATUS.md`에만 작성한다.
2. 코드 구조와 공개 계약은 `04_TECHNICAL_DESIGN.md`에만 작성하고 현황표를 복제하지 않는다.
3. 테스트 결과는 `10_TEST_PLAN.md`에 기록하고 다른 문서에는 증거 링크만 남긴다.
4. 결정이 바뀌면 `08_DECISION_LOG.md`에 새 결정을 추가하고 대체 관계를 기록한다.
5. AI 작업은 `06_AI_USAGE_TECHNICAL_DOCUMENT.md`, 외부 에셋은 `07_ASSET_SOURCES.md`에 기록한다.
6. 데미지 코드는 구체 Health 클래스를 직접 수정하지 않고 `IDamageable.TakeDamage`를 사용한다.
7. 공용 씬·프리팹·ScriptableObject 변경 전에는 작업트리와 다른 담당자의 변경을 확인한다.
8. 문서와 코드가 다르면 스크립트·프리팹·씬·ScriptableObject·Git 상태를 다시 대조해 정본 문서를 갱신한다.

> 기능 수보다 실제로 이어지는 한 판을 우선한다.
