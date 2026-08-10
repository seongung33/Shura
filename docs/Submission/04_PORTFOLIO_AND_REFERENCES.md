# 무궁(MUGUNG) — 포트폴리오 및 참고자료

## 1. 프로젝트 한눈에 보기

무궁은 세 명이 Unity 6.3 LTS로 개발한 2D 탑다운 생존 액션 게임이다. 한국사 영웅인 주몽과 척준경, 설화 속 장산범을 2179년 사이버펑크 세계에 배치하고, 싱글과 Relay 기반 온라인 2인 협동을 하나의 플레이 구조로 만들었다.

### 프로젝트 정보

| 항목 | 내용 |
|---|---|
| 개발 기간 | 2026-07-25 ~ 2026-08-10 |
| 인원 | 3명 |
| 엔진·언어 | Unity 6.3 LTS, C# |
| 네트워크 | Netcode for GameObjects, Unity Multiplayer Services/Relay |
| UI | Unity UI, TextMesh Pro, 런타임 반응형 UI |
| 배포 목표 | Web |
| 형상 관리 | GitHub, 기능 브랜치, Pull Request |

## 2. 대표 결과물

### 완결된 생존 액션 루프

메뉴에서 영웅을 선택하고, 적을 처치해 경험치를 얻고, 팀 레벨업으로 성장하며, 15분 뒤 장산범을 상대하는 시작·성장·보스·결과 흐름을 구성했다.

### 역할이 분명한 협동 전투

주몽은 원거리 투사체, 척준경은 근거리 돌파에 집중한다. 팀 경험치와 속성 연계, 필살기를 통해 두 플레이어가 같은 목표를 공유하면서도 서로 다른 판단을 하게 설계했다.

### Relay 기반 온라인 2인

방을 만든 플레이어가 6자리 코드를 공유하고, 참가자가 코드를 입력해 접속한다. 영웅 선택, 전투 진입, 네트워크 소유권, 퇴장과 복귀를 공용 흐름으로 구성했다.

### 한국 설화 × 픽셀 사이버펑크 비주얼

미래 한국 도시, 적청 무궁화, 한글 `무궁` 로고, 설화 기반 적과 장산범을 공통 색과 픽셀 스타일로 맞췄다. 생성 이미지에는 배경 제거, 팔레트 축소, Point 필터, 셀 분리 등의 후처리를 적용했다.

## 3. 기술적 문제 해결

| 문제 | 해결 방향 |
|---|---|
| 싱글과 멀티가 다른 플레이어 생성 경로 사용 | 캐릭터 선택 상태와 네트워크 플레이어 적용 계약 통합 |
| 멀티에서 근접 피해가 0이 되거나 중복될 가능성 | 서버 권한 피해 판정과 클라이언트 시각 연출 분리 |
| 많은 적의 네트워크·Web 성능 부담 | 동시 생존 수 상한, 역할별 웨이브와 성능 검증 관문 설계 |
| 작은 화면에서 적·텍스트 구분이 어려움 | 실루엣·색·크기 차이, 4프레임 워크, 외곽선과 대비 강화 |
| 팀원의 씬 동시 수정 충돌 | 담당 씬 구분, 런타임 UI 구성, 작은 PR과 최신화 원칙 적용 |
| AI 결과의 품질·근거 불확실성 | 실제 화면 검수, 후처리, 도구·날짜·사용 위치·라이선스 기록 |

## 4. 포트폴리오 링크

- Web 플레이: **[Web 플레이 URL 입력]**
- GitHub: **[GitHub URL 입력]**
- 시연 영상: **[YouTube 또는 영상 URL 입력]**
- 최종 발표 자료: **[발표 자료 URL 입력]**
- 빌드·릴리스: **[릴리스 URL 입력]**

## 5. 권장 캡처 구성

1. 한글 `무궁` 로고가 개화하는 첫 화면
2. 미래 한국 도시 배경과 메인 메뉴
3. 주몽·척준경 영웅 선택 비교
4. 일반 적 3종과 4프레임 이동
5. 전투 HUD와 팀 레벨업 선택
6. 주몽·척준경 필살기 컷인
7. 장산범 등장과 보스 체력바
8. Relay 참가 코드와 2인 로비
9. 승리·패배 및 재시작 화면
10. Web 브라우저 실행 화면

## 6. 실제 포함 에셋과 라이선스

| 자료 | 출처·라이선스 | 사용 위치 |
|---|---|---|
| Maplestory Bold / Light | 넥슨 공식 배포 조건에 따라 사용, 출처 표기 권장 | 한글 UI와 HUD |
| Liberation Sans | SIL Open Font License 1.1 | 보조·대체 글꼴 |
| EmptyCity | yd, OpenGameArt, CC0 | 메뉴·로비 BGM |
| Friendly Talk On a Robotic Battlefield | illin, OpenGameArt, CC0 | 전투 BGM |
| Unity 패키지 | Unity Technologies, 각 Unity 이용 조건 | 엔진·UI·입력·네트워크·Relay |
| AI 생성·팀 후처리 이미지 | 생성 도구·날짜·프롬프트 요약을 별도 에셋 문서에 기록 | 로고·배경·적·보스·연출 |

상세 경로와 후처리 내역은 [에셋 출처 문서](../Project/07_ASSET_SOURCES.md)에 기록한다.

## 7. 제작 도구·공식 참고자료

- [Unity Manual](https://docs.unity3d.com/Manual/index.html)
- [Netcode for GameObjects](https://docs-multiplayer.unity3d.com/netcode/current/about/)
- [Unity Relay](https://docs.unity.com/ugs/en-us/manual/relay/manual/introduction)
- [Maplestory 서체 공식 페이지](https://maplestory.nexon.com/Media/Font)
- [Image-to-Pixel](https://tezumie.github.io/Image-to-Pixel/)
- [EmptyCity — OpenGameArt](https://opengameart.org/content/emptycity-background-music)
- [Friendly Talk On a Robotic Battlefield — OpenGameArt](https://opengameart.org/content/friendly-talk-on-a-robotic-battlefield-looped)

## 8. 참고와 실제 사용의 구분

상용·기존 게임은 UI 정보 우선순위, 짧은 전투 피드백과 협동 생존 장르의 사용성을 연구하는 참고 대상으로만 활용한다. 해당 게임의 그래픽·음원·코드를 프로젝트에 포함하지 않는다. 제출 PDF에는 실제 포함 에셋의 출처와 라이선스를 우선 표기하고, 영감의 출처는 별도 ‘디자인 참고’로 구분한다.

## 9. 제출 전 최종 확인

- [ ] 모든 링크가 외부 환경에서 열리는지 확인
- [ ] 캡처에 Unity Console 오류나 개인정보가 보이지 않는지 확인
- [ ] Web 화면과 실제 최종 로고·배경이 일치하는지 확인
- [ ] 음원·서체·AI 에셋 출처와 라이선스 문구 재확인
- [ ] 미검증 기능을 완료로 표현하지 않았는지 확인
- [ ] PDF 글자가 깨지지 않고 링크가 클릭되는지 확인

