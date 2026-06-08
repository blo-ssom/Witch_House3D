# 졸업작품 진행 레포트

> 작성일: 2026-06-01 / 마감: 2026-06-17

---

## 2페이지 — 프로젝트 소개 및 개요

### 프로젝트명
**Witch House 3D** — 1인칭 시점 3D 공포 탈출 퍼즐 게임

### 장르 / 플랫폼
- 장르: 1인칭 호러 / 퍼즐 / 어드벤처
- 플랫폼: PC (Windows 스탠드얼론)
- 엔진: Unity 6 (6000.3.10f1), URP 17.3.0
- 목표 플레이타임: 약 17~20분 (1회차 클리어 기준)

### 컨셉
플레이어는 친구로부터 받은 의문의 편지를 들고 마녀가 살았다고 전해지는 폐가에 들어선다.
편지의 발신인이 친구가 아니라는 사실을 단서 조각으로 깨닫는 순간, 집은 플레이어를 가두고
방마다 흩어진 유품·기록·거울을 통해 친구의 흔적을 따라가게 만든다.
거울이라는 **하나의 모티프**를 게임 전반에 3단계(정보 → 위험 → 배반)로 빌드업하여,
같은 오브젝트에 다른 의미를 부여하는 방식으로 공포의 학습-배반 구조를 만든 것이 핵심 디자인이다.

### 진행 흐름
```
방1 액자 퍼즐 → Room3 키 → 샹들리에 이벤트 → 방2 출입 키
→ 방3 초상화 퍼즐 → 방2 사진 수집 → 메인홀 키
→ 2층 친구의 방 → 거울 폭발 + 추격 → 바닥 붕괴 → 지하 → 탈출
```

### 기술 스택
- C# 스크립트 약 30종 (PlayerScripts / ObjectScripts 디렉터리 구조)
- Unity AI Navigation (NavMeshAgent 기반 적 AI 추격)
- URP Lit 셰이더 기반 PBR 워크플로우
- 코루틴 기반 이벤트 시퀀싱, 싱글톤 매니저 + static event 통신

---

## 3페이지 — 본인 포지션 및 역할

### 포지션
**프로그래머 (단독)** — 본 프로젝트는 1인 개발 졸업작품으로, 기획·프로그래밍·레벨 디자인·라이팅·사운드 통합까지 전 영역을 직접 담당하지만,
레포트 본문은 핵심 기여 영역인 **프로그래밍**을 중심으로 서술한다.

### 담당 역할
1. **게임 시스템 아키텍처 설계**
   - 상호작용 시스템(`Interactable` 상속 구조), 인벤토리, 이벤트 통신 패턴 설계
   - 싱글톤 매니저(`NoteUI`, `FlipNoteUI`, `GameUI`, `GameOverManager`, `PhotoPuzzleManager`) 분리
   - static event(`OnNoteRead`, `OnPieceCollected`, `OnAllPiecesCollected`)로 결합도 낮춤
2. **플레이어 컨트롤러 구현**
   - `PlayerMove` / `PlayerLook` / `PlayerInteraction` / `PlayerInventory` / `PlayerFlashlight`
   - 1인칭 카메라, KeyType enum 기반 HashSet 키 관리
3. **퍼즐 / 이벤트 시퀀스 구현**
   - 방1 액자 퍼즐(`PaintingFlip`, `PaintingFallEvent`)
   - 방2 사진 조각 수집 + 분위기 변화(`PhotoPuzzleManager`, `Room2AtmosphereEvent`, `MemoryBox`)
   - 방3 초상화·석상 퍼즐(`PortraitCover`, `PortraitTracker`, `Room3StatueManager`)
4. **적 AI 및 추격 시퀀스**
   - `GhostChase` — NavMeshAgent 기반 추격 AI (4m/s, 감지 10m, 포착 1.2m)
   - `Floor2GhostEvent` / `UndergroundChaseEvent` 추격 트리거 + 바닥 붕괴(`FloorBreakTrigger`) 씬 전환
5. **UI / 연출 시스템**
   - `NoteUI`(단면 노트), `FlipNoteUI`(양면 메모 + R/우클릭 플립)
   - `IntroSequence`, `EndingSequence`, `Room2ExitCue` 등 카메라/페이드 연출
6. **문서화**
   - `CLAUDE.md`(아키텍처 가이드), `GAME_DESIGN.md`(전체 기획서)
   - `MIRROR_SETUP.md` / `MEMORYBOX_SETUP.md` / `FLOOR2_SETUP.md` (시스템별 셋업 가이드)

### 작업 방식
- 모든 주요 이벤트는 **코루틴(IEnumerator)** 기반으로 타이밍 제어
- 자식 콜라이더가 필요한 경우 **Helper Trigger 패턴**(`MirrorBranchInteract`, `Floor2NorthDoorTrigger`)으로 분리
- Unity의 "파일당 MonoBehaviour 1개" 규칙에 맞춰 헬퍼 클래스는 반드시 별도 .cs 파일로 분리

---

## 4페이지 — 현재까지 작업 내역

### 1) 코어 시스템 (완료)

| 시스템 | 핵심 스크립트 | 상태 |
|---|---|---|
| 플레이어 이동/시점/인벤토리 | `PlayerMove`, `PlayerLook`, `PlayerInventory` | 완료 |
| 상호작용 기반 클래스 | `Interactable`, `PlayerInteraction` | 완료 |
| 열쇠·문 잠금 시스템 | `KeyType`, `KeyItem`, `DoorInteract` | 완료 |
| 노트 UI (단면 / 양면) | `NoteUI`, `NoteItem`, `FlipNoteUI` | 완료 |
| HUD / 게임오버 | `GameUI`, `GameOverManager` | 완료 |
| 적 AI 추격 | `GhostChase` | 완료 |

### 2) 1층 (방1·방2·방3·메인홀) — 완료

- **방1 액자 퍼즐**: 액자를 뒤집어(`PaintingFlip`) 숨겨진 단서 노출 → Room3 키 획득
- **샹들리에 이벤트(`ChandelierEvent`)**: Room3 키 획득 시 자동 발동, 조명 깜빡임 → 낙하 → 잔해 → 방2 진입 키 등장
- **방3 초상화/석상 퍼즐**: 초상화 덮개(`PortraitCover`)를 벗기고 시선 추적(`PortraitTracker`)으로 위치를 맞춰 석상 정렬
- **방2 기록 보관실 (재설계 완료)**:
  - 거울 옵션 B(`MirrorRevealEvent`) 한 차례 구현 후, "거울 모티프 희석 방지" 판단으로 **방2에서 거울 폐기**
  - 사진 조각 3개 수집(`PhotoPuzzleManager`) → `MemoryBox` 자물쇠 해제 + 벽난로 소화
  - 양면 메모(앞면 친구 경고 / 뒷면 명부) → `FlipNoteUI`로 R키 플립
  - 메모 닫음 → 방 조명 다운 + 거울 손자국 + 메인홀 열쇠 등장

### 3) 2층 거울 미스디렉션 — 핵심 연출 (코드 완료)

거울이라는 단일 모티프를 **3단계로 점진적으로 빌드업**하는 본 작품의 핵심 시퀀스.

- **1단계 — 방2 거울 (정보)**: 페이크 텍스처 스왑으로 "거울 = 정보 노출" 학습 (※ 방2 재설계로 폐기, 손자국으로 대체)
- **2단계 — Mirror Branch 거울 (위험)**:
  - `FakeRoomMirrorEvent` + `MirrorBranchInteract` 헬퍼
  - 엄마 일기 노트 읽음 + 거울 E키 → 손자국 데칼 등장 + south 문 자동 해제(`DoorInteract.ForceUnlock`)
- **3단계 — 친구의 방 거울 (배반)**:
  - `Floor2MirrorEvent` (7단계 시퀀스)
  - 조건 3개(엄마 일기 + 메인 키 + 친구 메모) → south 문 잠금 → 조명 페이드 + 스포트라이트 → 거울 응시/뒤돌아봄/다시 응시 사이클 → north 문 개방 → `Floor2NorthDoorTrigger`로 거울 폭발 + `GhostChase.StartChase()`

### 4) 이벤트 통신 구조

```csharp
// static event 패턴으로 매니저 간 결합도 최소화
NoteItem.OnNoteRead          → Floor2MirrorEvent, Floor2GhostEvent 구독
PhotoPuzzleManager.OnPieceCollected   → Room2AtmosphereEvent 구독
PhotoPuzzleManager.OnAllPiecesCollected → MemoryBox 구독 (RevealKey 위임)
```

이벤트 발행자가 직접 `SolvePuzzle()`을 호출하지 않고 **RevealKey 책임을 구독자에게 위임**하는 구조로,
방2 분위기 변화·키 등장·메모 표시 같은 후속 연출을 자유롭게 갈아끼울 수 있도록 설계.

### 5) 작성한 주요 코드 (스크립트 30종 중 발췌)

`Floor2MirrorEvent.cs`, `FakeRoomMirrorEvent.cs`, `MemoryBox.cs`, `Room2AtmosphereEvent.cs`,
`PhotoPuzzleManager.cs`, `ChandelierEvent.cs`, `GhostChase.cs`, `MirrorBranchInteract.cs`,
`FlipNoteUI.cs`, `IntroSequence.cs`, `EndingSequence.cs` 등.

> 📎 **첨부 권장**: 위 스크립트들 중 `Floor2MirrorEvent.cs`(7단계 코루틴), `MemoryBox.cs`(이벤트 구독 + 자물쇠 해제), `GhostChase.cs`(NavMesh 추격 로직) 등을 코드 캡처로 첨부.
> 📎 **에디터 캡처 권장**: Unity 인스펙터에 보이는 `Floor2MirrorEvent` 슬롯 와이어링, 메인 씬(`WH.unity`) 하이어라키, NavMesh 베이크 결과.
> 📎 **인게임 캡처 권장**: 방2 양면 메모(앞/뒷면), Mirror Branch 손자국, 친구의 방 스포트라이트, 추격 장면.

---

## 5페이지 — 앞으로의 계획 및 구현 예정 기능

### 마감까지 남은 기간
**2026-06-17까지 D-16** — B 전략(*대충 다 만들고 폴리시*)으로 진행.
이번 주 안에 **처음~끝 클리어 가능 빌드 1개** 확보가 최우선 마일스톤.

### 단기 (~6/5)
1. **방2 닫기 (잔여 씬 작업)**
   - 폐기된 `Mirror_Room2` 오브젝트 + `MirrorRevealEvent` 컴포넌트 삭제
   - `PhotoPiece_3` 활성 상태로 천 덮개 가구 아래 위치 이동 (pieceID=2, Interact 레이어 + Collider)
   - 벽난로 불 Particle System 프리팹 드롭 (Unity Particle Pack)
2. **방3 석상 퍼즐 최종 와이어링** (`Room3StatueManager` 검증)
3. **2층 박스 메시 + NavMesh 베이크** — 친구의 방(16×20) / Chase Corridor(4×8)
4. **지하 추격 + 탈출 트리거** (`UndergroundChaseEvent`, `EscapeTrigger`) 완성

### 중기 (6/5 ~ 6/10) — Go 시 추가 구현
*Lock Your Door* 메커니즘 일부 차용 절충안을 **6/5 Go/No-Go 결정** 후 도입 여부 확정.
- 사운드 위협 시스템 (귀신 거리 기반 심박/속삭임 음원)
- 손전등 배터리 시스템 (`PlayerFlashlight` 확장)
- 단일 AI 페이즈 변화 (Stalker → Sprinter → Tank)
- 짧은 어둠 페이즈 1회 (지하 직전)

### 후기 (6/11 ~ 6/16) — 폴리시
- 통합 플레이테스트 (end-to-end 클리어 흐름)
- 조명 / Fog / SFX 통일
- 손자국·벽난로 텍스처 폴리시
- 사이트블로커 최종 배치 (책장 / 액자 / 콘솔테이블)
- 인트로(`IntroSequence`) / 엔딩(`EndingSequence`) 카메라 워크 정리

### 최종 (6/17)
- Windows 스탠드얼론 빌드 finalize
- README / 조작법 안내 / 데모 영상 캡처

### 구현 예정 기능 요약
| 기능 | 우선순위 | 상태 |
|---|---|---|
| 방2 씬 완료 (씬 작업만) | ★★★ | 코드 완료 / 씬 진행 중 |
| 2층 박스 메시 + NavMesh | ★★★ | 진행 중 |
| 지하 추격 + 탈출 | ★★★ | 코드 완료 / 씬 미구현 |
| end-to-end 클리어 빌드 | ★★★ | ~6/5 목표 |
| 사운드 위협 시스템 | ★★ | 6/5 Go 시 |
| 손전등 배터리 | ★★ | 6/5 Go 시 |
| 사운드/Fog/조명 폴리시 | ★ | 6/11~ |
| 빌드 finalize | ★★★ | 6/17 |

### 리스크 관리
- **씬 작업 병목**: 코드는 모든 핵심 시스템이 동작하지만, 씬에 슬롯 와이어링·콜라이더·라이트 배치가 남아 있음. 6/5까지 클리어 빌드 못 찍으면 절충안은 무조건 No-Go.
- **거울 모티프 디자인 충돌**: 페이크 방 잔상 AI와 어둠 페이즈 AI의 정체성 충돌 우려 → Go 결정 시 단일 AI로 통합.
- **마감 압박**: 폴리시 버퍼(6/11~6/16, 6일)는 확보 가능한 상태. 위험 단계 진입(6/15 이후) 시 절충안 기능부터 컷.
