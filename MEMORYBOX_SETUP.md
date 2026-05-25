# 방2 보관함 + 양면 메모 셋업 가이드

> 작성일: 2026-05-21
> 관련 스크립트: `MemoryBox.cs`, `FlipNoteUI.cs`
> 기획: GAME_DESIGN.md 4번 "방2 — 기록 보관실", 7번 1.5.1순위
> 선행: `MIRROR_SETUP.md` (방2 거울 옵션 B) 완료 후 진행

## 흐름 (실행 시)

```
종이 조각 3개 수집 (PhotoPuzzleManager)
  → MemoryBox.HandleAllCollected (자동)
  → 자물쇠 떨어짐 SFX + lockObject 비활성 + lockFallenObject 활성
  → memoVisual 활성 (보관함 안 떠 있는 메모)
  → 플레이어가 보관함에 E키
  → FlipNoteUI.Open(앞면, 뒷면)  ← 클로즈업 자동 진입
  → 플레이어가 R / 우클릭으로 뒷면 확인
  → Esc로 닫기
  → MemoryBox 분위기 변화: 벽난로 꺼짐, 거울 손자국 활성, 방 조명 어두워짐
  → PhotoPuzzleManager.RevealKey()  → 메인홀 열쇠 등장
```

---

## 1. FlipNoteUI 캔버스 셋업 (1회만)

이미 NoteUI 캔버스가 있다면 그 옆에 패널 하나 추가하면 됩니다.

```
Hierarchy:
└── Canvas (기존)
    └── FlipNotePanel  ← 새로 만듦
        ├── Image (배경 어두움 — RGBA 0,0,0,0.85)
        ├── MemoImage  ← 메모 Sprite (Image 컴포넌트)
        └── HintText (TextMeshProUGUI) — "R / 우클릭: 뒤집기   Esc: 닫기"
```

빈 GameObject 하나 더 만들어 FlipNoteUI 컴포넌트 부착:

```
Hierarchy → 빈 GameObject "FlipNoteUI"
- FlipNoteUI 컴포넌트 추가
- 인스펙터 슬롯:
  · panel       → FlipNotePanel (위에서 만든 패널)
  · memoImage   → MemoImage
  · hintText    → HintText (없어도 됨)
  · audioSource → (선택) AudioSource 컴포넌트
  · flipSfx / openSfx / closeSfx → 종이 펄럭임 / 부스럭 / 살짝 닫는 소리
- FlipNotePanel은 시작 시 비활성 (Awake에서 자동 비활성됨)
```

### MemoImage RectTransform 권장 설정
- Anchor: 중앙(center)
- Pivot: 0.5 0.5
- Width × Height: 600 × 800 (세로 메모) — 텍스처 비율에 맞춰 조정
- Scale: 1, 1, 1 (스크립트가 뒤집기 시 X만 변경)

### 양면 메모 텍스처 만들기 (Sprite 임포트 설정)
`Assets/Textures/Memo_Front.png`, `Memo_Back.png`:
- Texture Type: **Sprite (2D and UI)**
- sRGB: **ON**
- Wrap Mode: Clamp
- Filter: Bilinear
- Compression: High Quality
- **Apply**

내용은 GAME_DESIGN.md 4번 "양면 메모" 섹션 그대로:

**Memo_Front (둥근 친구 글씨체):**
```
오지 마.
편지는 내가 쓴 게 아니야.
이 집은 우리를 부른다.
```

**Memo_Back (각진 옛 필체):**
```
명부

엘리   17  ✓ (취소선)
마르   18  ✓ (취소선)
요한   16  ✓ (취소선)
친구   18  ✓ (취소선)
_____  17  _      ← 잉크 번짐
```

---

## 2. 보관함 GameObject 셋업

이미 방2 안에 보관함이 있다고 가정 (`Lock Box` 같은 이름의 GameObject).

```
Hierarchy → 보관함 GameObject 선택
- Box Collider 추가 (IsTrigger=OFF) — E키 Raycast 받기용
- Layer: Interact
- MemoryBox 컴포넌트 추가
```

자식 구조 권장:

```
LockBox
├── LockObject       ← 잠긴 상태 시각 (자물쇠 mesh)
├── LockFallenObject ← 떨어진 자물쇠 (시작 시 비활성)
├── MemoVisual       ← 보관함 안에 떠 있는 양면 메모 mesh (시작 시 비활성)
├── PieceSlot0       ← PhotoPuzzleManager의 pieceSlots[0]와 동일 가능
├── PieceSlot1
└── PieceSlot2
```

### MemoryBox 인스펙터 슬롯

| 슬롯 | 값 |
|---|---|
| photoPuzzle | PhotoPuzzleManager GameObject |
| pieceSlotsOnPaper[0..2] | PieceSlot0/1/2 (PhotoPuzzleManager.pieceSlots와 동일 배열 권장) |
| lockObject | LockObject |
| lockFallenObject | LockFallenObject |
| memoVisual | MemoVisual |
| memoFront | `Memo_Front` Sprite |
| memoBack | `Memo_Back` Sprite |
| keyToReveal | (선택) PhotoPuzzleManager의 keyToReveal과 동일 GameObject — RevealKey 호출로 자동 활성됨 |
| fireplaceLight | 방2 벽난로 Light |
| fireplaceFireObject | 벽난로 불꽃 메시/파티클 |
| mirrorHandprint | 거울 손자국 데칼 (시작 시 비활성) |
| roomLights | 방2 천장 조명들 (배열) |
| lightsDimRatio | 0.6 (기본) |
| promptLocked | "E : 보관함을 살펴본다" |
| promptReady | "E : 양면 메모를 들어올린다" |
| lockedNoteText | "종이가 찢겨 있다.\n\n세 조각이 비어 있다." |
| pieceFitSfx / lockFallSfx / memoLiftSfx / fireplaceOutSfx / handprintSfx | (선택) |

### PhotoPuzzleManager와의 관계
- `PhotoPuzzleManager.pieceSlots`와 `MemoryBox.pieceSlotsOnPaper`는 동일한 GameObject 배열을 가리켜도 됨
- `PhotoPuzzleManager.keyToReveal`는 비워두지 말고 메인홀 KeyItem 연결 — MemoryBox에서 `RevealKey()` 호출하면 PhotoPuzzleManager가 키 활성화
- `Room2AtmosphereEvent`도 함께 사용 가능 — Atmosphere는 조각마다 환경 해프닝, MemoryBox는 메모 + 키. 두 컴포넌트가 둘 다 OnAllPiecesCollected를 받지만 `PhotoPuzzleManager.RevealKey()` 측에서 `puzzleSolved` 플래그로 한 번만 작동

> ⚠️ `Room2AtmosphereEvent.HandleAllCollected`도 마지막에 `RevealKey()`를 호출함. 메모 닫힘 *전에* 키가 등장하면 안 되면 `Room2AtmosphereEvent`의 코드를 수정하거나 (마지막 `RevealKey()` 호출 제거) Atmosphere 컴포넌트를 비활성화하고 환경 해프닝만 별도로 구현.

---

## 3. SFX (선택, freesound.org 등)

| 이름 | 검색 키워드 |
|---|---|
| pieceFitSfx | "paper insert", "card place" |
| lockFallSfx | "padlock drop", "metal clink" |
| memoLiftSfx | "paper rustle pickup" |
| fireplaceOutSfx | "fire extinguish puff" |
| handprintSfx | "glass tap dull" |
| flipSfx | "paper flip", "card turn" |
| openSfx | "menu open soft" |
| closeSfx | "menu close soft" |

`Assets/Audio/`에 import → 슬롯 연결.

---

## 4. 플레이테스트

```
1. Play
2. 조각 1, 2, 3 모두 픽업
3. 보관함 위에 자물쇠 떨어지고 메모 visual이 활성화되는지 확인
4. 보관함에 E키 → FlipNoteUI 진입
5. 앞면 → R 또는 우클릭 → 0.5초 플립 → 뒷면(명부) 표시
6. Esc로 닫기
7. 벽난로 꺼짐 + 방 조명 어두워짐 + 거울 손자국 활성 확인
8. 메인홀 KeyItem 등장 → 픽업
```

## 트러블슈팅

| 문제 | 해결 |
|---|---|
| 보관함 E키 프롬프트 안 뜸 | Layer가 Interact인지, BoxCollider 있는지 확인 |
| 자물쇠가 안 떨어짐 | PhotoPuzzleManager.OnAllPiecesCollected 구독 확인 — Start() 한 번만 실행되는지 |
| FlipNoteUI Open 안 됨 | FlipNoteUI.Instance가 null. 씬에 FlipNoteUI 컴포넌트 부착된 GameObject 있는지 |
| R 눌러도 안 뒤집힘 | memoImage 슬롯 비어 있는지 확인 |
| 플립 시 텍스처가 좌우 반전 | 정상 동작 — scaleX가 0을 거치며 뒤집힘. 두 번 뒤집으면 원래 방향 |
| 메모 닫힘 후 키 안 나옴 | photoPuzzle 슬롯 빈 경우 keyToReveal 슬롯 채워야 함 |
