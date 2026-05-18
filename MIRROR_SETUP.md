# 방2 거울 작업 가이드 (옵션 B — 페이크 텍스처 스왑)

> 작성일: 2026-05-18
> 관련 스크립트: `Assets/ObjectScripts/MirrorRevealEvent.cs`, `Assets/ObjectScripts/MirrorCaptureHelper.cs`
> 기획: GAME_DESIGN.md 4번 섹션 "방2 — 기록 보관실"

## 개요

방2 거울 = 어두운 Quad + 텍스처 2장(빈 의자 / 의자+조각) 스왑.
플레이어가 E키로 거울 조사 → 거울에 조각 보임 → 의자 쪽 뒤돌아봄 감지 → 실제 의자 위 조각 활성화.
실시간 반사 카메라 안 씀. 2층 친구의 방 거울과 별도 시스템.

## 작업 순서 (예상 25분)

1. 거울 Quad GameObject 만들기
2. 거울 머티리얼 만들기
3. Collider + Layer 설정
4. 의자 + 조각 prefab 배치
5. 텍스처 2장 캡처 (MirrorCaptureHelper 사용)
6. MirrorRevealEvent 인스펙터 와이어링
7. SFX 클립 연결
8. 플레이테스트

---

## 1. 거울 Quad GameObject 만들기

```
Hierarchy → 우클릭 → 3D Object → Quad
- 이름: Mirror_Room2
- Position: 방2 벽 한쪽 (의자 반대편)
- Rotation: 벽을 등지고 의자 쪽 향함
- Scale: 1.5 × 2 × 1 (세로 거울 비율)
```

## 2. 거울 머티리얼 만들기

```
Project → Assets/Materials → 우클릭 → Create → Material
- 이름: M_MirrorRoom2
- Shader: Universal Render Pipeline/Unlit
- Surface Type: Opaque
- Base Map: 비워둠 (스크립트가 런타임에 설정)
- Base Color: 흰색 (#FFFFFF) — 텍스처 곱연산 정상화

Quad에 드래그 적용
```

## 3. Collider + Layer + 스크립트

```
Mirror_Room2 선택 →
- Layer: Interact (이미 있음)
- Box Collider 추가 (IsTrigger = OFF, 일반 Collider)
- MirrorRevealEvent 컴포넌트 추가
- (선택) AudioSource 추가 (Play On Awake = OFF, Loop = OFF)
```

## 4. 의자 + 조각 prefab 배치

```
방2 안 의자 GameObject 배치 (Hierarchy):
- Chair_Room2  ← 기존 의자 모델 또는 새로 배치
  └── PhotoPiece_3  ← 자식으로 종이 조각 prefab
      - PhotoPiece 컴포넌트
        · pieceID = 2
      - Layer: Interact
      - 시작 시 활성화돼 있어도 OK
        (MirrorRevealEvent.Awake에서 자동 SetActive(false))
```

## 5. 텍스처 2장 캡처 — `MirrorCaptureHelper` 사용

### 임시 캡처 카메라 만들기

```
Hierarchy → 빈 GameObject 추가 → Camera 컴포넌트 추가
- 이름: MirrorCaptureCam
- Position: 거울 위치 + 거울 forward 방향으로 0.1m
- Rotation: 의자 쪽 향함
- Background Type: Solid Color, 어두운 색 (#0a0a0a)
- Field of View: 60 (또는 거울 비율에 맞춰 조정)
- MirrorCaptureHelper 컴포넌트 추가
```

### 빈 의자 캡처

```
1. PhotoPiece_3 비활성화 (Hierarchy에서 체크박스 끔)
2. MirrorCaptureCam 선택 → MirrorCaptureHelper 인스펙터:
   - fileName = "Mirror_Empty"
3. MirrorCaptureHelper 컴포넌트 헤더(또는 우측 ⋮) 우클릭
   → "Capture as PNG" 클릭
4. 콘솔에 "저장 완료: .../Mirror_Empty.png" 뜨면 성공
```

### 의자+조각 캡처

```
1. PhotoPiece_3 활성화 (체크박스 켬)
2. MirrorCaptureHelper의 fileName = "Mirror_WithPiece"
3. 우클릭 → "Capture as PNG"
4. 콘솔에 저장 완료 메시지 확인
```

### 정리

```
1. MirrorCaptureCam 통째로 삭제
2. PhotoPiece_3 비활성화로 원복 (게임 시작 상태)
```

### 텍스처 임포트 설정

생성된 `Assets/Textures/Mirror_Empty.png`, `Mirror_WithPiece.png` 선택 → Inspector:

- Texture Type: **Default**
- sRGB (Color Texture): **ON**
- Wrap Mode: **Clamp**
- Filter Mode: **Bilinear**
- Compression: High Quality (선택)
- **Apply**

## 6. MirrorRevealEvent 인스펙터 와이어링

Mirror_Room2 선택 → MirrorRevealEvent 컴포넌트 슬롯 채우기:

| 슬롯 | 값 |
|---|---|
| Mirror Renderer | Mirror_Room2 자신의 MeshRenderer |
| Empty Texture | `Mirror_Empty.png` |
| Reveal Texture | `Mirror_WithPiece.png` |
| Dark Color | (#080808) — 기본값 OK |
| Piece Photo Piece | `PhotoPiece_3` GameObject |
| Chair | `Chair_Room2` Transform |
| Player Camera | Main Camera (플레이어 자식) |
| Chair Look Dot | 0.5 |
| Fade Out Duration | 1 |
| Hold Duration | 0.2 |
| Fade In Duration | 1 |
| Max Wait For Lookback | 0 (무한 대기) |
| Audio Source | Mirror_Room2의 AudioSource (선택) |
| Mirror Activate Sfx | 낮은 유리 울림 SFX (선택) |
| Piece Reveal Sfx | 종이 펄럭임 SFX (선택) |

## 7. SFX (선택, 없어도 작동)

freesound.org에서 무료로 다운:
- "glass low resonance" — 거울 활성화 SFX
- "paper rustle" — 조각 등장 SFX

WAV/MP3로 다운 → Assets/Audio/ 에 import → 슬롯 연결

## 8. 플레이테스트

```
1. Play 모드 진입
2. 방2 진입 후 거울 앞으로 다가감 (3m 이내)
3. "E : 거울을 살펴본다" 프롬프트 표시 확인
4. E키 누름
5. 거울 페이드 아웃 → 의자+조각 텍스처 페이드 인 확인
6. 뒤돌아서 의자 쪽 봄
7. 실제 의자 위에 조각 등장 + 거울 빈 의자로 원복 확인
8. 조각 픽업 → PhotoPuzzleManager가 자동 처리
   - 조각 1, 2도 이미 픽업한 상태라면 → Room2AtmosphereEvent의 최종 reveal 시퀀스 발동
```

## 트러블슈팅

| 문제 | 해결 |
|---|---|
| "E : 거울을..." 프롬프트 안 뜸 | Layer가 Interact인지, BoxCollider 있는지, IsTrigger 꺼져 있는지 확인 |
| 텍스처 안 바뀜 | Mirror Renderer 슬롯 채워졌는지, M_MirrorRoom2 셰이더가 URP/Unlit인지 확인 |
| 조각 안 나옴 | Chair / Piece Photo Piece 슬롯 채워졌는지 확인. `chairLookDot`을 0.3으로 낮춰서 감지 완화 시도 |
| ContextMenu "Capture as PNG" 안 보임 | 콘솔에 컴파일 에러 있는지 확인. Camera 컴포넌트가 같은 GameObject에 있어야 함 (`[RequireComponent(typeof(Camera))]`) |
| 캡처 PNG가 검은색 | MirrorCaptureCam의 Background Type/Color 확인, FOV 너무 좁지 않은지, 의자 위치가 카메라 시야 안에 있는지 확인 |
| 거울이 어두운 채로 안 돌아옴 | Empty Texture 슬롯 비어있으면 시작 시 검정 유지됨. Mirror_Empty.png 연결 확인 |

## 다음 작업 (방2 거울 끝난 후)

```
[ ] MemoryBox.cs 작성 (보관함 + 양면 메모 트리거)
[ ] FlipNoteUI.cs 작성 (NoteUI 확장, R키 뒤집기)
[ ] 양면 메모 텍스처 2장 (앞면/뒷면)
[ ] 거울 손자국 데칼 (클리어 후 추가 생성)
[ ] SFX 6종 (벽난로/책/드론/유리/종이/보관함)
```

GAME_DESIGN.md 7번 1.5.1순위 참조.
