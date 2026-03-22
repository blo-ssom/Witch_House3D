using UnityEngine;

/// <summary>
/// 플레이어를 쳐다보는 초상화.
/// 방 입장 후 일정 시간이 지나면 시선 추적 시작.
///
/// 사용법:
///  1. 시선 추적할 초상화 오브젝트에 부착
///  2. player 슬롯에 플레이어 Transform 연결
///  3. 나머지 초상화 3개는 그냥 일반 오브젝트로 배치
/// </summary>
public class PortraitTracker : MonoBehaviour
{
    [Header("Player")]
    public Transform player;

    [Header("추적 설정")]
    [Tooltip("방 입장 후 몇 초 뒤에 시선 추적 시작")]
    public float activateDelay = 2f;
    [Tooltip("시선이 돌아가는 속도 (낮을수록 천천히)")]
    public float trackSpeed = 1.5f;

    [Header("추적 축 설정")]
    [Tooltip("Y축만 회전 (벽에 걸린 액자면 true)")]
    public bool yAxisOnly = true;

    private bool isTracking = false;
    private float timer = 0f;
    private Quaternion originalRotation;

    private void Start()
    {
        originalRotation = transform.rotation;
    }

    private void Update()
    {
        if (isTracking) return;

        timer += Time.deltaTime;
        if (timer >= activateDelay)
        {
            isTracking = true;
            Debug.Log("[PortraitTracker] 시선 추적 시작");
        }
    }

    private void LateUpdate()
    {
        if (!isTracking || player == null) return;

        Vector3 direction = player.position - transform.position;

        if (yAxisOnly)
            direction.y = 0f;

        if (direction == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * trackSpeed
        );
    }

    /// <summary>
    /// 덮개 씌울 때 호출 → 시선 추적 멈춤
    /// </summary>
    public void StopTracking()
    {
        isTracking = false;
        enabled = false;
        Debug.Log("[PortraitTracker] 시선 추적 중단");
    }
}
