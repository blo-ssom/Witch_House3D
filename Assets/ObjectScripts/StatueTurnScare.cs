using UnityEngine;

/// <summary>
/// 복도 점프스케어 — 플레이어가 모퉁이를 도는 순간,
/// 등지고 서 있던 기사동상이 휙 돌아본다 (rotation Y 스냅 + 스팅 SFX).
///
/// 동작:
///  1. 플레이어가 트리거 존 진입 → 동상 rotation Y를 targetYAngle로 즉시 변경 (1회)
///  2. 동시에 스팅 SFX 재생
///
/// 셋업 (Unity 에디터):
///  1. 모퉁이 근처에 빈 GameObject + BoxCollider(IsTrigger=ON) + 이 컴포넌트
///  2. statue → 기사동상 Transform
///  3. targetYAngle → 돌아볼 각도 (기본 130)
///  4. (선택) audioSource + stingSound — audioSource 비워두면 동상 위치에서 3D 재생
/// </summary>
public class StatueTurnScare : MonoBehaviour
{
    [Header("동상")]
    [Tooltip("돌아볼 기사동상 Transform.")]
    public Transform statue;
    [Tooltip("발동 시 적용할 Y 회전 각도 (월드 기준 eulerAngles.y).")]
    public float targetYAngle = 130f;

    [Header("SFX")]
    public AudioSource audioSource;
    [Tooltip("돌아보는 순간 스팅 효과음. 비워두면 무음.")]
    public AudioClip stingSound;
    [Range(0f, 1f)] public float stingVolume = 1f;

    private bool done = false;

    private void OnTriggerEnter(Collider other)
    {
        if (done) return;
        if (!other.CompareTag("Player")) return;
        if (statue == null) return;

        done = true;

        Vector3 euler = statue.eulerAngles;
        euler.y = targetYAngle;
        statue.eulerAngles = euler;

        if (stingSound != null)
        {
            if (audioSource != null)
                audioSource.PlayOneShot(stingSound, stingVolume);
            else
                AudioSource.PlayClipAtPoint(stingSound, statue.position, stingVolume);
        }

        Debug.Log("[StatueTurnScare] 발동 — 동상이 돌아봄 (Y=" + targetYAngle + ")");
    }
}
