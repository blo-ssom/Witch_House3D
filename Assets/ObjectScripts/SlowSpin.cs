using UnityEngine;

/// <summary>
/// 천천히 Y축 회전하는 장식용 컴포넌트. (이스터에그 하트 등)
/// </summary>
public class SlowSpin : MonoBehaviour
{
    [Tooltip("초당 회전 각도(도)")]
    public float degreesPerSecond = 45f;

    private void Update()
    {
        transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.World);
    }
}
