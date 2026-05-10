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

    Quaternion originRot;

    void Start() => originRot = transform.rotation;

    bool IsPlayerLooking()
    {
        Vector3 toHead = (transform.position - playerCamera.transform.position).normalized;
        float dot = Vector3.Dot(playerCamera.transform.forward, toHead);
        return dot > Mathf.Cos(detectionAngle * Mathf.Deg2Rad);
    }

    void Update()
    {
        if (!isActive) return;
        if (IsPlayerLooking()) return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0;
        Quaternion target = Quaternion.LookRotation(dir);
        Quaternion limited = Quaternion.RotateTowards(originRot, target, maxAngle);
        transform.rotation = Quaternion.Slerp(transform.rotation, limited, Time.deltaTime * speed);
    }

    /// <summary>
    /// 외부에서 추적을 시작시키는 진입점. KeyItem(Room2) 픽업 시 Room3StatueManager가 호출.
    /// </summary>
    public void Activate() => isActive = true;
}
