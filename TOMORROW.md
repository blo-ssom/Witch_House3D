# 내일 학교에서 할 일 (2026-06-15 밤 갱신)

> **마감 2026-06-17 (D-2).**
> 우선순위: ① 플레이 테스트로 오늘 작업 검증 → ② 풀런 클리어 확인 → ③ 남은 SFX/폴리시.

---

## 0. 먼저 — 플레이 테스트로 오늘 추가분 검증 (제일 중요)
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
- [ ] **커밋** (오늘 작업: 폰트/밝기/일시정지/사운드 — 아직 커밋 안 함)

---

## 오늘(6/15) 완료한 것 (참고)
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
