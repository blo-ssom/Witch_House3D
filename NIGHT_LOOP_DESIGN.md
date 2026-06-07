# 밤→아침 루프 + 숨기 시스템 설계 (보류 / 설계만)

> 상태: **설계만 정리. 구현 보류.** 2026-06-04 결정.
> 기존 선형 탈출 기획(`GAME_DESIGN.md` 1~8번)을 먼저 마감하고,
> 6/5 Go/No-Go 판단 후 도입 여부 결정. 마감 우선.
> 락유어도어 차용 절충안(`GAME_DESIGN.md` 부록 9)의 구체 스크립트 설계 버전.

## 목표

락유어도어식 **밤에 귀신이 배회 → 플레이어가 숨으면서 퍼즐 진행 → 아침이 되면 귀신 사라짐** 루프.

## 핵심 인식

- 처음 떠올린 "랜덤 배회 / 랜덤 출몰"만으로는 게임이 성립하지 않음.
- **진짜 핵심은 "시야 기반 감지" + "숨기"** 두 가지. 이게 없으면 숨는 의미가 없어 그냥 쫓기는 게임이 됨.
- 현재 `GhostChase.cs`는 "거리 10m 안 = 무조건 추격, 1.2m = 포착"만 구현. 추격/포착/게임오버 로직은 재사용 가능.

## 필요한 스크립트

### 1. `GhostState.cs` — `GhostChase` 상태머신 확장 (핵심, ★★☆)

기존 `Chase()` / `CatchPlayer()` / `GameOver()`는 그대로 재사용하고 앞단에 상태만 추가.

```
enum State { Dormant, Patrol, Suspicious, Chase }

Dormant   (낮)   → 비활성, agent.enabled = false   (기존 동작)
Patrol    (밤)   → NavMesh 랜덤 포인트로 배회
Suspicious       → 플레이어를 봤을 때 마지막 목격 위치로 이동 (즉시 추격 X, 긴장 빌드업)
Chase            → 기존 Chase() 그대로
```

거리 감지 → **시야 감지로 교체**가 핵심:

```csharp
bool CanSeePlayer() {
    if (HidingSpot.PlayerIsHidden) return false;              // 숨으면 안 보임
    Vector3 dir = player.position - transform.position;
    if (dir.magnitude > detectRange) return false;            // 거리
    if (Vector3.Angle(transform.forward, dir) > fovAngle/2) return false;  // 시야각
    if (Physics.Raycast(eye, dir, out hit, detectRange) && hit.transform != player)
        return false;                                          // 벽에 가림
    return true;
}
```

- 배회 부분은 `Assets/Samples/.../RandomWalk.cs` 참고 가능.

### 2. `NightCycleManager.cs` — 싱글톤, 낮/밤 루프 (★☆☆)

```
OnNightStart → 귀신 깨우기 + 랜덤 스폰 + 화면 어둡게 (Post-Processing 페이드)
OnDayStart   → 귀신 Dormant + 퍼즐 진행 허용
```

- 화면 색조 페이드는 기존 Post-Processing 프로파일의 Exposure/ColorFilter를 lerp.

### 3. 랜덤 출몰 — 별도 스크립트 불필요 (★☆☆)

- `NightCycleManager`에 `Transform[] spawnPoints` 두고 밤 시작 시 `agent.Warp(랜덤 포인트)` 한 줄.

### 4. `HidingSpot.cs : Interactable` — 숨기 (핵심, ★★☆)

```
E키 → isHidden 토글 (카메라 옷장 안으로, 플레이어 이동 잠금)
static PlayerIsHidden 플래그로 GhostState.CanSeePlayer()가 참조
귀신이 detectRange 안으로 오면 심장박동 SFX (들킬락말락 긴장)
```

- `Interactable` 상속이라 기존 `PlayerInteraction` Raycast에 자동 감지됨.

## 의존성 / 재사용 요약

- ✅ 그대로 사용: `Interactable`, `PlayerInteraction`, `DoorInteract`, `KeyItem`, `GameOverManager`, NavMesh, Post-Processing, 추격/포착 로직
- 🔧 수정: `GhostChase` → 상태머신화 (또는 새 `GhostState`로 두고 `GhostChase` 백업)
- 🆕 신규: `NightCycleManager`, `HidingSpot`

## 작업량 / 리스크

- 코드 골격 자체는 1~2일.
- 진짜 시간은 **감지/숨기 밸런스 튜닝**(시야각·범위·발각 확률)과 기존 선형 레벨을 "밤에 귀신 도는 공간"으로 재배치하는 레벨 작업.
- ⚠️ 선형 진행 ↔ 밤 루프 진행은 레벨 디자인이 충돌. 게임 구조를 통째로 바꾸는 일이라 마감(6/17) 전 기존 기획 미완성 상태에서 착수하는 건 위험.

## 도입 판단

`GAME_DESIGN.md` 부록 9.6 Go/No-Go 기준 따름. 기존 기획 마감 + 가용 시간 35h 이상일 때만 Go.
