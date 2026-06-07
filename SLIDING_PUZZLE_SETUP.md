# 방2 슬라이딩 퍼즐 셋업 가이드

> 기획: 방2 퍼즐을 "사진 조각 수집"에서 **슬라이딩 퍼즐(3x3)** 로 변경.
> 완성 시 기존 흐름 그대로 `PhotoPuzzleManager.RevealKey()` → 메인홀 열쇠 등장.

## 동작 흐름

```
방2 액자에 E키
  → SlidingPuzzleUI 패널 열림 (3x3 흐트러진 사진)
  → 타일 클릭으로 빈칸 옆 타일 이동
  → 사진 완성
  → 0.8초 후 패널 닫힘 + PhotoPuzzleManager.RevealKey()
  → 메인홀 열쇠 등장
```

## 스크립트 2개

| 파일 | 역할 |
|---|---|
| `Assets/ObjectScripts/SlidingPuzzleUI.cs` | 퍼즐 UI 싱글톤. 이미지 1장을 3x3으로 자동 분할, 셔플, 클릭 이동, 완성 판정 |
| `Assets/ObjectScripts/SlidingPuzzleInteract.cs` | 액자에 붙이는 Interactable. E키로 UI 열고, 완성 콜백에서 RevealKey 호출 |

---

## 1. 사진 이미지 준비

- 맞출 사진을 **정사각형**(예: 512x512, 1024x1024)으로 준비
- Import Settings:
  - Texture Type: **Sprite (2D and UI)**
  - **Sprite Mode: Single**
  - Mesh Type: **Full Rect** (Tight면 분할 어긋날 수 있음)
  - Packing 태그/아틀라스 사용 안 함 권장 (또는 사용해도 동작은 함)
  - Read/Write Enabled: 꺼도 됨 (Sprite.Create는 픽셀 읽기 불필요)

## 2. SlidingPuzzleUI 부착 (UI 자동 생성 — 패널 만들 필요 없음!)

> `autoBuildUI = true`(기본값)이면 **Canvas / 어두운 패널 / 보드 / 닫기버튼 / EventSystem을
> 코드가 런타임에 전부 자동 생성**합니다. 손으로 패널 만들 필요 없음.

1. Hierarchy 빈 GameObject 생성 → 이름 `SlidingPuzzleUI`
2. `SlidingPuzzleUI` 컴포넌트 부착
3. 필드 설정:

| 필드 | 값 |
|---|---|
| autoBuildUI | ✅ true (기본) |
| boardPixelSize | 600 (보드 크기, 취향껏) |
| gridSize | **3** |
| tileGap | 4 |
| shuffleMoves | 80 |
| slideDuration | 0.12 |
| defaultImage | (선택) 기본 사진 Sprite |
| audioSource / slideSound / solveSound | (선택) 효과음 |
| panel / boardRoot / closeButton | **비워둠** (자동 생성됨) |

> `defaultImage`를 채우면 Interact에서 이미지를 안 넘겨도 됨.
> 직접 패널을 만들고 싶으면 panel/boardRoot에 수동 연결 → 그게 우선 사용됨.

## 3. 액자에 SlidingPuzzleInteract 부착

방2 액자/그림 오브젝트 (Collider 있고 **Interact 레이어**) 에 부착:

| 필드 | 연결 |
|---|---|
| puzzleImage | 맞출 사진 Sprite (비우면 UI의 defaultImage 사용) |
| unsolvedPrompt | "E : 흐트러진 사진을 맞추다" |
| solvedPrompt | "E : 완성된 사진" |

## 4. PhotoPuzzleManager 유지

기존 `PhotoPuzzleManager`를 **그대로 둠** (열쇠 등장 담당):

| 필드 | 연결 |
|---|---|
| keyToReveal | 메인홀 KeyItem (시작 시 자동 비활성화됨) |
| lockBox | (선택) 보관함 — 없으면 비워둠 |
| pieceSlots | **비워둠** (슬라이딩 퍼즐은 조각 안 씀) |
| roomLights | (선택) 완성 시 어두워질 방2 조명 |

> 완성 시 `RevealKey()` → `SolvePuzzle()` 코루틴이 `lockBox` 열기/조명 다운/`keyToReveal` 활성화 수행.
> `lockBox`·`roomLights`가 null이어도 null-check로 안전하게 건너뜀.

## 5. EventSystem

자동 생성 모드에선 **씬에 EventSystem이 없으면 코드가 자동으로 만듦** → 신경 안 써도 됨.
(이 프로젝트는 레거시 Input 사용 → StandaloneInputModule 자동 추가)

---

## 기존 사진수집 방식과의 관계

- `PhotoPiece` / `Room2AtmosphereEvent` / `MemoryBox`는 이제 **방2에서 사용 안 함** (조각 수집 기반).
  씬에서 해당 오브젝트들을 빼거나 비활성화. PhotoPuzzleManager는 열쇠 등장용으로만 남김.
- 분위기 연출을 살리고 싶으면 `Room2AtmosphereEvent` 대신 완성 콜백 시점에 별도 연출을 추가하는 방향 검토.

## 테스트 체크리스트

1. 방2 진입 → 액자에 조준 시 "E : 흐트러진 사진을 맞추다" 프롬프트
2. E → 패널 열림, 마우스 커서 나옴, 카메라 안 돌아감
3. 빈칸 옆 타일 클릭 → 슬라이드 이동
4. 빈칸 안 옆 타일 클릭 → 무반응 (정상)
5. 사진 완성 → solveSound + 0.8초 후 패널 닫힘
6. 메인홀 열쇠 등장 (keyToReveal 활성화)
7. ESC로 중간에 닫기 가능 (완성 전), 다시 E로 열면 새로 셔플

## 문제 해결

| 증상 | 점검 |
|---|---|
| 타일 클릭이 안 됨 | 씬에 EventSystem 있는지, panel 위에 다른 Graphic이 raycast 막는지 |
| 타일 위치가 어긋남 | boardRoot가 정사각형인지, gridSize=3 맞는지 |
| 조각이 잘못 잘림 | 사진 Sprite Mesh Type을 Full Rect로 |
| 카메라가 돌아감 | PlayerLook/PlayerMove가 씬에 있는지 (스크립트가 자동 disable) |
| 열쇠가 안 나옴 | PhotoPuzzleManager.keyToReveal 연결, 씬에 PhotoPuzzleManager 존재 확인 |
| 이미지가 없다고 에러 | Interact.puzzleImage 또는 UI.defaultImage 중 하나는 채워야 함 |
