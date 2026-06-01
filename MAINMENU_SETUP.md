# 메인메뉴 셋업 가이드

> 작성: 2026-06-01 / Layers of Fear Remake 스타일 좌측 정렬 메뉴

이미 작성 완료된 파일:
- `Assets/Editor/MainMenuBuilder.cs` — 클릭 한 번으로 UI 자동 생성
- `Assets/ObjectScripts/MainMenu/MainMenuController.cs` — 메뉴 동작 (시작/설정/나가기/페이드/BGM)
- `Assets/ObjectScripts/MainMenu/MainMenuCameraBreath.cs` — 카메라 호흡/스웨이/마우스 패럴랙스
- `Assets/ObjectScripts/MainMenu/SettingsManager.cs` — 볼륨/감도 저장(PlayerPrefs)
- `Assets/ObjectScripts/CandleFlicker.cs` — Perlin 기반 촛불 깜빡임
- `Assets/UI/MainMenu/left_gradient.png` — 좌측 검정→투명 그라데이션 (1024x512, 알파 정확)
- `Assets/PlayerScripts/PlayerLook.cs` — Start()에 `SettingsManager.LoadSensitivity()` 적용 (수정 완료)

집에서 할 작업은 **Unity Editor에서 UI 자동 빌드 + 카메라/조명/배경 배치 + Build Settings 등록**.

---

## 1. UI 자동 빌드 (1분)

### 1-1. MainMenu.unity 씬 생성
1. Unity Editor 열기
2. `Ctrl + N` → **Basic (Built-in)** 선택
3. `Ctrl + S` → `Assets/Scenes/MainMenu.unity`로 저장

### 1-2. UI 자동 생성
상단 메뉴바 → **Witch House → Build Main Menu UI** 클릭

생성되는 것:

| 오브젝트 | 내용 |
|---|---|
| **EventSystem** | UI 클릭 처리 |
| **MainMenuCanvas** | 1920x1080 ref, ScreenSpaceOverlay |
| **LeftGradient** | `left_gradient.png` 자동 로드, 좌측 800px |
| **TitleText** | "WITCH\nHOUSE", 좌측 상단, Size 88 |
| **MenuButtonsPanel** | 시작/설정/나가기 3버튼, 좌측 중앙 |
| **SettingsPanel** | Volume + Sensitivity 슬라이더 + Back 버튼 (초기 비활성) |
| **FadePanel** | 검정 + CanvasGroup |
| **MainMenuBgmSource / SfxSource** | AudioSource 2개 |
| **MainMenuManager** | MainMenuController 부착, 모든 슬롯 자동 와이어 |

자동 와이어되는 이벤트:
- 시작/설정/나가기/Back 버튼 OnClick
- Volume/Sensitivity 슬라이더 OnValueChanged
- SettingsManager의 슬롯들(슬라이더/텍스트)

### 1-3. 잘 만들어졌는지 확인
- Hierarchy에서 `MainMenuCanvas`/`MainMenuManager` 보이는지
- Console에 빨간 에러 없는지
- Play 눌러서 페이드인 + 좌측 메뉴 보이면 UI는 OK

### 트러블슈팅
- **메뉴바에 "Witch House"가 안 보임** → 컴파일 에러. Console 확인
- **"TMP 필수 리소스 없음" 다이얼로그** → Window → TextMeshPro → Import TMP Essential Resources 클릭 후 재시도
- **LeftGradient가 흰 사각형으로 보임** → `Assets/UI/MainMenu/left_gradient.png` 임포트 설정에서 Sprite Mode: Single인지 확인

---

## 2. 카메라 셋업 (5분)

### 2-1. 카메라 위치
1. Hierarchy의 **Main Camera** 선택
2. Transform 초기화 (`0, 0, 0`)
3. 책상/거울 정면 30~50cm 거리, 눈높이(약 1.6m)에 배치
4. 회전: 약간 내려다보는 각도 (X +10도 정도)

### 2-2. MainMenuCameraBreath 부착
1. Main Camera 선택 → **Add Component → Main Menu Camera Breath**
2. 기본값 그대로 사용 가능

권장 값:
| 항목 | 값 | 효과 |
|---|---|---|
| Breath Amplitude | 0.015 | 위아래 1.5cm 호흡 |
| Sway Amplitude X / Y | 0.5 / 0.7 | 살짝 두리번 |
| Slow Pan | OFF (처음엔) | 너무 정적이면 ON |
| Mouse Parallax | ON | 마우스 따라 미세 기울임 |

### 2-3. 카메라 설정
- **Background**: Solid Color, `#000000` (검정)
- **Clear Flags**: Solid Color
- **Field of View**: 60 (호러 분위기엔 좀 좁게)

---

## 3. 조명 + 분위기 (15분)

분위기의 90%가 여기서 결정됨.

### 3-1. 기본 라이트 정리
- Directional Light **삭제** (또는 Intensity 0)
- Window → Rendering → Lighting → Environment → **Ambient Color**를 `#0A0A12`로

### 3-2. 촛불 (메인 광원)
1. 빈 GameObject `Candle_Light` 생성 → 책상 위 (촛불 위치)
2. **Light** 컴포넌트 추가:
   - Type: **Point**
   - Color: `#FFAA55`
   - Intensity: 2
   - Range: 4
   - Shadows: Soft Shadows
3. **Add Component → Candle Flicker** 부착
   - Base Intensity: 2
   - Intensity Variance: 0.4
   - Intensity Speed: 3
   - Vary Color: ☑
   - Bright Color: `#FFC773`
   - Dim Color: `#FF8C4D`

### 3-3. 달빛 (보조 광원)
1. 빈 GameObject `Moon_Light` 생성 → 창가 방향
2. **Light** 컴포넌트:
   - Type: **Spot**
   - Color: `#5A7A9F`
   - Intensity: 0.4
   - Range: 8
   - Spot Angle: 45
3. 위치/회전: 창문에서 책상 방향으로 비스듬히

### 3-4. Fog
1. Window → Rendering → Lighting → Environment → **Fog** ☑
2. Color: `#0F0E15`
3. Mode: Exponential
4. Density: 0.05

### 3-5. Post Processing (필수)
1. Hierarchy 우클릭 → Volume → **Global Volume**
2. Inspector의 Volume → **New Profile** 클릭
3. **Add Override**로 다음 추가:

| Override | 설정 |
|---|---|
| **Vignette** | Intensity 0.45, Smoothness 0.3 |
| **Bloom** | Threshold 0.9, Intensity 0.6, Scatter 0.7 |
| **Color Adjustments** | Saturation -30, Contrast +15, Post Exposure +0.2 |
| **Film Grain** | Type Medium2, Intensity 0.3 |
| **Chromatic Aberration** | Intensity 0.1 (살짝만) |

4. Main Camera 선택 → Inspector → **Rendering** 섹션 → **Post Processing** ☑

---

## 4. 배경 (방2 재활용, 20분)

가장 빠른 방법: WH 씬의 방2를 그대로 가져오기.

### 4-1. 방2 복사
1. **WH.unity** 열기
2. Hierarchy에서 방2 GameObject 찾기 (책상/거울/촛불/가구가 든 부모)
3. 우클릭 → **Copy**
4. **MainMenu.unity** 열기
5. Hierarchy 우클릭 → **Paste**

### 4-2. 게임 로직 제거
복사한 방2에서 메뉴엔 필요 없는 컴포넌트 제거 (오브젝트 자체는 유지, 컴포넌트만 Remove):
- `MemoryBox`, `PhotoPiece`, `Interactable` 상속들
- `MirrorRevealEvent` (이미 폐기됐다면 무시)
- `NoteItem`, `Room2AtmosphereEvent`, `PhotoPuzzleManager`
- Trigger Collider들

남길 것: **시각적 메시 + 머티리얼 + 라이트만**.

### 4-3. 카메라가 잘 잡히는 구도 만들기
- 카메라 시야에 책상/촛불/거울이 균형 있게 들어오게
- LeftGradient가 깔리는 좌측은 어둑하고, 우측에 디테일이 보이게

---

## 5. 오디오 (10분)

### 5-1. BGM
1. 무료 사이트에서 다운 (예: freesound.org, "horror ambient drone" 검색)
2. `Assets/Audio/MainMenu/bgm_main.wav` 임포트
3. MainMenuManager의 **Bgm Clip** 슬롯에 드래그
4. **Bgm Fade In Volume**: 0.4 (낮게)
5. **Bgm Fade In Duration**: 3 (천천히)

### 5-2. SFX (선택)
- **Hover Clip**: 종이 바스락 짧은 클립 → MainMenuManager의 `Hover Clip`
- **Click Clip**: 미세한 메탈릭 핑 → `Click Clip`

### 5-3. 호버 사운드 활성화 (선택)
각 버튼(시작/설정/나가기/Back)에:
1. **Add Component → Event Trigger**
2. **Add New Event Type** → **Pointer Enter**
3. + 버튼 → MainMenuManager 드래그 → `MainMenuController.OnHover` 선택

---

## 6. Build Settings 등록 (1분)

1. **File → Build Profiles** (또는 Build Settings, Ctrl+Shift+B)
2. **Add Open Scenes** 또는 직접 드래그
3. 씬 순서:
   - **인덱스 0**: `MainMenu.unity` ★ 반드시 위
   - **인덱스 1**: `WH.unity`
   - **인덱스 2**: `UnderGround.unity`
4. 인덱스 0이 게임 시작 시 처음 로드됨

---

## 7. 최종 테스트

Play 누르고 확인:
- [ ] 검정 화면에서 시작 → 1.5초 페이드인
- [ ] 카메라가 미세하게 호흡/스웨이
- [ ] 마우스 움직이면 카메라 살짝 따라옴 (Mouse Parallax)
- [ ] 좌측에 WITCH HOUSE 타이틀 + 시작/설정/나가기
- [ ] 마우스 호버 시 텍스트가 흰색으로 천천히 페이드
- [ ] 시작 → 페이드아웃 → WH 씬 로드
- [ ] 설정 → 패널 등장, 슬라이더 작동, Back → 메뉴 복귀
- [ ] 나가기 → 페이드 후 (에디터에선 Play 멈춤)
- [ ] BGM이 페이드인됨
- [ ] 게임 중 ESC로 메뉴 복귀는 별도 구현 (이번 작업 범위 아님)

---

## 8. 폴리시 (선택, 시간 남으면)

### 한글 폰트 (좀 더 분위기)
- 본명조 (Adobe Source Han Serif KR) 다운로드
- TMP Font Asset Creator로 변환:
  - Atlas Resolution: 4096x4096
  - Character Set: Unicode Range (Hex)
  - 입력: `0020-007E,AC00-D7A3,3131-318E`
- 만든 Font Asset을 TitleText / 각 Button의 Label에 적용

### LeftGradient 강도 조정
- 너무 어두우면 LeftGradient Image 컴포넌트의 Color alpha를 200 정도로
- 너무 좁으면 RectTransform의 Width를 1000~1200으로

### 버전 표기 (좌하단)
- Canvas 자식으로 TMP 추가
- 내용: `v0.4`, Color `#606060`, Size 18, Anchor bottom-left

---

## 트러블슈팅

**Q. UI가 너무 작거나 크게 나옴**
A. CanvasScaler의 Match가 0.5인지 확인. 화면 비율 따라 1로 바꾸기도.

**Q. 마우스가 안 보임**
A. MainMenuController.Start()에서 Cursor.lockState = None 설정함. 안 되면 Inspector에서 직접 풀기.

**Q. 폰트가 □□□로 보임**
A. TMP 폰트에 그 문자가 없음. 영문으로 바꾸거나 한글 폰트 임포트.

**Q. 페이드인이 안 되고 검정 화면 그대로**
A. MainMenuManager의 Fade Panel 슬롯이 비었나 확인. 또는 FadePanel의 CanvasGroup이 활성인지.

**Q. 시작 버튼 눌러도 씬 전환 안 됨**
A. MainMenuManager의 Game Scene Name이 "WH"인지 + Build Settings에 WH.unity 등록됐는지.

**Q. 카메라가 안 움직임 (정적)**
A. MainMenuCameraBreath 부착 안 됐거나 enableBreath/enableHeadSway 둘 다 꺼짐.

---

## 시간 견적

| 작업 | 시간 |
|---|---|
| 1. UI 자동 빌드 | 1분 |
| 2. 카메라 셋업 | 5분 |
| 3. 조명 + 분위기 | 15분 |
| 4. 방2 복사 + 정리 | 20분 |
| 5. 오디오 | 10분 |
| 6. Build Settings | 1분 |
| 7. 테스트 + 튜닝 | 10분 |
| **합계** | **약 1시간** |

폴리시(한글폰트, 버전 표기 등) 포함 시 1.5~2시간.

---

## 컬러 팔레트 (복붙용)

| 용도 | HEX |
|---|---|
| 기본 텍스트 | `#C0B0A0` |
| 호버 텍스트 | `#FFFFFF` |
| 누름 텍스트 | `#807060` |
| 슬라이더 BG | `#1F1F1F` |
| 슬라이더 Fill | `#A09080` |
| FadePanel | `#000000` |
| 촛불 색 | `#FFAA55` (밝게) / `#FF8C4D` (어둡게) |
| 달빛 색 | `#5A7A9F` |
| Ambient | `#0A0A12` |
| Fog | `#0F0E15` |
