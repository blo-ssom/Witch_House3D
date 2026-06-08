# 지하 촛불 순서 퍼즐 셋업 가이드

> 기획: 지하(UnderGround) 엔딩 시퀀스를 **제단 조사 폐기 → 촛불 8개 순서 퍼즐**로 변경.
> 촛불을 정해진 순서대로 켜면 열쇠 등장 → 열쇠 획득 시 촛불 일제히 소등 + 마지막 유령 추격 → 탈출 → 엔딩.

## 동작 흐름

```
눈뜨기(UndergroundChaseEvent 페이드인)
  → 촛불 8개를 정해진 순서대로 E
      ├ 맞으면: 해당 촛불 점등, 다음 단계로
      └ 틀리면: 전체 리셋 (다시 처음부터)
  → 8개 다 맞음 → solveSound + 열쇠(keyObject) 등장
  → 열쇠 E로 획득
      → KeyItem.onPickup → CandlePuzzleManager.OnKeyCollected()
      → candleOutSound + 촛불 전체 소등 + 잠금
      → chaseDelay(0.4s) 후 UndergroundChaseEvent.TriggerChase()
  → 유령 활성 + 추격 → EscapeTrigger 도달 → 엔딩
```

## 스크립트

| 파일 | 역할 |
|---|---|
| `Assets/ObjectScripts/CandleInteractable.cs` | 개별 촛불. flameVisual/flameLight On/Off, E키 시 매니저에 알림 |
| `Assets/ObjectScripts/CandlePuzzleManager.cs` | 순서 판정·리셋·열쇠 등장·일제 소등·추격 트리거 |
| `Assets/ObjectScripts/KeyItem.cs` (수정) | `onPickup` UnityEvent 추가 — 획득 순간 훅 |
| `Assets/ObjectScripts/UndergroundChaseEvent.cs` (수정) | public `TriggerChase()` 추가 (제단 방식 OnAltarInvestigated도 호환 유지) |
| `Assets/PlayerScripts/KeyType.cs` (수정) | `Underground` 값 추가 |

> **정답 순서 = `candleOrder` 리스트의 등록 순서**(Element 0 = 1번째). 코드 수정 없이 인스펙터 드래그로 변경.

---

## 1. 촛불 8개 배치

1. 촛불 오브젝트 8개를 **원형(시계 모양)** 으로 배치
   - 들어오기 전 방의 손그림 메모(시계 배치 + 잇는 선/화살표)와 **개수·각도를 맞춰야** 공정한 퍼즐이 됨
   - **빨간 점 = 시작 촛불**: 1개만 색/위치/크기를 다르게 해서 "여기서 시작"이 명확하게
2. 각 촛불에:
   - **Collider** (Raycast 감지용) + **Layer = Interact**
   - `CandleInteractable` 컴포넌트 부착

| CandleInteractable 필드 | 연결 |
|---|---|
| flameVisual | 불꽃 파티클/메시 오브젝트 (점·소등 시 자동 On/Off) |
| flameLight | Point Light (점·소등 시 자동 On/Off) |
| audioSource / igniteSound / extinguishSound | (선택) 촛불별 개별 효과음 — 매니저가 일괄 처리하면 비워둬도 됨 |
| promptMessage | "촛불" 등 |

## 2. CandlePuzzleManager 부착

빈 GameObject 생성 → `CandlePuzzleManager` 부착:

| 필드 | 값 / 연결 |
|---|---|
| candleOrder | 촛불 8개를 **메모 그림 순서대로** 드래그 (Element 0 = 빨간 시작 촛불) |
| startLit | **해제** (꺼진 채 시작 → 켜는 순서. 점등으로 진행 피드백) |
| keyObject | 정답 시 등장할 열쇠 GameObject (시작 시 자동 비활성됨) |
| solveSound | (선택) 열쇠 등장 SFX |
| candleOutSound | 촛불 꺼지는 사운드 (열쇠 획득 시 1회 재생) |
| chaseDelay | 0.4 (소등 후 추격까지 딜레이) |
| audioSource | 매니저용 AudioSource |

> `startLit`을 체크하면 "켜진 채 시작 → 끄는 순서" 퍼즐로 자동 전환됨(대칭 동작).

## 3. 열쇠(KeyItem) 설정

정답 시 등장할 열쇠 오브젝트:

| 필드 | 값 |
|---|---|
| keyType | **Underground** (의식용 — 문 해제보다 연출 트리거 목적) |
| pickupSound | (선택) 줍는 소리 |
| **onPickup** | → **CandlePuzzleManager.OnKeyCollected** 드래그 연결 |

> 열쇠 GameObject는 매니저 `keyObject`에도 연결 → 시작 시 자동 비활성, 정답 시 활성.

## 4. UndergroundChaseEvent (기존 그대로)

- 눈뜨기 페이드인 + 추격 시퀀스 담당. **그대로 둠.**
- 추격은 이제 제단이 아니라 촛불 퍼즐이 `TriggerChase()`로 시작.
- **제단 / AltarInteractable 오브젝트는 더 이상 필요 없음** (씬에서 제거 또는 비활성). 코드는 호환을 위해 `OnAltarInvestigated()`도 남겨둠 — 안 지워도 컴파일·동작 정상.

| UndergroundChaseEvent 필드 | 연결 |
|---|---|
| fadePanel | 검정 CanvasGroup (시작 시 alpha=1) |
| ghostObject | 지하 귀신 GameObject (시작 시 비활성) |
| ghostChase | 지하 귀신의 GhostChase |
| playerMove / playerLook | 비워둬도 됨 (Player 태그 자동검색) |
| chaseStartDelay | 1.5 (추격 시작까지 딜레이) |

## 5. 탈출구 + 엔딩 (기존 그대로)

`EscapeTrigger` — 빈 GameObject + **BoxCollider(Is Trigger)**, 지하 경로 끝에 배치. `fadePanel` + `endingPanel`(또는 `endingSceneName`) 연결.

---

## 테스트 체크리스트

1. 지하 진입 → 페이드인(눈뜨기) → 자유 탐색
2. 촛불에 조준 시 프롬프트 표시, E로 점등
3. **틀린 순서** → 전체 소등 + 처음부터 (정상)
4. **맞는 순서 8개** → solveSound + 열쇠 등장
5. 열쇠 E → 촛불 꺼지는 소리 + 전체 소등 + 어두워짐
6. 0.4s 후 유령 등장 + 추격
7. 탈출구 도달 → 페이드아웃 → 엔딩
8. ⭐ **현관~끝 한 번 클리어** (졸작 생존선)

## 문제 해결

| 증상 | 점검 |
|---|---|
| 촛불 E가 안 됨 | Collider 있는지, Layer=Interact인지, CandleInteractable 부착됐는지 |
| 순서 맞춰도 무반응 | candleOrder에 8개 다 등록됐는지, 같은 촛불 중복/누락 없는지 |
| 점등/소등 시각 변화 없음 | flameVisual/flameLight 연결됐는지 |
| 열쇠가 안 나옴 | keyObject 연결 + 시작 시 비활성인지, currentStep이 8 도달하는지(Console 로그) |
| 열쇠 주워도 추격 안 됨 | KeyItem.onPickup에 OnKeyCollected 연결됐는지, UndergroundChaseEvent.Instance 존재하는지 |
| 추격은 시작되는데 귀신이 안 움직임 | NavMesh 베이크됐는지, 귀신이 NavMesh 위인지, NavMeshAgent+GhostChase 둘 다 있는지 |
| 메모 단서대로 풀어도 틀림 | 지하 촛불 배치 순서/각도가 메모 그림과 일치하는지, candleOrder 드래그 순서가 메모와 같은지 |
