# 지하 탈출구 + 엔딩 셋업 (간단 패널 방식)

> 목표: 지하 경로 끝에 도달하면 페이드아웃 → "탈출 성공" 패널 → 끝.
> 이것만 연결하면 **현관~끝 클리어 빌드 완성**. (편지 변화 풀 엔딩 EndingSequence는 나중 폴리시)
>
> 관련 코드: `Assets/ObjectScripts/EscapeTrigger.cs` (수정 불필요, 슬롯 연결만)

---

## 1. "탈출 성공" 패널 만들기

1. Hierarchy의 지하 **Canvas** 선택 (없으면 `UI > Canvas` 생성, Render Mode = Screen Space - Overlay)
2. Canvas 우클릭 → `UI > Panel` → 이름 **`EscapePanel`**
   - Image 색: **검정 (불투명, alpha 255)** — 게임 화면을 완전히 덮게
3. EscapePanel 우클릭 → `UI > Text - TextMeshPro` → 텍스트 **"탈출 성공"** (또는 "You Escaped")
   - 가운데 정렬, 폰트 크게(80+), 흰색
   - (선택) 그 아래 작은 텍스트 "ESC로 종료" 등
4. **EscapePanel을 비활성화** — 인스펙터 좌상단 체크박스 해제 (EscapeTrigger가 도달 시 켜줌)

> ⚠️ **렌더 순서 주의**: `EscapePanel`은 Hierarchy에서 **암전 fadePanel보다 아래(나중에 그려지게)** 두세요. 안 그러면 검정 페이드 패널에 가려져 글씨가 안 보입니다.

---

## 2. 탈출구 트리거 배치

1. 지하 경로 **끝(출구/계단/현관 쪽)** 에 빈 GameObject 생성 → 이름 **`EscapeTrigger`**
2. `Add Component` → **Box Collider**
   - **Is Trigger ✅ 체크**
   - Size를 통로를 가로막을 만큼 키우기 (플레이어가 반드시 통과하게)
3. `Add Component` → **EscapeTrigger** (스크립트)

### 슬롯 연결
| 필드 | 연결할 것 |
|---|---|
| **fadePanel** | 지하 **암전 CanvasGroup** (UndergroundChaseEvent에 쓰는 그 패널 재사용 OK) |
| fadeOutDuration | 2 (기본) |
| **endingSceneName** | **비워둠** (← 비워야 같은 씬에서 패널 표시) |
| **endingPanel** | 위에서 만든 **EscapePanel** 드래그 |
| audioSource | (선택) 문소리 낼 AudioSource |
| doorOpenSound | (선택) 문 열림 SFX |

---

## 3. 동작 확인용 체크

- [ ] **Player 오브젝트 Tag = `Player`** (EscapeTrigger는 Player 태그만 인식)
- [ ] Player에 Collider + (CharacterController거나 Rigidbody) 있어 트리거 진입이 잡히는지
- [ ] EscapePanel이 시작 시 **비활성** 상태인지
- [ ] fadePanel CanvasGroup이 시작 시 alpha 0 / 또는 UndergroundChaseEvent가 관리 중인지

---

## 4. 흐름 테스트

1. 지하 진입 → 눈뜨기(페이드인)
2. 촛불 8개 순서대로 E → 열쇠 등장 → 열쇠 획득(소등 + SFX) → 귀신 추격
3. **탈출구 트리거 통과** → 이동/시점 멈춤 → (문소리) → 페이드아웃 → **"탈출 성공" 패널**
4. ⭐ **현관부터 여기까지 끊김 없이 한 번 클리어** ← 졸작 생존선 통과

---

## (나중 폴리시) 편지 변화 풀 엔딩으로 교체

`EndingSequence.cs`가 이미 완성되어 있음(편지 원본→글리치→변한 편지→크레딧).
풀 엔딩으로 바꾸려면:
1. 빈 GameObject에 `EndingSequence` 부착 → fadePanel/letterPanel/letterText(TMP)/creditsPanel 연결
2. EscapeTrigger의 **endingPanel을 비우기** (EndingSequence.Instance가 있으면 자동으로 StartEnding() 호출됨)
3. originalLetter / changedLetter 텍스트는 스크립트 인스펙터에서 편집
