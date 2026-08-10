# 척준경 캐릭터 에셋 기준

> 마지막 정리: **2026-08-10 KST**

## 캐릭터 선택 초상화

- 현재 사용 파일: `Assets/_Project/Art/Characters/CheokJunGyeong/Profile/CheokJunGyeong_CharacterSelect_512_v2.png`
- 이전 시안: `Assets/_Project/Art/Characters/CheokJunGyeong/Profile/CheokJunGyeong_CharacterSelect_512.png`
- 크기: 512×512
- 형식: RGBA PNG
- 배경: 완전 투명
- 알파: 0 또는 255만 사용
- Unity 임포트: Sprite, Point 필터, Mipmap 해제, 무압축
- 연결 데이터: `CheokJunGyeongData.asset`의 `portrait`

팀 제공 척준경 전신 설정화를 정체성 참조로 사용했다.
캐릭터 선택 화면에서는 가슴 위 흉상, 왼쪽을 향한 3/4 시점, 원형 흉부 문양과 대검의 상단부가 함께 보이도록 구성한다.

## 고정 팔레트

| 용도 | 색상 |
|---|---|
| 암철 그림자 | `#14141A` |
| 암철 기본 | `#24242C` |
| 암철 밝음 | `#3A3A45` |
| 강철 하이라이트 | `#6E7079` |
| 칼날 은색 | `#B9BFC9` |
| 칼날 광택 | `#E4E8EE` |
| 혈적 어두움 | `#4A1216` |
| 혈적 기본 | `#7A1F24` |
| 발광 적색 | `#E01B1B` |
| 발광 코어 | `#FF4A3A` |
| 피부 | `#C08A66` |
| 피부 그림자 | `#8A5A3E` |
| 머리 흑 | `#17161B` |
| 머리 하이라이트 | `#34313C` |

불투명 픽셀은 위 14색만 사용한다.
팔레트 밖의 중간색, 안티앨리어싱, 디더링과 반투명 픽셀을 사용하지 않는다.

## 기준 포즈 스프라이트

- 파일: `Assets/_Project/Art/Characters/CheokJunGyeong/CheokJunGyeong_Idle_Right_192.png`
- 프레임: 192×192
- 캐릭터·무기 불투명 경계 높이: 108px
- 불투명 경계: `x=73~116`, `y=52~159`
- 공통 바닥선: `y=159`
- 방향: 오른쪽
- 시점: 측면 기반의 약한 3/4 탑다운
- 자세: 양발을 바닥에 둔 대기 자세
- 무기: 양손으로 손잡이를 잡고 칼끝을 바닥선에 둔 대검
- Unity 임포트: Single Sprite, Point 필터, Mipmap 해제, 무압축, PPU 96, Bottom 피벗

기준 포즈는 이후 걷기·공격 스프라이트에서 신체 비율, 갑옷 실루엣, 바닥선과 무기 크기를 맞추는 기준으로 사용한다.

## 걷기 스프라이트 시트

- 파일: `Assets/_Project/Art/Characters/CheokJunGyeong/CheokJunGyeong_Walk_Right_8F_192.png`
- 전체 크기: 1536×192
- 구성: 192×192 프레임 8개, 가로 1행
- 프레임 순서: 왼발 접지, 다운, 패싱, 업, 오른발 접지, 다운, 패싱, 업
- 각 프레임 불투명 경계 높이: 108px
- 공통 바닥선: `y=159`
- 방향: 오른쪽
- 무기: 오른손 중심으로 칼끝을 아래에 둔 대검, 보폭에 따라 작은 흔들림
- 보조 움직임: 갑옷 치마판·붉은 술의 흔들림, 머리와 상투의 미세한 상하 이동
- Unity 임포트: Multiple Sprite, 192×192 Grid by Cell Size, Point 필터, Mipmap 해제, 무압축, PPU 96, Bottom 피벗

걷기는 가벼운 달리기가 아니라 무거운 갑옷의 체중 이동이 느껴지는 보행으로 유지한다.

## 철혈참 스프라이트 시트

- 파일: `Assets/_Project/Art/Characters/CheokJunGyeong/CheokJunGyeong_CheolhyeolSlash_Right_8F_192.png`
- 전체 크기: 1536×192
- 구성: 192×192 프레임 8개, 가로 1행
- 프레임 순서: 대기, 뒤로 체중 이동, 최대 준비, 전진 베기 시작, 수평 최대 신전, 회전 후속, 회복, 대기 복귀
- 각 프레임 불투명 경계 높이: 108px
- 공통 바닥선: `y=159`
- 방향: 오른쪽
- 발광: 4~6프레임의 칼날 중앙선에 `#FF4A3A` 비중을 높인다.
- Unity 임포트: Multiple Sprite, 192×192 Grid by Cell Size, Point 필터, Mipmap 해제, 무압축, PPU 96, Bottom 피벗

철혈참 본체 시트에는 검격 궤적, 에너지 아크, 잔상, 스파크와 파티클을 포함하지 않는다.
속성별 검격 FX는 흰색 기준의 별도 에셋으로 만들고 `ElementVisuals`가 런타임 색을 입히도록 구성한다.

## 철혈참 FX

- 파일: `Assets/_Project/Art/Combat/CheokJunGyeong/FX_CheolhyeolSlash_256.png`
- 크기: 256×256
- 형태: 좌측 중앙에서 +X 방향으로 펼쳐지는 120도 부채꼴 검격
- 색상: 가시 픽셀 RGB는 순백색 `#FFFFFF`만 사용
- 투명도: 알파 0/64/128/192/255의 단계형 표현
- Unity 임포트: Single Sprite, Point 필터, Mipmap 해제, 무압축, PPU 256
- Pivot: Custom `(0, 0.5)`
- 연결: `Skill_CheolhyeolSlash.prefab`의 SpriteRenderer
- 런타임 크기: 기본 `1.8 × 1.17`, 잔상은 기본 크기의 `×1.15`

`ElementVisuals.ApplyColor`가 SpriteRenderer의 기존 알파를 보존하고 RGB를 속성색으로 교체하므로, FX 원본에는 고유 색상을 넣지 않는다.

## 파진 FX

- 파일: `Assets/_Project/Art/Combat/CheokJunGyeong/FX_Pajin_512x256.png`
- 크기: 512×256
- 형태: 왼쪽의 끊어진 잔상에서 오른쪽의 넓고 조밀한 선두로 이어지는 수평 돌진 검격
- 중앙선: 얇은 투명 절단선으로 공기가 갈라진 형태 표현
- 색상: 가시 픽셀 RGB는 순백색 `#FFFFFF`만 사용
- 투명도: 알파 0/64/128/192/255의 단계형 표현
- Unity 임포트: Single Sprite, Point 필터, Mipmap 해제, 무압축, PPU 256
- Pivot: Center `(0.5, 0.5)`
- 연결: `Skill_Pajin.prefab`의 SpriteRenderer
- 런타임 크기: 기본 `3.5 × 1.2`, Lv2 폭 `×1.2`, Lv3 거리 `×1.25`

## 회천참 FX

- 파일: `Assets/_Project/Art/Combat/CheokJunGyeong/FX_Hwecheon_256.png`
- 크기: 256×256
- 형태: 네 변 중앙에 닿는 완전한 360도 외곽 링과 한 바퀴 감기는 내부 회전 스트로크
- 중심부: 완전 투명한 원형 공간
- 색상: 가시 픽셀 RGB는 순백색 `#FFFFFF`만 사용
- 투명도: 알파 0/64/128/192/255의 단계형 표현
- Unity 임포트: Single Sprite, Point 필터, Mipmap 해제, 무압축, PPU 256
- Pivot: Center `(0.5, 0.5)`
- 연결: `Skill_Hwecheon.prefab`의 SpriteRenderer
- 런타임 크기: 기본 지름 `4.4`, Lv5 마지막 파동 지름 `×1.45`

## 파성참 FX

- 파일: `Assets/_Project/Art/Combat/CheokJunGyeong/FX_Paseong_256.png`
- 크기: 256×256
- 형태: 중앙의 강한 내려베기 충격점, 외곽으로 뻗는 방사형 지면 균열, 네 변에 닿는 충격파 링
- 파편: 균열 사이의 분리된 작은 돌조각과 먼지 픽셀
- 색상: 가시 픽셀 RGB는 순백색 `#FFFFFF`만 사용
- 투명도: 알파 0/64/128/192/255의 단계형 표현
- Unity 임포트: Single Sprite, Point 필터, Mipmap 해제, 무압축, PPU 256
- Pivot: Center `(0.5, 0.5)`
- 연결: `Skill_Paseong.prefab`의 SpriteRenderer
- 런타임 위치·크기: 캐릭터 전방 `1.6`, 기본 지름 `3.8`, Lv5 두 번째 파동 지름 `×1.3`

## 검풍 FX

- 파일: `Assets/_Project/Art/Combat/CheokJunGyeong/FX_Geompung_256x128.png`
- 크기: 256×128
- 형태: 오른쪽을 향한 세로 초승달형 압축 공기 칼날과 왼쪽으로 뻗는 두 줄의 바람 잔상
- 색상: 가시 픽셀 RGB는 순백색 `#FFFFFF`만 사용
- 투명도: 본체 알파 255, 주요 잔상 128, 분절된 꼬리 64
- Unity 임포트: Single Sprite, Point 필터, Mipmap 해제, 무압축, PPU 256
- Pivot: Center `(0.5, 0.5)`
- 연결: `Skill_Geompung.prefab`의 SpriteRenderer
- 런타임 크기·판정: Transform `1.5 × 0.55`, 관통 2회

## 혈로 FX

- 파일: `Assets/_Project/Art/Combat/CheokJunGyeong/FX_Hyeollo_256.png`
- 크기: 256×256
- 형태: 네 변 중앙에 닿는 얇은 지속 오라 링, 안쪽을 향한 짧은 칼날 눈금, 성긴 내부 회전선
- 중심부: 캐릭터 가독성을 위해 대부분 완전 투명
- 색상: 가시 픽셀 RGB는 순백색 `#FFFFFF`만 사용
- 투명도: 외곽 알파 255, 안쪽 눈금 192, 내부 회전선 64
- Unity 임포트: Single Sprite, Point 필터, Mipmap 해제, 무압축, PPU 256
- Pivot: Center `(0.5, 0.5)`
- 연결: `Skill_Hyeollo.prefab`의 SpriteRenderer, 렌더러 알파 0.2
- 런타임 크기·지속: 기본 지름 `2.8`, Lv3 지름 `×1.15`, 4초/Lv2부터 5초

## 일기당천 FX

- 파일: `Assets/_Project/Art/Combat/CheokJunGyeong/FX_Ilgidangcheon_256.png`
- 크기: 256×256
- 형태: 조밀한 내부 폭주 링에서 외곽으로 솟는 각진 에너지 혀와 네 변에 닿는 압력 링
- 차별화: 혈로보다 가시 픽셀 밀도가 약 2.75배 높고 날카로운 방사형 실루엣 사용
- 중심부: 캐릭터 가독성을 위한 완전 투명 공간
- 색상: 가시 픽셀 RGB는 순백색 `#FFFFFF`만 사용
- 투명도: 알파 0/64/128/192/255의 단계형 표현
- Unity 임포트: Single Sprite, Point 필터, Mipmap 해제, 무압축, PPU 256
- Pivot: Center `(0.5, 0.5)`
- 연결: `Skill_Ilgidangcheon.prefab`의 SpriteRenderer, 렌더러 알파 0.25
- 런타임 크기·지속: 지름 `2.5`, 지속 6초, 정상 종료 마무리 참격 지름 `7.0`

## 척준경 스킬 FX 연결 완료

철혈참·파진·회천참·파성참·검풍·혈로·일기당천 7종의 `Square.png` 플레이스홀더를 모두 전용 순백색 알파 FX로 교체했다.
모든 FX는 `ElementVisuals.ApplyColor`의 런타임 속성색 치환을 위해 가시 RGB를 `#FFFFFF`로 제한한다.

## 스킬 아이콘 연결 대기

- 배치 경로: `Assets/_Project/Art/UI/SkillIcons/`
- 예상 원본: 128×128 아이콘 7개를 가로로 배치한 896×128 시트
- Unity 임포트: Sprite Mode Multiple, Point, Compression None, Mipmap 끔
- Sprite Editor: Grid by Cell Size 128×128로 7개 슬라이스
- 연결 대상: `Assets/_Project/ScriptableObjects/Skills/CheokJunGyeong/SD_*.asset` 7개의 `icon`

현재 저장소에는 척준경 스킬 아이콘 원본 시트가 없으므로 대상 폴더만 준비했다.
아이콘이 제공되기 전까지 7개 `SD_*`의 `icon` 필드는 연결하지 않는다.

## Unity 연결

- `CheokJunGyeong_Idle.anim`: 기준 포즈 1프레임 반복
- `CheokJunGyeong_Walk.anim`: 걷기 8프레임, Sample Rate 8, 반복
- `CheokJunGyeong_Animator.controller`: `IsMoving` Bool에 따라 Idle과 Walk 전환
- `Assets/_Project/Prefabs/Players/CheokJunGyeongVisual.prefab`: SpriteRenderer, Animator, CharacterWalkAnimator 구성
- `CheokJunGyeongData.asset.portrait`: `CheokJunGyeong_CharacterSelect_512_v2.png` 연결
- `CheokJunGyeongData.asset.gameplayVisualPrefab`: `CheokJunGyeongVisual.prefab` 연결

왼쪽 이동은 별도 스프라이트를 만들지 않고 `CharacterWalkAnimator`가 Visual Transform의 X Scale을 반전한다.
현재 연결은 에셋·GUID 정적 확인까지 완료했으며 캐릭터 선택과 Main 실제 실행 회귀가 남아 있다.

## 시각 기준

- 고려 시대 장군을 2179년 사이버 전사로 재해석한다.
- 검은 상투와 관자놀이의 잔머리를 유지한다.
- 각진 턱, 짙은 눈썹과 웃지 않는 강한 표정을 유지한다.
- 검은 찰갑의 직사각형 비늘 행, 높은 깃과 무거운 견갑을 명확히 보인다.
- 흉부 원형 문양과 갑옷 틈의 적색 발광선을 유지한다.
- 대검은 넓고 곧은 강철 칼날, 중앙 적색 발광선, 육각형 가드와 원형 문양을 유지한다.
- 대검은 왼손의 장갑 낀 손가락이 손잡이를 감싸 쥐고 어깨 옆에 세워 든 자세로 표현한다.
- 손, 손잡이, 가드와 칼날은 하나의 축으로 자연스럽게 이어지며 칼집이나 공중에 뜬 형태로 보이지 않아야 한다.
- 금색과 청색을 사용하지 않는다.
