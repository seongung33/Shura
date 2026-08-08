# 장산범 보스 도트 에셋

> 2026-08-09 정적 확인: 런타임 에셋은 `Assets/_Project/Art/Enemies/Jangsantiger/`에 있고 `BossJangsanTiger.prefab`이 `StageTest`와 `Main`에서 참조된다. Play Mode 보스 전투·승리 검증은 별도 필요하다.

## 포함 파일

| 파일 | 용도 | 크기 |
|---|---|---:|
| `jangsanbeom_boss_128.png` | 장산범 본체 기본 도트 | 128×128 |
| `jangsanbeom_boss_256.png` | 확대 화면·고해상도용 본체 도트 | 256×256 |
| `jangsanbeom_hit_fx_4x64.png` | 흰 털이 튀는 피격 이펙트, 가로 4프레임 | 256×64 |

모든 PNG는 투명 배경이다. 기존 상대 로봇 캐릭터 도트와 변신 프레임은 포함하지 않는다.

## Unity 권장 임포트 설정

### 장산범 본체

- Texture Type: `Sprite (2D and UI)`
- Sprite Mode: `Single`
- Filter Mode: `Point (no filter)`
- Compression: `None`
- Generate Mip Maps: 끔
- Pixels Per Unit: 기존 캐릭터 도트와 같은 값 사용
- `128px` 버전을 우선 사용하고, 화면에서 디테일이 너무 뭉개질 때 `256px` 버전으로 교체

### 피격 이펙트

- Texture Type: `Sprite (2D and UI)`
- Sprite Mode: `Multiple`
- Sprite Editor → Slice → Grid by Cell Size: `64 × 64`
- 총 4프레임, 왼쪽에서 오른쪽 순서
- Filter Mode: `Point (no filter)`
- Compression: `None`
- Generate Mip Maps: 끔
- Animation Samples: `20` 권장
- Loop Time: 끔
- 전체 재생 시간: 약 `0.20초`

피격 지점이나 장산범 중심에 생성하고, 장산범보다 Sorting Order를 1 높게 두면 된다.

## 디자인 기준

- 2D 탑다운에서 구분되는 높은 어깨와 긴 앞다리 실루엣
- 긴 은백색 털 아래에 남은 어두운 로봇 골격과 청록색 기계광
- 털에 가려진 검은 얼굴과 두 개의 적안
- 길고 크게 휘어진 흰 꼬리
- 일반 백호처럼 보이지 않도록 호랑이 줄무늬와 주황색을 배제
- 피격 시 피나 연기 대신 은백색 털 조각만 흩어짐

## 생성 기록

- 생성일: 2026-08-03
- 방식: OpenAI 이미지 생성으로 픽셀 아트 원본 제작
- 후처리: 단색 크로마키 제거, Point 필터 축소, 64×64 프레임 슬라이스
- 본체 프롬프트 요약: `2179년 사이버펑크 장산범, 로봇 골격 위 긴 은백색 털, 검은 얼굴과 두 적안, 긴 꼬리, 탑다운 3/4 시점 픽셀 아트`
- 피격 FX 프롬프트 요약: `장산범 몸 없이 흰 털이 충격점에서 터져 퍼지고 사라지는 4프레임 픽셀 아트`
