# AI 사용 기록

프로젝트에서 AI 도구를 사용한 내역과 사람의 검토·수정 내용을 기록합니다.

| 날짜 | AI 도구 | 사용 목적 | 주요 프롬프트 | 생성 결과 | 사람이 수정한 내용 | 관련 커밋 |
| --- | --- | --- | --- | --- | --- | --- |
| 2026-07-26 | Claude Code | 플레이어 기본 이동 기능 구현 (`feat/player-movement`) | "PlayerTest 테스트 씬 생성, 임시 사각형 스프라이트 플레이어에 Rigidbody2D/Collider2D 추가, Unity Input System으로 WASD 이동 구현, 속도 Inspector 노출, 대각선 이동 속도 정규화, Prefab으로 저장" | `Assets/_Project/Scripts/Player/PlayerController.cs` 코드 전체 작성. Unity 에디터에서 만들 GameObject 구성/컴포넌트 세팅 단계별 가이드 제공 | 코드는 수정 없이 그대로 사용. 씬 생성, GameObject 구성, 컴포넌트 추가(Rigidbody2D, Box Collider 2D, Player Input), Prefab 생성, Play 테스트는 사람이 Unity 에디터에서 직접 수행. Prefab 저장 위치는 지시받은 `Prefabs/Player` 대신 기존 프로젝트 컨벤션인 `Prefabs/Players`(복수형)로 변경 | 3408a2b |
