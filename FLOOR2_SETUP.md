# 2층 Mirror Branch + 친구의 방 + Chase Corridor 셋업 가이드

> 작성일: 2026-05-21 / 구조 변경: 2026-05-22
> 관련 스크립트: `FakeRoomMirrorEvent.cs`(손자국 연출), `MirrorBranchInteract.cs`(거울 E키 헬퍼 — 2026-05-25 동봉→별도 파일 분리), `Floor2MirrorEvent.cs`, `Floor2NorthDoorTrigger.cs`(north 근접 헬퍼 — 별도 파일), `Floor2GhostEvent.cs`(기존), `FloorBreakTrigger.cs`(기존), `DoorInteract.cs`(`ForceUnlock` 완료)
> 기획: GAME_DESIGN.md 4번 "Mirror Branch" / "2층 친구의 방" / 7번 1.6순위

## 전체 구조 (2026-05-22 변경)

```
1층 계단 (south-west)
   ↑
Main Corridor (ㄱ자, 약 6 × 8) ─── east 분기 ───→ Mirror Branch (dead-end, 약 8 × 3)
   │                                                · 거울 1개 (끝벽)
   │                                                · 콘솔 + 엄마 일기
   │                                                · 손자국 데칼들 (연출 시 등장)
   │
   ↓ north 진입
Friend's Room (16 × 20)
   · south 문: 처음엔 잠김 → Mirror Branch 거울 손자국 연출 완료 시 자동 해제
   · 거울 (west 벽) + 책상 + 침대 + north 출구 문
   │
   ↓ north 출구
Chase Corridor (약 4 × 8)
   │
   ↓ 끝부분 FloorBreakTrigger
Trapdoor → UnderGround 씬 로드
```

**구조 변경 이유 (2026-05-22):**
페이크 *방*은 "왜 이 방에 왔지?" 모호함과 별도 빌드업 부담이 있어서 폐기.
Main Corridor에서 east로 분기되는 **막다른 골목 (Mirror Branch)** 으로 압축.
친구의 방 south 문 잠금 + Mirror Branch 거울 손자국 연출이 해제 트리거 → 학습 강제 동선.

방 크기 단위: Unity m (Cube primitive 기준).

---

## A. Mirror Branch (east 분기, dead-end) — 거울 손자국 연출 + 동선 강제

### A.1 메시 + 가구
GAME_DESIGN.md "Mirror Branch" 컨셉:
- **거울 1개 (끝벽 정면)** — 막다른 골목 끝
- 콘솔/작은 책상 1개 (거울 옆) — 엄마 일기 올림
- 별도 침대/책장/의자 없음 (방이 아니라 복도 분기이므로)

권장 배치 (Mirror Branch 입구 (0,0,0) 기준, east로 8m 깊이):
```
거울       : 끝벽 (x = +7.5), 높이 1.5m, 크기 1.5 × 2
콘솔/책상  : 거울 옆 (x = +6, z = +0.5) — 엄마 일기 올림
입구       : west (x = 0) — Main Corridor와 연결, 문 없는 열린 통로
```

### A.2 핵심 오브젝트 + 컴포넌트

```
MirrorBranch (빈 GameObject 부모)
├── Walls / Floor / Ceiling
├── Furniture
│   ├── Console               ← 콘솔/작은 책상 (일기 올림)
│   └── Mirror_Branch          ← Quad. Layer=Interact, Collider + MirrorBranchInteract
├── Items
│   └── MotherDiary_Mirror    ← NoteItem (noteID = "mirrorbranch_mother_diary")
├── Handprints                ← 손자국 데칼들 (Quad/Decal, 시작 시 비활성)
│   ├── Handprint_0
│   ├── Handprint_1
│   └── Handprint_2 ...
├── Lights
│   └── Point Light × 1~2 (어둑한 분위기, 거울 위 살짝 강조)
└── FakeRoomMirrorEvent        ← 매니저 컴포넌트 (클래스명 유지)
```

> ⚠️ 작은 열쇠(DrawerSmall) **폐기**. dead-end라 출입문 없음 → 옛 `FakeRoomDoorProximity` 헬퍼도 폐기 (씬에 남아 있으면 Missing Script로 뜨니 삭제).
> 거울 트리거는 거울 자체에 E키 상호작용(`MirrorBranchInteract`, 별도 파일 `MirrorBranchInteract.cs`).

### A.3 FakeRoomMirrorEvent 인스펙터

| 슬롯 | 값 |
|---|---|
| motherDiaryNoteID | `mirrorbranch_mother_diary` |
| mirrorInteract | Mirror_Branch의 `MirrorBranchInteract` |
| handprints[] | 손자국 데칼들 (떠오를 순서대로 등록) |
| handprintInterval | 0.6 |
| handprintFadeDuration | 0.8 (0이면 즉시 등장) |
| roomLights[] | Mirror Branch Point Light 배열 |
| flickerDuration / flickerInterval | 0.4 / 0.07 |
| **friendRoomSouthDoor** | Friend's Room SouthDoor의 `DoorInteract` — 연출 완료 시 `ForceUnlock()` |
| unlockDelay | 1.0 |
| audioSource | (선택) AudioSource |
| mirrorAwakenSfx | 일기 읽은 직후 거울 낮은 울림 (상호작용 활성 신호) |
| mirrorRumbleSfx | 연출 시작 시 낮은 쿵/유리 울림 |
| handprintSfx | 손자국 하나 떠오를 때마다 |
| unlockSfx | south 문 잠금 해제 알림 (선택) |

### A.4 거울 트리거 — MirrorBranchInteract (E키)

```
Mirror_Branch GameObject:
- Layer: Interact
- Collider (IsTrigger = OFF)
- MirrorBranchInteract 컴포넌트 (Interactable 상속, 별도 파일 MirrorBranchInteract.cs)
    · manager = FakeRoomMirrorEvent
    · readyPrompt = "E : 거울을 들여다본다"
- 일기 읽기 전: Awake에서 콜라이더 자동 OFF → 레이캐스트 안 맞아 프롬프트 안 뜸
- 일기 읽으면: 매니저가 SetReady(true) → 콜라이더 ON → 상호작용 가능
- E키 1회 → FakeRoomMirrorEvent.TriggerMirrorSequence() (1회성)
```

### A.5 MotherDiary_Mirror (NoteItem) 내용
```
noteID: mirrorbranch_mother_diary
noteContent:
    왜 자꾸 이 방으로 돌아오는 걸까.
    저 거울 속에 그 애가 있는 것만 같아.
```

### A.6 손자국 데칼 만들기
- 손자국 Quad/Decal을 거울 표면에 겹쳐 배치, 시작 시 **전부 비활성** (매니저 Start가 한 번 더 꺼줌)
- 매니저 `handprints[]`에 떠오를 순서대로 등록
- **페이드(안쪽에서 스르륵 등장)** 를 쓰려면 데칼 머티리얼이 **Transparent** (URP Lit/Unlit Transparent)여야 함
  - 매니저가 런타임에 `_BaseColor`(URP) 또는 `_Color` 알파를 0→1로 페이드
  - Opaque 머티리얼이면 페이드 없이 `SetActive` 등장으로 폴백
- MVP는 빨간 Quad placeholder로 흐름만 확인 → 진짜 손자국 텍스처는 폴리시 단계

### A.7 연출 완료 → 친구의 방 잠금 해제 흐름

`FakeRoomMirrorEvent.HandprintSequence()` 코루틴 마지막부:
```csharp
// 손자국 순차 등장 → 조명 깜빡 후
yield return new WaitForSeconds(unlockDelay);

if (friendRoomSouthDoor != null)
    friendRoomSouthDoor.ForceUnlock();   // south 문 자동 해제

if (audioSource != null && unlockSfx != null)
    audioSource.PlayOneShot(unlockSfx);
```

> `DoorInteract.ForceUnlock()`은 이미 구현됨 (`isLocked=false` + `"{name} 잠금 해제됨"` 로그). 작은 열쇠 활성화 단계는 폐기됨.

---

## B. 친구의 방 — 클라이맥스 + 7단계 시퀀스

### B.1 메시 + 가구
GAME_DESIGN.md 친구의 방 컨셉:
- 책상 (엄마 메인 일기 위에)
- 책상 서랍 (친구 마지막 메모 안에 / 작은 열쇠 폐기 → 키 없이 열림)
- 침대 옆 메인 열쇠
- **거울 (왼쪽 벽)**
- south 입구 문 (들어온 문)
- north 출구 문 (처음엔 잠김, 추격 복도로 연결)

권장 배치 (방 중심 (0,0,0) 기준, 16×20):
```
거울            : west 벽 (x = -8), 크기 1.5 × 2.5
책상            : 방 가운데 (0, 0, +5) — 거울이 옆에 있는 구도
서랍 (DoorInteract): 책상 자식
침대            : east 벽 (x = +7) 따라
south 입구 문    : south (z = -10) — Main Corridor와 연결, 처음엔 잠김
north 출구 문    : north (z = +10) — Chase Corridor로
메인 열쇠 KeyItem: 침대 옆 (x = +6, z = +3)
```

### B.1.1 South 문 잠금 메커니즘 (2026-05-22 추가 / 05-24 손자국 반영)
south 문은 처음엔 잠겨 있고, Mirror Branch 거울 손자국 연출 완료 시 `FakeRoomMirrorEvent`가 외부에서 `ForceUnlock()` 호출로 해제.

```
SouthDoor GameObject:
- DoorInteract 컴포넌트:
    isLocked = true
    requiredKey = KeyType.None  (외부 트리거로만 해제)
    lockedPrompt = "문이 잠겨 있다. 무언가 더 살펴봐야 한다."
- DoorInteract.cs에 신규 메소드:
    public void ForceUnlock() {
        isLocked = false;
        // 선택: 잠금 해제 SFX 재생
    }
```

플레이어 흐름:
1. 계단으로 2층 진입 → south 문 접근 → 잠김 (콘솔 로그로 확인, 화면 메시지는 폴리시 항목)
2. Mirror Branch로 우회 → 엄마 일기 읽기 → 거울 E키 → 손자국 연출
3. 연출 끝 → south 문 자동 잠금 해제
4. south 문 다시 접근 → 정상 개방

### B.2 핵심 오브젝트 + 컴포넌트

```
FriendRoom (빈 부모)
├── Walls / Floor / Ceiling
├── Furniture
│   ├── Desk
│   │   ├── MotherDiary_Main   ← NoteItem (noteID = "floor2_mother_diary")
│   │   └── Drawer              ← DoorInteract (isLocked=false — 작은 열쇠 폐기, 키 없이 열림)
│   │       └── FriendFinalNote ← NoteItem (noteID = "floor2_friend_final_note")
│   ├── Bed
│   └── Mirror_Friend           ← Quad/Mesh, 반사 카메라 또는 옵션 B
├── Items
│   └── MainKey_Floor2          ← KeyItem (keyType = KeyType.Floor2)
├── MirrorGhost                 ← 거울 속 귀신 (MirrorOnly 레이어, 비활성)
├── RealGhost                   ← 추격용 귀신 (GhostChase + NavMeshAgent, 비활성)
│   └── GhostSpawn (Transform)
├── MirrorShatterFx             ← 거울 깨짐 파티클 (비활성)
├── Lights
│   ├── Point Light × N (방 조명)
│   └── MirrorSpotlight (Spot Light, 거울 위)
├── Doors
│   ├── SouthDoor   ← DoorInteract (isLocked=true, requiredKey=None) — Main Corridor와 연결. Mirror Branch 거울 손자국 연출 완료 시 `ForceUnlock()`로 해제
│   └── NorthDoor   ← DoorInteract (isLocked=true, requiredKey=None) — Chase Corridor로 연결
├── NorthDoorTrigger ← BoxCollider IsTrigger=ON + Floor2NorthDoorTrigger (시작 시 GameObject 비활성)
└── Floor2MirrorEvent ← 매니저 컴포넌트
```

### B.3 Floor2MirrorEvent 인스펙터

| 슬롯 | 값 |
|---|---|
| motherDiaryNoteID | `floor2_mother_diary` |
| friendNoteID | `floor2_friend_final_note` |
| requiredKey | `KeyType.Floor2` |
| entranceDoor | SouthDoor (DoorInteract) |
| northDoor | NorthDoor (DoorInteract) |
| forceOpenNorth | ON |
| mirror | Mirror_Friend Transform |
| playerCamera | Main Camera |
| gazeDot | 0.75 |
| mirrorSpotlight | MirrorSpotlight Light |
| roomLights | 방 Point Light 배열 |
| roomDimRatio | 0.15 |
| lightFadeDuration | 1.2 |
| spotlightFadeIn | 1.2 |
| mirrorGhost | MirrorGhost GameObject |
| mirrorGhostPosition | (선택) MirrorGhost 위치 anchor — 책상 뒤 |
| ghostMinHoldOnFirstGaze | 1.5 |
| realGhost | RealGhost GameObject |
| realGhostSpawn | GhostSpawn Transform |
| ghostChase | RealGhost의 GhostChase 컴포넌트 |
| mirrorShatterFx | MirrorShatterFx GameObject |
| mirrorRenderer | Mirror_Friend의 Renderer (선택) |
| northDoorTriggerObject | NorthDoorTrigger GameObject |
| audioSource / SFX clips | 선택 |

### B.4 NorthDoorTrigger 설정
```
NorthDoorTrigger (빈 GameObject)
- Position: NorthDoor 앞 1m 위치 (방 안쪽)
- BoxCollider:
    IsTrigger = ON
    Size: 2 × 2 × 1.5
- Floor2NorthDoorTrigger 컴포넌트:
    target = Floor2MirrorEvent
    playerTag = "Player"
- GameObject **시작 시 비활성** — Floor2MirrorEvent가 6단계에서 활성화
```

### B.5 서랍 셋업 — DoorInteract 재사용
```
Drawer (책상 자식)
- DoorInteract 컴포넌트:
    isLocked = false   (작은 열쇠 폐기 — 키 없이 열림)
    requiredKey = KeyType.None
    doorPivot = (서랍이 빠지는 pivot Transform — 회전 또는 위치 이동)
    openAngle = 45 (서랍이 살짝 빠지는 각도) 또는 별도 처리
- Layer: Interact
- BoxCollider (E키 받기)
```

> DoorInteract은 회전 기반이라 서랍 슬라이딩이 어색할 수 있음. 단기 MVP면 그대로 사용, 마감 직전에 별도 `DrawerInteract` 슬라이딩 컴포넌트로 교체 검토.

### B.6 NoteItem 내용

**MotherDiary_Main (책상 위):**
```
noteID: floor2_mother_diary
noteContent:
    내 아들...
    의사들은 모두 손을 떼었다.
    하지만 나에겐 약속의 존재가 있다.
    같은 또래 한 명이면 된다고 했다.
    그게 어떻게 끝났는지...
```

**FriendFinalNote (서랍 안):**
```
noteID: floor2_friend_final_note
noteContent:
    여기서 나가지 못할 것 같다.
    이 집은 우리를 부른다고 했지.
    내가 미안하다. 너에게 편지를 보낸 적이 없다.
    누군가 내 이름을 빌렸을 뿐이다.
```

### B.7 MirrorOnly 레이어 (정석 옵션)
```
Edit → Project Settings → Tags and Layers
- Layer 8: MirrorOnly  (또는 비어있는 번호)

거울 반사 카메라 (Mirror_Friend 옆에 자식 Camera):
- Culling Mask: Default + MirrorOnly + (귀신 레이어들)
- Output Target: RenderTexture (Mirror_Friend 머티리얼에 바인딩)

플레이어 Main Camera:
- Culling Mask: Default + (모든 레이어) - MirrorOnly  ← MirrorOnly만 제외

MirrorGhost.layer = MirrorOnly  → 거울에서만 보임
```

---

## C. Chase Corridor (약 4 × 8)

### C.1 메시
- 4m × 8m × 3m (높이) — 기존 12m에서 단축 (2026-05-22 그림 기준)
- 단순 직선
- 끝부분에 `FloorBreakTrigger` 배치 (Trapdoor 위치)

### C.2 FloorBreakTrigger
이미 기존 스크립트가 있음. 인스펙터 슬롯 그대로 채워서 UnderGround 씬으로 전환.

```
FloorBreakTrigger 배치:
- Position: Chase Corridor 끝 2m 전쯤 (Trapdoor 직전)
- BoxCollider IsTrigger=ON, Size: 4 × 3 × 1
- FloorBreakTrigger.cs 컴포넌트 슬롯 채우기
```

### C.3 NavMesh
- **Main Corridor, Mirror Branch, 친구의 방, Chase Corridor 모두** NavMesh 베이크에 포함
- 1층 계단/높이 차이는 NavMeshLink로 연결
- Window → AI → Navigation → Bake

---

## D. RealGhost (추격 귀신) 셋업
2층에서 새로 만들어야 하면 기존 `GhostChase`가 부착된 prefab 복제해서 친구의 방용으로 따로:

```
RealGhost_Floor2 (시작 시 비활성)
- NavMeshAgent
- GhostChase 컴포넌트
- 머티리얼: Renderer (페이드인 가능한 머티리얼)
- 자식: GhostSpawn (Transform, 거울 위치)
```

`Floor2MirrorEvent`가 7단계에서 `realGhost.SetActive(true)` + `ghostChase.StartChase()` 호출.

---

## E. 작업 순서 권장 (실작업, 2026-05-22 갱신)

```
1. [완료] Main Corridor + Mirror Branch + 친구의 방 + Chase Corridor 메시   [0h]
2. Mirror Branch 가구 배치 (콘솔 + 거울)                                     [0.5h]
3. 친구의 방 가구 배치 (책상, 침대, 거울)                                    [1h]
4. NavMesh 베이크 (전체)                                                      [0.3h]
5. NoteItem 3개 (Mirror Branch 일기, 친구의 방 일기, 친구 최종 메모)           [0.4h]
6. KeyItem 1개 (Floor2 — 친구의 방). 작은 열쇠 폐기                           [0.2h]
7. DoorInteract: SouthDoor (잠김) / NorthDoor (잠김) / Drawer (키 없이 열림)  [0.5h]
8. [완료] DoorInteract.cs에 `ForceUnlock()` 메소드                            [0h]
9. [완료] FakeRoomMirrorEvent 손자국 연출 + MirrorBranchInteract(거울 E키) +
   연출 완료 시 friendRoomSouthDoor.ForceUnlock()                            [0h]
10. MirrorGhost, RealGhost prefab/배치                                         [1h]
11. Floor2MirrorEvent 와이어링 + NorthDoorTrigger                             [0.7h]
12. MirrorShatterFx (간단한 파티클로 충분)                                     [0.5h]
13. SFX 8~10종 import + 슬롯 연결                                              [1h]
14. FloorBreakTrigger 배치 (Chase Corridor 끝)                                 [0.3h]
15. 플레이테스트:
    south 잠김 확인 → Mirror Branch 진입 → 일기 → 거울 E키 → 손자국 →
    south 잠금 해제 → 친구의 방 진입 →
    메인 키 + 서랍(친구 메모, 키 없이) + 엄마 일기 → 7단계 시퀀스 →
    north 개방 → 거울 폭발 → 추격 → FloorBreak → UnderGround                [1h]
```

총 약 **8.7시간**. 마감 4주 = 80~110h 범위 내. 페이크 *방* 폐기로 약 1.3h 절감.

---

## F. 트러블슈팅

| 문제 | 해결 |
|---|---|
| 친구의 방 south 문이 안 열림 | Mirror Branch 손자국 연출이 완료됐는지 확인. `FakeRoomMirrorEvent`가 `friendRoomSouthDoor.ForceUnlock()` 호출하는지 콘솔 로그(`{name} 잠금 해제됨`) 확인 |
| 거울 E키 프롬프트가 안 뜸 | 엄마 일기를 먼저 읽었는지(noteID 일치), `MirrorBranchInteract.manager` 연결, 거울 Layer=Interact + Collider 확인 |
| 손자국이 안 보임 | `handprints[]`에 등록·시작 시 비활성인지. 페이드 쓰면 데칼 머티리얼이 Transparent인지 (Opaque면 즉시 등장) |
| 7단계가 시작 안 됨 | 3조건이 모두 충족됐는지 콘솔 확인. NoteItem noteID 오타, Drawer 잠금이 너무 빡빡한지 |
| 거울 응시 판정이 너무 빡빡 | `gazeDot` 0.75 → 0.6으로 낮춤 |
| 거울 속 귀신이 직접 뒤돌아봐도 보임 | MirrorGhost.layer를 MirrorOnly로 설정 + Main Camera Culling Mask에서 MirrorOnly 제외 |
| north 트리거가 너무 빨리 발동 | NorthDoorTriggerObject가 6단계 전에 활성화돼 있는지. 시작 시 SetActive(false) 확인 |
| 추격 시작 후 귀신이 안 따라옴 | NavMesh 베이크 확인, NavMeshLink가 굽이/계단에 깔려있는지, GhostChase의 detect/catch 거리 확인 |
| 메인 열쇠 픽업 시 다른 이벤트 발동 | KeyItem.cs에서 KeyType.Floor2 특수 처리 안 함. KeyType.Room3/Room2가 아닌 이상 안전 |

---

## G. 다음 작업 (마감 전)
- [x] `DoorInteract.cs`에 `ForceUnlock()` 메소드 (완료)
- [x] `FakeRoomMirrorEvent` 손자국 연출 + `MirrorBranchInteract`(거울 E키) + ForceUnlock 호출 (완료)
- [ ] 손자국 데칼 텍스처 + Transparent 머티리얼 (지금은 placeholder Quad)
- [ ] MirrorOnly 레이어 + 거울 반사 카메라 (친구의 방 거울용)
- [ ] 환경 단서 3~4개 (찢어진 사진 조각, 손톱자국, 일기 페이지) — Main Corridor와 Mirror Branch에 분산
- [ ] 사이트블로커 배치 (책장, 액자, 콘솔 테이블) — Main Corridor 시야 분절
- [ ] 거울 깨짐 셰이더 또는 파편 파티클 디테일
- [ ] 추격 BGM + 발소리 / 숨소리 / 속삭임 SFX (락유어도어 차용안 9.4.1 검토 시점)
