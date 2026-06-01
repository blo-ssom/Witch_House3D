using UnityEngine;

/// <summary>
/// UnderGround 씬에서 Player가 시작할 위치 마커.
/// PlayerPersistence가 씬 로드 직후 FindFirstObjectByType으로 찾아 텔레포트.
///
/// 사용법:
///  1. UnderGround.unity 씬 열기
///  2. 빈 GameObject 생성 → 이름 "PlayerSpawnPoint"
///  3. 이 컴포넌트 부착 + 원하는 시작 위치/회전으로 배치
///  4. 회전은 Y축(좌우)만 의미 있음 (PlayerLook이 X축 카메라 회전 담당)
/// </summary>
public class UndergroundSpawnPoint : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, transform.forward * 1.5f);
    }
}
