# 작업 인계 (2026-06-15 오후 갱신 — 집에서 이어서)

> **마감 2026-06-17 (D-2).**
> 우선순위: ① 플레이 테스트로 오늘 작업 검증 → ② 풀런 클리어 확인 → ③ 남은 SFX/폴리시.
> **원격 동기화 완료** — `origin/master`까지 푸시됨(커밋 `97e1fc1`). 집에서 `git pull` 후 이어서.

---

## ★ 집에서 먼저 — 오늘 오후 추가분(추격/문) 플레이 검증
오늘 오후 작업은 코드+씬 연결+컴파일까지 끝났지만 **플레이로 눈/귀 확인은 아직.**

- [ ] **2층 추격(거울 깨짐 트리거)** — 플래시라이트 완전 OFF(토글도 잠김) / 화면 빨갛게 / 유령 깜빡임(사라졌다 나타남) / 유령은 Crawl 애니
- [ ] **지하 추격(마지막)** — Running Crawl 애니 재생 / 플래시 밝기 45%로 줄어듦 / 빨간화면 / 깜빡임
- [ ] **발 미끄러짐** — 크롤 재생속도가 이동속도에 연동됨. 발 밀리면 `Ghost_Mesh`(WH/지하 각각)의 `Anim Move Reference`(기본 3.5) 조절
- [ ] (선택) 더 무섭게 하려면 GhostChase의 `Teleport On Blink` 켜기 — 사라진 동안 플레이어 주변으로 순간이동(불공정 즉사 주의)
- [ ] **모든 문 사운드** — 아무 문이나 열기/닫기/잠긴문 소리 나는지(WH 15 + 지하 5 = 20개 전부). 볼륨/거리감 OK?
- [ ] 지하 유령 옷(드레스) 뒤틀림은 스키닝 한계라 **그냥 두기로 함**(빨간화면+거리로 가려짐). 거슬리면 다리 모은 크롤 클립으로 교체 가능

> 참고: 추격 로직은 `GhostChase.cs`, 플래시는 `PlayerFlashlight.cs`(ForceOff/SetDim), 빨간화면은 `GameUI.cs`(런타임 오버레이). 2층 진짜 추격 유령은 `Ghost`(프록시)가 아니라 **`Ghost_Mesh`** (`Floor2MirrorEvent`가 추격시킴).

## 0. 플레이 테스트로 그 이전 추가분 검증
오늘 만든 건 전부 코드/임포트까지만 됐고 **귀로 확인은 아직 안 함.** WH 씬 플레이해서:

- [ ] **발소리** — 나무 소리 톤 OK? 너무 잦/뜸하면 `PlayerMove`의 `strideLength`(기본 1.9, 크게=뜸/작게=잦음), 시끄러우면 `footstepVolume`(0.55)
- [ ] **분위기 사운드** — 삐걱/노크/발소리/숨소리/속삭임. 너무 뜸하면 `AmbientHorrorSound.cs`의 `AddLayer` 간격 숫자 줄이기. 유령신음(숨소리/속삭임) 톤 과하면 볼륨/교체
- [ ] **촛불 정답음(Win sound)** — 호러톤에 너무 밝으면 교체 (지하 퍼즐 다 풀면 나는 소리)
- [ ] **밝기 슬라이더** — 일시정지>설정에서 밝기 움직일 때 화면 밝기 실제로 변하나? (안 변하면 `BrightnessController` Volume priority가 기존보다 낮은 것 → priority 올리기)
- [ ] **일시정지 메뉴(ESC)** — 계속하기 복귀(커서 잠김) / 설정 / 메인메뉴로 / 게임 종료 작동, 노트·슬라이딩퍼즐 열고 ESC 누르면 그것만 닫히는지(가드)
- [ ] **메뉴 버튼음** — 메인메뉴/일시정지 버튼 클릭·호버음
- [ ] **타이틀 폰트** — 메인메뉴 "MANOR" 송명체로 보이는지

## 1. 풀런(현관→엔딩) 클리어 테스트 ⭐
- [ ] 시작~지하 탈출까지 한 번에 클리어 (졸작 생존선)
- [ ] ⚠️ **소프트락 미검증**: `SR_Enter_Door`(계단방 입구, MainHall키 잠금)를 방2 안 거치고 갈 수 있는지 — 못 가면 열쇠 못 얻어 소프트락. 좌표상 계단문(0.1,-48.8)이 시작(29,-46.9)과 방2(-11.7,-36.4) 사이. 풀런으로 반드시 확인

## 2. 남은 SFX (전부 CC0로 — OpenGameArt. `Assets/Audio/CREDITS.txt` 갱신 잊지 말기)
- [ ] 점프스케어 스팅어 (StatueJumpscare 등)
- [ ] 석상 사자 파편 붕괴 (LionBreakEvent)
- [ ] 시선 퍼즐 완성음 (Room3StatueManager)
- [ ] 엔딩 SFX (EndingSequence)
- [ ] 추격 BGM (이미 Chase.wav 있음 — 쓸지 확인)
- [ ] 벽난로 불꽃·종이 타는 소리 / 거울 낮은 울림 / 종이 펄럭임 (메모리박스·미러 이벤트)
> 받는 법: OpenGameArt에서 CC0만, 페이지 라이선스 확인 → PowerShell로 다운 → Resources나 Audio 폴더 → 인스펙터/매니저 연결. (오늘 UI·촛불·분위기 다 이 방식)

## 3. 마감 직전 마무리
- [ ] **DevCheats 차단** (F1 전체열쇠/F2 전체문/F4 씬스킵 등 빌드 들어가면 안 됨)
- [ ] 빌드 한 번 뽑아보기
- [x] **커밋/푸시** — 추격 연출·문 사운드까지 `origin/master`에 푸시 완료(`97e1fc1`). (그 이전 폰트/밝기/일시정지/사운드 작업이 미커밋 상태면 그것도 확인)

---

## 오늘 오후(6/15) 완료한 것 — 추격/문 (참고)
- **git 머지 충돌 해결** — 원격 main(사운드 작업) ↔ 로컬(디테일) 머지, `WH.unity` 충돌 union 처리
- **추격 연출 강화** (`GhostChase.cs`)
  - 크롤 재생속도를 이동속도에 연동(발 미끄러짐 제거)
  - 추격 중 깜빡임(사라졌다 나타남) + 선택적 순간이동
  - `chaseClipOverride`로 추격별 애니 분리: 2층=Crawl / 지하=Running Crawl
- **플래시라이트 제어** (`PlayerFlashlight.cs`) — 2층 ForceOff(토글잠금), 지하 Dim(45%)
- **빨간 화면** (`GameUI.cs`) — 런타임 풀스크린 오버레이, 거리 가까울수록 진해지고 캐치 시 강한 플래시
- **Running Crawl.fbx → Humanoid 재임포트** — Generic이라 오버라이드 재생 안 되던 것 수정
- **모든 문 사운드 일괄 연결** — WH 15 + 지하 5개에 AudioSource(3D)+DoorOpen/Close/Lock 클립(빈 슬롯만, 커스텀 보존)

## 그 이전 오늘(6/15) 완료한 것 (참고)
- 메인메뉴 타이틀 = 송명체 폰트 / 일기·편지용 나눔펜 SDF 준비(미적용)
- 설정에 **밝기 슬라이더** 추가 (URP Post Exposure, `BrightnessController`)
- **인게임 일시정지 메뉴(ESC)** 신설 (`PauseMenu.cs`, 런타임 UI)
- 사운드(전부 CC0): UI 클릭/호버, 촛불 4종, 분위기 5종(`AmbientHorrorSound.cs`), 플레이어 나무 발소리
- 발소리 버그(isGrounded) 수정 + 나무 톤 교체

## 새로 생긴 스크립트/위치 (참고)
- `Assets/ObjectScripts/MainMenu/BrightnessController.cs` — 전역 밝기(자동 부트스트랩)
- `Assets/ObjectScripts/MainMenu/PauseMenu.cs` — 일시정지(자동 부트스트랩, 게임씬만)
- `Assets/ObjectScripts/MainMenu/UIButtonSound.cs` — 버튼 클릭/호버음 (Resources/UISfx)
- `Assets/ObjectScripts/AmbientHorrorSound.cs` — 분위기 5종 (Resources/Ambient/<카테고리>)
- `PlayerMove.cs` — 발소리 추가 (Resources/Footsteps)
- 사운드 출처: `Assets/Audio/CREDITS.txt`
