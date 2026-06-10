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
- `Interactable` (base class) → `KeyItem`, `NoteItem`, `PhotoPiece`, `PaintingFlip`, `PortraitCover`, `TableClothInteract`, `MirrorRevealEvent`, `MemoryBox`, `DoorInteract`
- `PlayerInteraction`이 3m Raycast로 감지 → E키 입력 시 `Interact(PlayerInventory)` 호출
- 새 상호작용 오브젝트 추가 시 반드시 `Interactable`을 상속하고 `Interact()`, `GetInteractPrompt()` 오버라이드

**싱글톤 매니저**
- `NoteUI.Instance` — 단면 노트 표시 UI
- `FlipNoteUI.Instance` — 양면 메모 UI (Sprite 2장 + R/우클릭 플립 + 닫힘 콜백)
- `GameUI.Instance` — HUD 상호작용 프롬프트
- `GameOverManager.Instance` — 게임오버 처리
- `PhotoPuzzleManager.Instance` — 방2 퍼즐 상태

**이벤트 기반 통신**
- `NoteItem.OnNoteRead` (static event, string noteID) — 노트 읽기 완료 시 발행
- `PhotoPuzzleManager.OnPieceCollected` (int id) / `OnAllPiecesCollected` — `Room2AtmosphereEvent`, `MemoryBox`가 구독
- 구독자가 있는 이벤트(예: `OnAllPiecesCollected`)는 `PhotoPuzzleManager`가 직접 `SolvePuzzle()` 호출하지 않고 구독자에게 RevealKey 책임을 위임
- `Floor2GhostEvent` (1차 안), `Floor2MirrorEvent` (7단계 정식) 모두 `NoteItem.OnNoteRead` + `PlayerInventory.HasKey`로 조건 추적
- 둘 다 두면 충돌 가능 — 친구의 방엔 한쪽만 부착할 것

**Helper Trigger 패턴**
- 매니저 컴포넌트가 OnTriggerEnter를 직접 받기 어려운 경우 (자식 콜라이더 필요) `MirrorBranchInteract`, `Floor2NorthDoorTrigger` 같은 별도 헬퍼 컴포넌트를 자식 GameObject + BoxCollider(IsTrigger)에 부착하여 매니저의 메소드 호출
- ⚠️ 헬퍼는 **반드시 자기 이름의 .cs 파일로 분리** (파일명 = 클래스명). Unity는 파일당 MonoBehaviour 1개만 인식하므로 매니저와 같은 파일에 *동봉하면 보조 헬퍼가 Add Component 메뉴에 안 뜸* → 씬에서 못 붙임. (2026-05-25 `MirrorBranchInteract`/`Floor2NorthDoorTrigger`를 동봉 → 별도 파일로 분리)

**열쇠 시스템**
- `KeyType` enum: None, PathToRoom1, PathToRomm3, Room3, PathToRoom2, Room2, MainHall, Floor2, DrawerSmall
- `DrawerSmall`은 페이크 방 작은 열쇠 — 친구의 방 책상 서랍(DoorInteract) 해제용
- `PlayerInventory`가 `HashSet<KeyType>`으로 관리
- `DoorInteract`가 `PlayerInventory.HasKey()`로 문 잠금 확인

### 이벤트/퍼즐 시퀀싱
모든 주요 이벤트는 **코루틴(IEnumerator)** 기반으로 타이밍 제어:
- `ChandelierEvent` — Room3 키 획득 시 자동 발동 (조명 깜빡임 → 낙하 → 잔해)
- `FloorBreakTrigger` — 바닥 붕괴 → 1.5초 낙하 → 페이드아웃 → UnderGround 씬 로드
- `Floor2GhostEvent` — 열쇠 + 노트 조건 충족 시 문 잠금 → 귀신 페이드인 → 추격 시작
- `Room2MirrorEvent` — 거울 3사이클 미스디렉션 연출 (구버전). `Floor2MirrorEvent`로 대체됨, 폐기 검토
- `Room2ExitCue` — P.T.식 퇴장 여운. Room2MirrorEvent와 세트 (현재 보류)
- `Room2AtmosphereEvent` — 방2 MVP 연출. `PhotoPuzzleManager.OnPieceCollected`/`OnAllPiecesCollected` 구독. 조각별 해프닝 (조명 깜빡임/책 떨어짐/불꽃 튐 + 엄마 일기 NoteUI) + 완성 후 사진 텍스처 전환. `useMemoryBox=true`이면 RevealKey 호출 위임
- `MemoryBox` — 방2 기록 보관실 보관함 (Interactable). `OnAllPiecesCollected` 구독 → 자물쇠 해제 → E키로 `FlipNoteUI` 자동 진입 → 닫힘 콜백에서 분위기 변화(벽난로 끔/거울 손자국/방 조명 다운) + `PhotoPuzzleManager.RevealKey()`
- `FakeRoomMirrorEvent` — 2층 거울 잔상 (Visage식). **2026-05-22 구조 변경: 페이크 방 → Mirror Branch dead-end로 이동.** `MirrorBranchEvent`로 리네임 예정. 거울 E키 트리거 → 잔상 → 친구의 방 south 문 `ForceUnlock()` + 작은 열쇠 활성화. 자식 `FakeRoomDoorProximity` 헬퍼는 폐기 (dead-end이므로 출입문 없음)
- `Floor2MirrorEvent` — 2층 친구의 방 7단계 시퀀스 (정식). south 문은 처음 잠김 (Mirror Branch 거울 잔상이 해제 트리거). 조건 3개(엄마 일기 + 메인 키 + 친구 메모) → south 잠금 → 조명 페이드 + 스포트라이트 → 거울 응시/뒤돌아봄/다시 응시 사이클 → north 개방 → `Floor2NorthDoorTrigger` 헬퍼 발동 → 거울 폭발 + `GhostChase.StartChase()`

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

## 보조 가이드 문서

- `MIRROR_SETUP.md` — 방2 거울 옵션 B (페이크 텍스처 스왑) 셋업
- `MEMORYBOX_SETUP.md` — 방2 보관함 + 양면 메모 (FlipNoteUI) 와이어링
- `FLOOR2_SETUP.md` — 2층 Mirror Branch·친구의 방·Chase Corridor 통합 셋업 (2026-05-22 구조 변경 반영)

---

## 🔥 HANDOFF — 현재 상태와 다음 작업 (2026-06-11 새벽 기준, 마감 6/17)

> 이 섹션은 작업 컴퓨터가 바뀌어도 이어서 작업하기 위한 인계서. 완료되면 갱신/삭제할 것.

### 최근 완료 (커밋 ~7d9aa44, 전부 푸시됨)
- 지하 엔딩 와이어링 완료 (EscapeTrigger→EndingSequence, 편지 글리치+크레딧). 나눔고딕 TMP 폰트 추가 + LiberationSans fallback 등록(전 씬 한글 깨짐 해결)
- **방3 리뉴얼 완전체**: 석상 시선 사슬 퍼즐(전원이 중앙 사자상 응시→사자가 첫 석상 응시=시작 단서→정답 순서 WingedLion→Girl→Broken→Man→정답 시 다음 석상 응시) → 완성 시 사자상 파편 붕괴(LionBreakEvent, 물리 미사용 shatter) → 방2 열쇠 → 기존 위핑엔젤 추적 → **복도 점프스케어**(StatueJumpscare: Meshy 베일 여인상이 복도 끝→문 나서면 눈앞, 받침대만 남음)
- 상호작용 프롬프트 통일: 문 `[E]` / 열쇠 `[E] : 줍기` / 노트 `[E] : 읽기` / 석상 `[E] : 살펴보기` / 촛불 `[E] : 켜기`. InteractText 크로스헤어 오른쪽으로 이동. **UnderGround에 GameUI+InteractText 신설**(프롬프트 안 뜨던 버그 수정)
- 슬라이딩 퍼즐 UI 입체 액자(UIVerticalGradient) + 방2 단서그림 월드 나무액자(R2_FramedPicture) + 이스터에그 하트(ForYou/SlowSpin)

### ⚠️ 미해결 사건: WH.unity 무단 변경 → git restore로 복구됨
- AtmosphereController가 플레이 중 바꾼 RenderSettings(Fog/Ambient)가 **플레이 종료 후에도 에디터에 잔류**하는 Unity 함정 + R2_Asset 캐비닛 4개 삭제(경위불명)가 저장됐었음 → 디스크는 git restore 완료
- **다음 세션 첫 작업: WH 씬 열고 검증** — R2_Asset에 Cabinet 4개 존재 + Lighting의 FogEnd=20 확인. 잘못돼 있으면 `git restore Assets/Scenes/WH.unity` 후 씬 다시 열기 (저장 금지)
- 재발 방지: AtmosphereController에 OnDisable 시 원래 RenderSettings 복원 코드 추가 권장

### 🔴 크리티컬 (전체 감사 결과 — 이것부터)
1. **지하 촛불 퍼즐 단서 없음** — candleOrder 8개+오답 전체리셋인데 단서 미배치 → 클리어 불가. 해결: 정답을 "빨간 초부터 시계방향"으로 재배열(인스펙터 드래그) 또는 손그림 단서 배치
2. **방2 양면 메모 비어있음** — FlipNoteUI 씬 미배치 + 앞/뒷면 텍스처 미제작 (문구 확정본은 GAME_DESIGN.md 방2 섹션). 현재 보관함 열면 "(양면 메모 UI 미설정)" 폴백. 스토리 심장이라 최우선
3. **DevCheats 활성** (F1 전체열쇠/F2 전체문/F4 씬스킵) — 빌드 전 차단
4. **IntroSequence 미배치** — 시작 편지 인트로가 어느 씬에도 없음. 엔딩(편지 변화) 임팩트를 위해 필요

### 🟡 게임성
- 달리기 무의미(walk 3.0/run 3.2) → run 4.5~5 권장. 스태미나는 PlayerMove에 구현돼 있음
- key=None+locked 문은 첫 E에 그냥 열림(HasKey(None)=true) → 방 순서 강제 없음. Room3 열쇠는 트리거 전용(문 안 엶). 의도 확인
- 잠긴 문 화면 피드백 없음(콘솔 로그만)
- **풀런(시작→엔딩) 테스트 미실시**

### 🟢 그 외
- SFX 전반 부재(최대 공백): 시선퍼즐 3(stoneGrind/wrong/solve)·사자붕괴 2(crack/thud)·점프스케어 스팅·엔딩 3(door/letter/glitch) + GAME_DESIGN.md SFX 리스트
- 허브(홀) 4단계 변화 미구현 — 축소판(클리어마다 조명 끄기) 또는 컷
- MainMenu 씬에 방2 사본이 로직 컴포넌트째 포함 — 정리 권장. MemoryBox "Doll" UG 잔재 1개
- MemoryBox/PhotoPuzzleManager의 keyToReveal 비어있음(메인홀 열쇠 요구 문이 없어 진행은 됨)

### 권장 일정 (6일)
1일차: 크리티컬 1·2·3 / 2일차: 인트로+달리기+잠금프롬프트 / 3~4일: SFX 일괄 / 5일: 풀런 테스트×2(블라인드 1회 포함) / 6일: 버그픽스+빌드
