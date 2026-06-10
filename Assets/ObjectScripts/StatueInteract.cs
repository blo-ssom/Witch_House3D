using UnityEngine;

/// <summary>
/// 석상 상호작용 — E키를 누르면 그 석상의 머리가 '열쇠(테이블) 쪽'을 바라본다 (Y축 회전만).
/// 처음부터 가리키지 않고, 플레이어가 건드려야 단서를 드러내는 방식.
/// 여러 석상을 건드리면 각자 같은 지점을 보게 되어, 시선이 한 곳에서 교차 = 열쇠 위치 단서.
///
/// 셋업 (Unity 에디터):
///  1. 석상 본체 GameObject: Layer=Interact, Collider(IsTrigger=OFF)
///  2. 이 컴포넌트 부착
///  3. head      → 회전할 석상 머리 Transform (비우면 자기 자신)
///  4. lookTarget→ 열쇠/테이블 위치 Transform (석상들이 바라볼 지점)
///  5. (선택) 머리 정면이 +Z가 아니면 yawOffset 으로 보정
///  6. (선택) audioSource + stoneGrindSfx 에 돌 갈리는 소리 연결
/// </summary>
public class StatueInteract : Interactable
{
    [Header("회전")]
    [Tooltip("회전시킬 머리 Transform. 비우면 이 오브젝트 자신을 회전.")]
    public Transform head;
    [Tooltip("바라볼 목표(열쇠/테이블) Transform.")]
    public Transform lookTarget;
    [Tooltip("회전 속도(클수록 빠름). 낮으면 천천히 갈리듯 돌아감.")]
    public float turnSpeed = 2f;
    [Tooltip("머리 정면이 +Z가 아닐 때 보정 각도(도).")]
    public float yawOffset = 0f;

    [Header("SFX")]
    public AudioSource audioSource;
    [Tooltip("돌 갈리는 소리 (머리 돌아갈 때 1회)")]
    public AudioClip stoneGrindSfx;

    [Tooltip("한 번 돌아본 뒤에는 다시 상호작용해도 무반응으로 둘지")]
    public bool oneShot = true;

    private bool triggered = false;
    private bool turning = false;
    private Quaternion targetRot;

    private void Start()
    {
        if (head == null) head = transform;
        if (interactPrompt == "E : 상호작용")
            interactPrompt = "[E] : 살펴보기";
    }

    public override string GetInteractPrompt()
    {
        return (oneShot && triggered) ? "" : interactPrompt;
    }

    public override void Interact(PlayerInventory playerInventory)
    {
        if (oneShot && triggered) return;
        triggered = true;

        if (lookTarget == null)
        {
            Debug.LogWarning($"{name}: lookTarget이 비어 있음");
            return;
        }

        // 목표 방향에서 수평 성분만 사용 → Y축(yaw) 회전만 계산
        Vector3 dir = lookTarget.position - head.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        float targetYaw = Quaternion.LookRotation(dir).eulerAngles.y + yawOffset;

        // 머리의 기존 X/Z(기울기)는 유지하고 Y만 교체
        Vector3 e = head.eulerAngles;
        targetRot = Quaternion.Euler(e.x, targetYaw, e.z);

        turning = true;

        if (audioSource != null && stoneGrindSfx != null)
            audioSource.PlayOneShot(stoneGrindSfx);
    }

    private void Update()
    {
        if (!turning) return;

        head.rotation = Quaternion.Slerp(head.rotation, targetRot, Time.deltaTime * turnSpeed);

        // 거의 다 돌면 정렬 후 정지
        if (Quaternion.Angle(head.rotation, targetRot) < 0.5f)
        {
            head.rotation = targetRot;
            turning = false;
        }
    }
}
