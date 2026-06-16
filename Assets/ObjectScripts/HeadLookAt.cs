using UnityEngine;

public class HeadLookAt : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] Camera playerCamera;
    [SerializeField] float maxAngle = 60f;
    [SerializeField] float speed = 2f;
    [SerializeField] float detectionAngle = 30f;

    [Tooltip("처음부터 추적 활성화. 4개 단서 석상은 false로 두고 키 획득 시 Activate() 호출.")]
    [SerializeField] bool isActive = true;

    [Tooltip("켜면 플레이어가 쳐다봐도 멈추지 않고 계속 추적(감시). 끄면 위핑엔젤식(안 볼 때만 회전).")]
    [SerializeField] bool alwaysTrack = false;

    [Header("회전 사운드 (돌 긁힘)")]
    [Tooltip("석상이 돌아갈 때 재생할 사운드(루프). 비우면 무음.")]
    public AudioClip slideSfx;
    [Tooltip("이 각도(도/프레임) 이상 움직일 때만 소리 — 미세 떨림 무시")]
    public float rotateSoundThreshold = 0.06f;
    [Range(0f, 1f)] public float slideVolume = 0.8f;

    Quaternion originRot;
    AudioSource audioSrc;

    void Start()
    {
        originRot = transform.rotation;

        if (slideSfx != null)
        {
            audioSrc = GetComponent<AudioSource>();
            if (audioSrc == null) audioSrc = gameObject.AddComponent<AudioSource>();
            audioSrc.clip = slideSfx;
            audioSrc.loop = true;
            audioSrc.playOnAwake = false;
            audioSrc.spatialBlend = 1f;   // 3D — 석상 위치에서 들림
            audioSrc.volume = slideVolume;
            audioSrc.minDistance = 2f;
            audioSrc.maxDistance = 22f;
        }
    }

    bool IsPlayerLooking()
    {
        Vector3 toHead = (transform.position - playerCamera.transform.position).normalized;
        float dot = Vector3.Dot(playerCamera.transform.forward, toHead);
        return dot > Mathf.Cos(detectionAngle * Mathf.Deg2Rad);
    }

    void Update()
    {
        if (!isActive) { StopSlide(); return; }
        if (!alwaysTrack && IsPlayerLooking()) { StopSlide(); return; }   // 위핑엔젤: 쳐다보면 멈춤 (alwaysTrack이면 계속 추적)

        Vector3 dir = player.position - transform.position;
        dir.y = 0;
        Quaternion target = Quaternion.LookRotation(dir);
        Quaternion limited = Quaternion.RotateTowards(originRot, target, maxAngle);

        Quaternion before = transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, limited, Time.deltaTime * speed);
        float moved = Quaternion.Angle(before, transform.rotation);

        SetSlide(moved > rotateSoundThreshold);
    }

    void SetSlide(bool moving)
    {
        if (audioSrc == null) return;
        if (moving && !audioSrc.isPlaying) audioSrc.Play();
        else if (!moving && audioSrc.isPlaying) audioSrc.Stop();
    }

    void StopSlide()
    {
        if (audioSrc != null && audioSrc.isPlaying) audioSrc.Stop();
    }

    /// <summary>
    /// 외부에서 추적을 시작시키는 진입점. KeyItem(Room2) 픽업 시 Room3StatueManager가 호출.
    /// </summary>
    public void Activate() => isActive = true;
}
