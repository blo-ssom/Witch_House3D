# 할 일 (업데이트 2026-05-26)

> 마감 2026-06-17 (D-22) / B 전략 — 대충 다 만들고 폴리시
> 이번 주 목표 (5/30까지): **처음~끝 클리어 가능 빌드 1개** (흰 박스여도 OK)

---

## 오늘(5/24~25) 한 것

**2층 Mirror Branch — 거울 잔상 → 손자국 연출(B안)로 재설계 + 코드 완료**
- `FakeRoomMirrorEvent.cs` 재작성 (손자국 + 거울 E키 트리거) + `MirrorBranchInteract` 헬퍼 추가
- 작은 열쇠(DrawerSmall) **폐기** → south 문 자동 해제 / 친구의 방 서랍은 키 없이 열림
- `DoorInteract.ForceUnlock()` 그대로 사용
- 의도: "거울에서 뭔가 나오려 한다" 학습 → 친구의 방 7단계 '귀신이 거울에서 튀어나옴' 복선

**방2 — 재설계 결정 (코드 작업은 거의 없음)**
- 거울(`MirrorRevealEvent`) **전면 폐기** — 거울은 2층 전용 (3번째 거울은 모티프 희석)
- 퍼즐은 **조각 3개 수집 그대로** (대충 닫기로 결정). 3번째 조각 위치 = 천 덮인 가구 밑/구석 상자
- 벽난로 불 = **Particle System** (무료 에셋 Unity Particle Pack 권장)
- erasure(친구 흔적 지워짐)는 **선택** — 한 줄짜리(`SetActive(false)`)면 넣고 아니면 생략
- 방2 = *유품 보관실*로 재해석. 소파 비대칭 배치 + 천 덮개 추가 완료 (씬)

**문서 동기화**
- `TOMORROW` / `FLOOR2_SETUP` / `GAME_DESIGN` — 손자국·거울폐기 반영
- `MIRROR_SETUP.md` — 폐기 표시

---

## 할 일 (우선순위 순)

### 1. ✅ 2층 Mirror Branch 손자국 와이어링 — **완료 (5/25 밤)**
- [x] 컴파일 에러 0 확인 (옛 `FakeRoomDoorProximity`는 코드에 이미 없음)
- [x] 거울: Layer=Interact + Collider + `MirrorBranchInteract`(manager 연결)
- [x] 콘솔 위 엄마 일기 `NoteItem` (noteID = `mirrorbranch_mother_diary`)
- [x] 손자국 데칼 placeholder → `handprints[]` 등록
- [x] SouthDoor `isLocked=true`/`requiredKey=None`, 매니저 슬롯 연결
- [x] 플레이테스트: 일기→거울 E키→손자국→south 문 열림 OK

**+ 추가로 한 것 (코드/씬 정리):**
- [x] 동봉 헬퍼 2개를 별도 파일로 분리 — `MirrorBranchInteract.cs`, `Floor2NorthDoorTrigger.cs`
      (Unity는 파일당 MonoBehaviour 1개 = 파일명 클래스만 Add Component에 뜸. 동봉이면 보조 클래스가 메뉴에 안 보여서 못 붙임)
- [x] `MirrorCaptureCam` 삭제 — Audio Listener가 2개라 시계 등 3D 사운드가 무음이었음. 폐기된 방2 거울 옵션 B 잔재라 통째 삭제 안전

### 2. 방2 닫기 (1시간) — **다음 최우선**
- [ ] `Mirror_Room2` + `MirrorRevealEvent` 삭제, `PhotoPiece_3`을 새 위치(천 밑/구석)로 이동
      ⚠️ 거울이 더는 조각3을 켜주지 않음 → `PhotoPiece_3`는 **활성 + PhotoPiece(pieceID=2) + Interact 레이어 + Collider**로 직접 갖출 것
- [ ] 벽난로 불 파티클 프리팹 드롭 (무료 에셋)
- [ ] `Room2AtmosphereEvent.useMemoryBox = true` 확인 (키 이중 등장 방지)
- [ ] (선택) erasure: 조각 픽업 시 친구 흔적 `SetActive(false)` 한 줄씩
- [ ] 플레이테스트: 조각 3개 수집 → 메모 완성 → 명부 → 메인홀 열쇠

> ⚠️ **불 꺼짐 타이밍**: 벽난로 불은 *조각 수집*이 아니라 **메모를 열고 닫은 뒤**(`MemoryBox.PostMemoSequence`) 꺼진다.
> 막히면 순서대로 확인 — ① 자물쇠 떨어지나(MemoryBox `photoPuzzle` 연결?) ② E키로 메모 뜨나(Collider+Interact, FlipNoteUI+memo Sprite?) ③ 닫으면 불/열쇠 나오나(`fireplaceLight`/`fireplaceFireObject` 슬롯?). 콘솔 `[MemoryBox]` 로그 확인.

### 3. 박스 메시 + NavMesh (남으면)
- [ ] 친구의 방(16×20) / Chase Corridor(4×8) / 지하 흰 큐브
- [ ] Chase Corridor 끝 FloorBreakTrigger
- [ ] NavMesh 베이크 → 2층 end-to-end 플레이테스트

---

## 마감 여유 계산 (오늘 5/25 기준)

**남은 기계장치(mechanic) 덩어리 ≈ 6개:**
1. 2층 손자국 와이어링  2. 방2 닫기  3. 방3 석상 퍼즐
4. 지하(2차 추격 + 탈출)  5. 박스 메시 + NavMesh  6. end-to-end 클리어 테스트

**여유의 핵심 = "처음~끝 클리어되는 빌드"를 *언제* 찍느냐.**
한번 클리어만 되면 그 뒤는 전부 폴리시라 *마감 실패가 불가능*해짐. 6/17까지 남는 날이 곧 폴리시 버퍼.

| 클리어 빌드 완성일 | 폴리시 버퍼 | 체감 |
|---|---|---|
| ~5/29 | 19일 | 아주 여유 |
| ~6/1 | 16일 | 여유 |
| ~6/8 | 9일 | 빠듯하지만 OK |
| 6/15+ | 2일 | 위험 |

**내일 분량 기준:**
- **최소(안 뒤처짐)**: 1번 + 2번 = 약 2시간 → 2층·방2 두 덩어리 잠금
- **여유 만들기(앞서감)**: + 3번 박스 메시 + NavMesh → 2층을 end-to-end로 돌려봄

→ 내일 1+2만 끝내도 6개 중 2개 처리. 이 페이스(하루 2덩어리)면 **~6/1엔 클리어 빌드** = 폴리시 16일 = 여유.

---

## 5/30까지 큰 그림

| 날짜 | 목표 |
|---|---|
| 5/25 (오늘) | Mirror Branch 손자국 재설계+코드, 방2 결정, 문서 동기화 |
| 5/26 | 2층 손자국 와이어링 + 방2 닫기 (+ 박스 메시) |
| 5/27 | 방3 석상 퍼즐 + 친구의 방 7단계 작동 확인 |
| 5/28 | 지하 박스 메시 + 2차 추격 + 탈출 트리거 |
| 5/29 | 끝까지 클리어 빌드 1차 확인 |
| 5/30 | 막힌 곳 핀포인트 수정 |

**5/31~** 폴리시 순회 (조명/Fog/SFX/손자국·불 텍스처/사이트블로커).
