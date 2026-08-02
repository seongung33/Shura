# Shura 외부 에셋 및 오픈소스 출처

> 마지막 저장소 정리: **2026-08-03 KST**
> 버전의 정본은 `Packages/manifest.json`, 실제 배포 전 최종 이용 조건은 각 공식 링크와 저장소 내 라이선스 파일을 다시 확인한다.

## 1. 기록 규칙

- 외부 파일을 프로젝트에 추가한 당일 원본 URL, 제공자, 이용 조건, 사용 위치와 수정 여부를 기록한다.
- 무료라는 설명만으로 판단하지 않고 공식 배포처 또는 동봉 라이선스 원문을 확인한다.
- 원본을 수정했거나 Unity용 파생 데이터·아틀라스를 만들었으면 둘 다 기록한다.
- 출처를 확인할 수 없는 이미지·음원·폰트·코드는 최종 빌드에서 사용하지 않는다.
- AI 생성 에셋은 도구, 프롬프트, 생성일, 사람의 편집과 사용 위치를 함께 기록한다.
- Unity Package Manager 항목은 패키지 ID와 정확한 버전, 제공자와 라이선스를 기록한다.

## 2. 외부 에셋·서체

| ID | 에셋 | 종류 | 제공자·원본 | 이용 조건 | 프로젝트 내 위치·사용 | 수정·확인 상태 | 담당 |
|---|---|---|---|---|---|---|---|
| A-001 | Maplestory Bold / Light | TTF 서체 | ㈜넥슨코리아, [메이플스토리 서체 공식 페이지](https://maplestory.nexon.com/Media/Font) | 개인·기업 무료 사용·배포 가능. 임의 수정·편집과 글꼴 자체 유료 판매 금지. 저작권 안내를 포함한 번들·임베딩 허용. 출처 표기 권장 | `Assets/TextMesh Pro/Fonts/Maplestory Bold.ttf`, `Maplestory Light.ttf`, 생성된 TMP SDF 에셋. HUD·UI용 | 원본 TTF는 그대로 보관. TMP용 SDF 생성. 저장소에 공식 저작권 안내 사본이 없으므로 제출 전 안내문 보존·표기 재확인 | 진미리 |
| A-002 | Liberation Sans | TTF 서체 | Liberation Fonts / Unity TextMesh Pro Essential Resources, [upstream](https://github.com/liberationfonts/liberation-fonts) | SIL Open Font License 1.1 | `Assets/TextMesh Pro/Fonts/LiberationSans.ttf`, TMP 기본·Fallback SDF | `Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt` 동봉 확인 | 전원 |
| A-003 | EmojiOne sample sprites | 이모지 스프라이트·JSON | EmojiOne 샘플, 동봉 `EmojiOne Attribution.txt` | 동봉 문서는 원 제공처와 별도 이용 조건 확인을 요구 | `Assets/TextMesh Pro/Sprites`, TMP Sprite Asset | 현재 게임 사용 여부 미확인. 최종 빌드에서 쓰지 않으면 제외하고, 사용 시 현재 공식 라이선스 재확인 | 전원 |

2026년 7월 31일 감사에서 `_Project/Art`와 `_Project/Audio` 아래의 실제 이미지·음원 파일은 확인되지 않았다. 현재 전투 화면은 Unity 기본·임시 그래픽 중심이며, 향후 외부 아트·사운드를 넣는 즉시 이 표를 갱신한다.

## 3. AI 생성 에셋

현재 저장소 감사에서는 AI로 생성했다고 확인되는 이미지·음원·3D 모델을 찾지 못했다. AI로 생성한 시각·음향 에셋을 추가하면 아래 형식으로 기록한다.

| ID | 에셋 | 생성 도구 | 주요 프롬프트 | 생성일 | 사람의 편집 | 사용 위치 | 담당 |
|---|---|---|---|---|---|---|---|
| AI-A-001 | 등록 전 |  |  |  |  |  |  |

AI 코딩 도구 사용은 에셋 표가 아니라 `06_AI_USAGE_TECHNICAL_DOCUMENT.md`에서 관리한다.

## 4. 주요 Unity·오픈소스 패키지

| ID | 패키지 | 버전 | 제공자·공식 위치 | 라이선스·약관 | 사용 목적 | 상태 |
|---|---|---|---|---|---|---|
| O-001 | `com.unity.inputsystem` | `1.19.0` | Unity Technologies, [Input System 저장소](https://github.com/Unity-Technologies/InputSystem) | Unity Companion License | 일반 플레이어 입력과 테스트 입력 | 사용 중 |
| O-002 | `com.unity.netcode.gameobjects` | `2.13.1` | Unity Technologies, [Netcode for GameObjects 저장소](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects) | Unity Companion License | 네트워크 오브젝트·소유권·이동 기술 검증 | 사용 중 |
| O-003 | `com.unity.services.multiplayer` | `2.3.0` | Unity Technologies, Unity Package Registry | [Unity Terms of Service](https://unity.com/legal/terms-of-service) | UGS 초기화, 인증, Relay 세션 생성·참가·퇴장 | 사용 중 |
| O-004 | `com.unity.render-pipelines.universal` | `17.3.0` | Unity Technologies, Unity Package Registry | Unity Companion License | Universal 2D 렌더링 | 사용 중 |
| O-005 | `com.unity.ugui` / TextMesh Pro 리소스 | `2.0.0` | Unity Technologies | Unity 패키지 이용 조건 및 각 동봉 리소스 라이선스 | Relay 로비, 로컬 HUD, 네트워크 HUD·결과 UI | 사용 중 |
| O-006 | `com.unity.ai.assistant` | `2.16.0-pre.1` | Unity Technologies, Unity Package Registry | [Unity 법적 약관](https://unity.com/legal) | 패키지 설치 확인. 실제 개발 사용 내역은 별도 AI 기록과 대조 필요 | 설치됨 |
| O-007 | `com.unity.ai.inference` | `2.6.1` | Unity Technologies, Unity Package Registry | [Unity 법적 약관](https://unity.com/legal) | 패키지 설치 확인. 런타임 추론 코드는 현재 없음 | 설치됨·미사용 |
| O-008 | Unity `.gitignore` template | 2025-12-18 동기화 표기 | [GitHub gitignore Unity template](https://github.com/github/gitignore/blob/main/Unity.gitignore) | CC0-1.0 | Unity 생성물·빌드·개인 설정 제외 | 사용 중 |

전체 직접·간접 패키지 목록과 정확한 의존성 버전은 `Packages/manifest.json`과 `Packages/packages-lock.json`을 따른다. `Library/PackageCache`의 라이선스 파일은 로컬 캐시이므로 제출 문서에는 공식 URL도 함께 유지한다.

## 5. 제출 전 확인표

- [x] 메이플스토리 서체 공식 배포처와 이용 조건 URL 기록
- [x] Liberation Sans OFL 원문이 저장소에 포함되어 있음
- [ ] 메이플스토리 서체 저작권 안내문을 저장소 또는 제출 자료에 보존
- [ ] EmojiOne 샘플을 실제 빌드에서 사용하는지 확인하고, 사용한다면 현재 라이선스 재검토
- [ ] 최종 아트·음원·폰트의 상업적 사용 가능 여부 확인
- [ ] 수정·파생 제작 가능 여부 확인
- [ ] 저작자 표시와 재배포 조건 확인
- [ ] 원본 파일을 저장소와 빌드에 포함할 수 있는지 확인
- [ ] Steam 출시에서도 같은 조건이 적용되는지 재확인
- [ ] AI 생성 에셋이 추가되면 도구·프롬프트·편집 기록 작성
- [ ] 최종 빌드에 포함된 패키지와 실제 사용하지 않는 패키지 정리

## 6. 금지 대상

- 출처가 없는 커뮤니티 재업로드 파일
- 검색 결과에서 직접 저장한 이미지
- 다른 게임에서 추출한 스프라이트·음원
- 상업적 이용이 금지된 폰트·음원
- 라이선스가 불명확한 AI 모델 또는 생성 에셋
- 구매 계정 외 팀원이 사용할 수 없는 유료 에셋의 무단 공유
