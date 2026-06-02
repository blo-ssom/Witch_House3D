using UnityEngine;

/// <summary>
/// 플레이어가 이 영역을 지나가면 지정한 문을 닫고 영구 봉인.
/// 시작 문 너머(두 번째 방 입구 쪽)에 깔아두는 "되돌아갈 수 없는 지점".
///
/// 사용법:
///  1. 빈 GameObject → 이름: DoorSealTrigger
///  2. Box Collider 추가 → Is Trigger 체크
///  3. 문 안쪽(통과한 직후 위치)에 박스를 문 폭만큼 배치
///  4. 이 컴포넌트 부착 → targetDoors에 봉인할 문(DoorInteract)들 연결 (양문이면 2개)
///
///  ※ 한 번 발동하면 비활성화되며, 문은 게임 끝까지 잠긴 채 유지됨.
/// </summary>
public class DoorSealTrigger : MonoBehaviour
{
    [Header("봉인할 문 (양문이면 2개)")]
    public DoorInteract[] targetDoors;

    [Tooltip("닫히기 전 약간 대기 (플레이어가 완전히 통과하도록). 0이면 즉시.")]
    public float closeDelay = 0.3f;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;
        if (targetDoors == null || targetDoors.Length == 0)
        {
            Debug.LogWarning("DoorSealTrigger: targetDoors가 비어 있음");
            return;
        }

        triggered = true;

        if (closeDelay > 0f)
            Invoke(nameof(SealNow), closeDelay);
        else
            SealNow();
    }

    private void SealNow()
    {
        foreach (var door in targetDoors)
        {
            if (door != null)
                door.CloseAndSeal();
        }
    }
}
