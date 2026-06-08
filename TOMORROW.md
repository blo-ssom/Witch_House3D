# 할 일 (2026-06-08 갱신)

> **마감 2026-06-17 (D-9).**
> **최우선 = 처음~끝(현관→지하 탈출) 클리어되는 빌드 1개. 흰 박스여도 OK.**
> 한 번 클리어만 되면 그 뒤는 전부 폴리시 버퍼 = 마감 실패 불가능. 신기능보다 "끝까지 굴러가게"가 절대 우선.

---

## 오늘(6/8) 완료한 것
- ✅ 마지막 방(친구의 방) 단순화 — 미스디렉션 폐기, **조건3/F7 → south잠금 → 조명다운 → north개방 → 거울깨짐 → 귀신등장 → 추격** (모션까지 작동 확인)
- ✅ Chase Corridor → 바닥꺼짐 → 지하 전환 확인
- ✅ 지하: 공간 메시 + NavMesh 베이크 + PlayerSpawnPoint + 눈뜨기(암전 Canvas + UndergroundChaseEvent fadePanel)
- ✅ 코드: GhostChase에 player 태그 자동검색 추가 (지하 귀신용)

---

## 집에서 할 일 — 지하 마무리 (이것만 끝내면 클리어 빌드 완성! 🎯)

### 5. 지하 귀신 배치
- [ ] 추격 귀신 prefab 복제 (마지막 방 realGhost 또는 1층 귀신 그대로)
- [ ] **GhostChase + NavMeshAgent** 붙어있는지 확인 / **Default 레이어**
- [ ] **시작 시 비활성**(체크 해제)으로 둠 — 제단 조사 후 등장
- [ ] **NavMesh 위**, 플레이어 스폰에서 좀 떨어진 위치
- [ ] player 슬롯은 **비워둬도 됨**(자동검색 추가함)
- [ ] Animator: **Apply Root Motion 끄기** + 걷기 클립 **Loop Time 켜기** (안 그럼 모션 끊김)

### 6. 매니저 슬롯 채우기
- [ ] `UndergroundManager`(UndergroundChaseEvent)에:
  - [ ] `ghostObject` ← 지하 귀신 GameObject
  - [ ] `ghostChase` ← 지하 귀신의 GhostChase

### 7. 제단
- [ ] Cube 하나(제단 모양) + **Collider**
- [ ] Add Component → **AltarInteractable**
- [ ] **Layer = Interact** (E키 조사 가능하게)

### 8. 탈출구 + 엔딩
- [ ] 빈 GameObject + **BoxCollider (IsTrigger 켜기)**
- [ ] Add Component → **EscapeTrigger**
- [ ] `fadePanel` 연결 (암전 Canvas)
- [ ] `endingPanel`에 간단한 "탈출 성공" 패널 연결 (편지 변화 풀연출 EndingSequence는 나중 폴리시)
- [ ] 탈출구를 지하 경로 끝(현관/출구)에 배치

### 테스트
- [ ] 지하 들어가면 눈뜸 → 돌아다님 → 제단 E → 1.5초 후 귀신 추격 → 탈출구 도달 → 엔딩
- [ ] ⚠️ **NavMesh 베이크됐는지 + 귀신이 NavMesh 위인지** 확인 (안 그럼 추격 안 됨)
- [ ] ⭐ **현관부터 끝까지 한 번 클리어** ← 이게 진짜 목표. 되면 졸작 생존선 통과

---

## 그 다음 (클리어 빌드 나온 뒤에만)
- [ ] **촛불 순서 퍼즐** (락유어도어식) — 마지막 방 트리거를 조건3 → 촛불 순서로 교체. 촛불 N개 + 정해진 순서대로 E키 → 틀리면 리셋 / 맞으면 거울 깨짐. (마지막 방은 이미 굴러가니 트리거만 바꿔 끼우면 됨)
- [ ] 폴리시: 거울 파편 파티클, 엔딩 편지 변화 연출, 사운드, 조명, Fog

---

## 참고 메모

**이미 작동하는 코드 (배치만 하면 됨)**
- 마지막 방: `Floor2MirrorEvent`(단순화됨)
- 지하: `UndergroundChaseEvent` / `AltarInteractable` / `EscapeTrigger` / `EndingSequence`
- `GhostChase`: player 태그 자동검색 추가됨

**디버그 치트 (DevCheats — F9로 도움말 표시)**
- F1 모든 열쇠 / F2 모든 문 해제 / F3 속도부스트 / F4 다음 씬 / F5 씬 재시작 / F6 바라보는 곳 순간이동 / **F7 마지막 방 거울 시퀀스 강제시작**

**막히면 체크 (추격 안 될 때)**
1. NavMesh 베이크됐나
2. 귀신이 NavMesh 위인가
3. NavMeshAgent + GhostChase 둘 다 있나
