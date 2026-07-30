# Shura 프로젝트 문서 안내

## 프로젝트 개요

- 프로젝트 코드명: **Shura**
- 장르: 2D 탑다운 협동 생존 액션
- 핵심 경험: 여러 플레이어의 스킬이 순서와 조합에 따라 연계 효과를 일으키는 협동 전투
- 단기 목표: 2026년 8월 10일까지 NAN 2026 사전 과제로 제출 가능한 빌드 완성
- 장기 목표: 최대 4인 온라인 협동과 Steam 출시
- 엔진: Unity 6.3 LTS 계열, Universal 2D
- 팀 구성: 3명(개발 2명, 콘텐츠·아트·QA·문서 1명)

## 문서 목록

| 파일 | 목적 | 주 담당 |
|---|---|---|
| `01_GAME_DESIGN_DOCUMENT.md` | 게임의 재미, 규칙, 콘텐츠 방향 정의 | 전원 |
| `02_MVP_SCOPE_AND_ROADMAP.md` | 해커톤에서 만들 것과 버릴 것 구분 | 전원 |
| `03_TEAM_ROLES_AND_WORKFLOW.md` | 역할, 협업, Git·Unity 작업 규칙 | 전원 |
| `04_TECHNICAL_DESIGN.md` | Unity 구조와 멀티플레이 기술 방향 | 개발자 2명 |
| `05_DAILY_PLAN_2026-07-25_to_08-10.md` | 마감일까지 날짜별 실행 계획 | 전원 |
| `06_AI_USAGE_TECHNICAL_DOCUMENT.md` | AI 사용 구조와 프롬프트 기록 | AI 문서 담당 |
| `07_ASSET_SOURCES.md` | 외부 에셋 출처와 라이선스 기록 | 콘텐츠 담당 |
| `08_DECISION_LOG.md` | 주요 결정과 변경 이유 기록 | 회의 기록 담당 |
| `09_HACKATHON_SUBMISSION_CHECKLIST.md` | NAN 2026 필수 제출물 점검 | 제출 담당 |
| `10_TEST_PLAN.md` | 기능·멀티플레이·빌드 QA 기준 | QA 담당 |
| `11_CONCEPT_AND_HERO_DESIGN.md` | 확정 콘셉트, 속성 시스템, 영웅 설계 (최신 기준) | 전원 |
| `GUIDE_JUMONG_SKILLS_SETUP.md` | 주몽 스킬 Unity 세팅 절차 (프리팹·컴포넌트) | 스킬 담당 |
| `IMPL_2026-07-31_JUMONG_SKILLS.md` | 기본공격·P0 스킬 3종 구현·테스트 기록 | 스킬 담당 |
| `HANDOFF_2026-07-30.md` | 세션 인수인계 (결정·미결 정리) | 전원 |

## 문서 운영 규칙

1. 결정한 내용은 Discord 대화에만 남기지 않고 관련 문서에 반영한다.
2. `08_DECISION_LOG.md`에는 방향이 바뀐 결정도 삭제하지 않고 새 행으로 남긴다.
3. AI를 사용한 작업은 작업 직후 `06_AI_USAGE_TECHNICAL_DOCUMENT.md`에 기록한다.
4. 외부 이미지·음원·폰트·코드는 다운로드 직후 `07_ASSET_SOURCES.md`에 기록한다.
5. 매일 작업 종료 전에 일정표의 완료 여부와 다음 날 우선순위를 갱신한다.
6. 해커톤 제출용 PDF는 이 Markdown 문서들을 기반으로 8월 8일부터 작성한다.

## 현재 가장 중요한 원칙

> 4인 멀티플레이 전체를 먼저 만들지 않는다.  
> 플레이어 이동 → 전투 → 적과 웨이브 → 스킬 연계 → 2인 협동 → 빌드 순서로 작동하는 한 판을 완성한다.