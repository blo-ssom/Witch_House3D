using UnityEngine;

/// <summary>
/// 방2 진입 시 입구 문을 닫고 봉인(가둠). 슬라이딩 퍼즐을 풀면
/// SlidingPuzzleInteract.OnSolved()가 exitDoor의 봉인을 해제해 다시 나갈 수 있게 한다.
///
/// 사용법:
///  1. 방2 입구 '안쪽'에 빈 GameObject + BoxCollider(IsTrigger=ON)를 문 폭만큼 배치
///  2. 이 컴포넌트 부착 → door 에 입구 DoorInteract 연결
///  3. 같은 door를 SlidingPuzzleInteract.exitDoor 에도 연결(완성 시 해제용)
/// </summary>
[RequireComponent(typeof(Collider))]
public class Room2EntryLock : MonoBehaviour
{
    [Tooltip("방2 입구 문 (진입 시 닫고 봉인)")]
    public DoorInteract door;

    private bool triggered = false;

    private void Reset()
    {
        var c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;
        if (door == null) { Debug.LogWarning("[Room2EntryLock] door 미연결"); return; }

        triggered = true;
        door.CloseAndSeal();   // 닫고 봉인 — 퍼즐 풀 때까지 못 나감
        Debug.Log("[Room2EntryLock] 방2 입구 봉인 — 퍼즐을 풀어야 열린다");
    }
}
