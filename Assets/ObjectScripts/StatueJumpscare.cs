using UnityEngine;

/// <summary>
/// 방3 점프스케어 — 복도 끝에 멀리 서 있던 석상이,
/// 퍼즐을 풀고(방2 열쇠 획득) 문을 나서는 순간 눈앞에 와 있다.
///
/// 흐름:
///  1. 시작: statue가 복도 끝(멀리)에 보이게 서 있음 (복선)
///  2. 방2 열쇠 획득 → Room3StatueManager.ActivateAll()이 Arm() 호출
///  3. 플레이어가 문 앞 트리거 진입 → 석상이 nearPoint로 순간이동 + 스팅 SFX (1회)
///
/// 셋업 (Unity 에디터):
///  1. 문 바로 바깥에 빈 GameObject + BoxCollider(IsTrigger=ON) + 이 컴포넌트
///  2. statue    → 점프스케어용 석상 Transform (복도 끝에 배치해 둘 것)
///  3. nearPoint → 놀래킬 위치 빈 Transform (문 앞 2~3m, 동선 살짝 옆. 회전값 = 석상이 바라볼 방향)
///  4. (선택) audioSource + stingSound 에 스팅 사운드
/// </summary>
public class StatueJumpscare : MonoBehaviour
{
    [Header("석상")]
    [Tooltip("점프스케어용 석상. 평소엔 복도 끝에 서 있음.")]
    public Transform statue;
    [Tooltip("놀래킬 위치/방향. 이 Transform의 위치·회전을 그대로 가져감.")]
    public Transform nearPoint;

    [Header("발동 후 변화 (선택)")]
    [Tooltip("점프스케어 순간 활성화될 오브젝트 — 피눈물 등. 시작 시 자동 비활성.")]
    public GameObject revealOnScare;

    [Header("SFX")]
    public AudioSource audioSource;
    [Tooltip("등장 순간 스팅(찌르는 효과음). 비워두면 무음.")]
    public AudioClip stingSound;
    [Range(0f, 1f)] public float stingVolume = 1f;

    private bool armed = false;
    private bool done = false;

    /// <summary>방2 열쇠 획득 시 Room3StatueManager가 호출 — 점프스케어 활성화.</summary>
    public void Arm() => armed = true;

    private void Start()
    {
        if (revealOnScare != null)
            revealOnScare.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!armed || done) return;
        if (!other.CompareTag("Player")) return;
        if (statue == null || nearPoint == null) return;

        done = true;
        statue.position = nearPoint.position;
        statue.rotation = nearPoint.rotation;

        // 발동 후 변화 — 피눈물 등 (처음 볼 땐 없던 것)
        if (revealOnScare != null)
            revealOnScare.SetActive(true);

        if (stingSound != null)
        {
            if (audioSource != null)
                audioSource.PlayOneShot(stingSound, stingVolume);
            else
                AudioSource.PlayClipAtPoint(stingSound, nearPoint.position, stingVolume);
        }

        Debug.Log("[StatueJumpscare] 발동 — 석상이 눈앞으로 이동");
    }
}
