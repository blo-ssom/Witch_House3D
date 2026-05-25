using UnityEngine;

/// <summary>
/// 친구의 방 north 문 근접 트리거 (helper). 7단계 발동.
///
/// 사용법:
///  - north 문 앞에 빈 GameObject + BoxCollider(IsTrigger=ON) + 본 컴포넌트
///  - target에 Floor2MirrorEvent 드래그
///  - 시작 시 GameObject 비활성 — Floor2MirrorEvent가 6단계에서 자동 활성화
/// </summary>
public class Floor2NorthDoorTrigger : MonoBehaviour
{
    public Floor2MirrorEvent target;
    public string playerTag = "Player";
    private bool fired = false;

    private void OnTriggerEnter(Collider other)
    {
        if (fired || target == null) return;
        if (!other.CompareTag(playerTag)) return;
        fired = true;
        target.TriggerNorthApproach();
    }
}
