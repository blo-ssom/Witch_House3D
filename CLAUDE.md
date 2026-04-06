# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

**Witch House 3D** — Unity 6 (6000.3.10f1) 기반 1인칭 3D 공포 탈출 퍼즐 게임. URP 17.3.0 사용.
게임 기획 상세는 `GAME_DESIGN.md` 참조.

## 빌드 & 실행

- Unity 6 (6000.3.10f1) 에디터에서 열기
- 메인 씬: `Assets/Scenes/WH.unity` (1층+2층), 지하 씬: `Assets/Scenes/UnderGround.unity`
- 렌더 파이프라인: URP (`Assets/Settings/PC_RPAsset.asset`)
- NavMesh: AI Navigation 2.0.10 패키지 사용, 에디터에서 베이크 필요

## 코드 아키텍처

### 스크립트 구조
```
Assets/
├── PlayerScripts/    # 플레이어 관련 (이동, 카메라, 상호작용, 인벤토리)
└── ObjectScripts/    # 게임 오브젝트 로직 (퍼즐, 이벤트, UI, 적 AI)
```

### 핵심 설계 패턴

**상호작용 시스템 (Interactable 상속 구조)**
- `Interactable` (base class) → `KeyItem`, `NoteItem`, `PhotoPiece`, `PaintingFlip`, `PortraitCover`
- `PlayerInteraction`이 3m Raycast로 감지 → E키 입력 시 `Interact(PlayerInventory)` 호출
- 새 상호작용 오브젝트 추가 시 반드시 `Interactable`을 상속하고 `Interact()`, `GetInteractPrompt()` 오버라이드

**싱글톤 매니저**
- `NoteUI.Instance` — 노트 표시 UI
- `GameUI.Instance` — HUD 상호작용 프롬프트
- `GameOverManager.Instance` — 게임오버 처리
- `PhotoPuzzleManager.Instance` — 방2 퍼즐 상태

**이벤트 기반 통신**
- `NoteItem.OnNoteRead` (static event) — 노트 읽기 완료 시 발행
- `Floor2GhostEvent`가 이 이벤트를 구독하여 귀신 등장 트리거

**열쇠 시스템**
- `KeyType` enum: None, Room3, PathToRoom2, Room2, MainHall, Floor2
- `PlayerInventory`가 `HashSet<KeyType>`으로 관리
- `DoorInteract`가 `PlayerInventory.HasKey()`로 문 잠금 확인

### 이벤트/퍼즐 시퀀싱
모든 주요 이벤트는 **코루틴(IEnumerator)** 기반으로 타이밍 제어:
- `ChandelierEvent` — Room3 키 획득 시 자동 발동 (조명 깜빡임 → 낙하 → 잔해)
- `FloorBreakTrigger` — 바닥 붕괴 → 1.5초 낙하 → 페이드아웃 → UnderGround 씬 로드
- `Floor2GhostEvent` — 열쇠 + 노트 조건 충족 시 문 잠금 → 귀신 페이드인 → 추격 시작

### 적 AI
- `GhostChase` — NavMeshAgent 기반, 4m/s 추격, 10m 감지, 1.2m 포착 거리
- `StartChase()` 호출로 추격 시작, 포착 시 `GameOverManager` 호출

## 씬 구성

| 씬 | 용도 |
|---|---|
| `WH.unity` | 메인 게임 (1층 허브 + 방1~3 + 2층) |
| `UnderGround.unity` | 지하 (2차 추격 + 탈출) |
| `SampleScene.unity` | 테스트용 |

## 진행 흐름 (열쇠-문 체인)
```
방1 액자 퍼즐 → Room3 키 → 샹들리에 이벤트 → PathToRoom2 키
→ 방3 초상화 퍼즐 → Room2 키 → 방2 사진수집 → MainHall 키
→ 2층 친구의 방 → 추격 → 바닥 붕괴 → 지하 → 탈출
```

## 코드 작성 시 주의사항

- C# 스크립트는 용도에 따라 `PlayerScripts/` 또는 `ObjectScripts/`에 배치
- 상호작용 가능 오브젝트는 `Interactable` 상속 필수
- UI 접근은 싱글톤 Instance를 통해 (`NoteUI.Instance`, `GameUI.Instance` 등)
- 씬 전환은 `UnityEngine.SceneManagement.SceneManager.LoadScene()` 사용
- 귀신 투명도 제어 시 머티리얼 blend mode를 런타임에 변경하는 패턴 사용 (Floor2GhostEvent 참조)
- 텍스처는 2K 해상도 통일, PBR 워크플로우 (URP Lit 셰이더)
- 프리팹: `Assets/Prefebs/` (Door, Door_Pivot, Key)
