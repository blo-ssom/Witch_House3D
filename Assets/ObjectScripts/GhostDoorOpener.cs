using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 귀신이 일정 거리 안으로 다가오면 주변 문(DoorInteract)을 자동으로 연다.
/// NavMeshAgent가 닫힌 문을 통과하지 못하는 문제를 해결.
///
/// 사용법:
///  1. Ghost 오브젝트(또는 빈 매니저)에 부착
///  2. openRange 안의 모든 DoorInteract를 자동 감지하여 강제 개방
///  3. 특정 문만 대상으로 하고 싶으면 targetDoors에 직접 할당
/// </summary>
public class GhostDoorOpener : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("이 거리 안의 문을 자동으로 연다")]
    public float openRange = 3f;

    [Tooltip("체크 주기 (초). 0이면 매 프레임")]
    public float checkInterval = 0.2f;

    [Header("대상 문 (비워두면 씬 전체에서 자동 탐색)")]
    public List<DoorInteract> targetDoors = new List<DoorInteract>();

    [Header("연동 (선택)")]
    [Tooltip("이 GhostChase가 추격 중일 때만 동작. 비워두면 항상 동작")]
    public GhostChase ghostChase;

    private float timer;

    private void Start()
    {
        if (targetDoors.Count == 0)
            targetDoors.AddRange(FindObjectsByType<DoorInteract>(FindObjectsSortMode.None));
    }

    private void Update()
    {
        if (ghostChase != null && !ghostChase.isChasing) return;

        timer += Time.deltaTime;
        if (timer < checkInterval) return;
        timer = 0f;

        float sqrRange = openRange * openRange;
        Vector3 myPos = transform.position;

        foreach (var door in targetDoors)
        {
            if (door == null) continue;
            if ((door.transform.position - myPos).sqrMagnitude <= sqrRange)
                door.ForceOpen(transform);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, openRange);
    }
}
