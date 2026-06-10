using UnityEngine;

/// <summary>
/// 방3 시선 사슬 퍼즐의 개별 석상.
/// 머리 회전(Y축)과 상호작용만 담당하고, 정답 판정은 StatueGazePuzzleManager가 한다.
///
/// 셋업 (Unity 에디터):
///  1. 석상 본체 GameObject: Layer=Interact, Collider(IsTrigger=OFF), 이 컴포넌트 부착
///     (기존 StatueInteract는 제거하거나 비활성 — Interactable 중복 방지)
///  2. head      → 회전할 머리 Transform (비우면 자기 자신)
///  3. (선택) 머리 정면이 +Z가 아니면 yawOffset 으로 보정
///  4. (선택) audioSource + stoneGrindSfx 에 돌 갈리는 소리
///  5. StatueGazePuzzleManager.statueOrder 에 "정답 순서대로" 등록
/// </summary>
public class StatueGazeInteract : Interactable
{
    [Header("회전")]
    [Tooltip("회전시킬 머리 Transform. 비우면 이 오브젝트 자신을 회전.")]
    public Transform head;
    [Tooltip("회전 속도(클수록 빠름). 낮으면 천천히 갈리듯 돌아감.")]
    public float turnSpeed = 2f;
    [Tooltip("머리 정면이 +Z가 아닐 때 보정 각도(도).")]
    public float yawOffset = 0f;

    [Header("SFX")]
    public AudioSource audioSource;
    [Tooltip("돌 갈리는 소리 (머리 돌아갈 때 1회)")]
    public AudioClip stoneGrindSfx;

    private StatueGazePuzzleManager manager;
    private bool locked = false;
    private bool turning = false;
    private Quaternion targetRot;

    private void Start()
    {
        if (head == null) head = transform;
        if (interactPrompt == "E : 상호작용")
            interactPrompt = "E : 석상을 살펴본다";
    }

    /// <summary>매니저가 Start에서 호출해 자신을 등록.</summary>
    public void Bind(StatueGazePuzzleManager m) => manager = m;

    /// <summary>퍼즐 종료 후 상호작용 차단 (프롬프트도 숨김).</summary>
    public void Lock() => locked = true;

    public override string GetInteractPrompt()
    {
        return locked ? "" : interactPrompt;
    }

    public override void Interact(PlayerInventory playerInventory)
    {
        if (locked || manager == null) return;
        manager.OnStatueInteracted(this);
    }

    /// <summary>
    /// 지정 지점을 바라보도록 머리를 Y축 회전.
    /// instant=true 면 즉시(시작 포즈용, 무음), false 면 끼익 소리와 함께 천천히.
    /// </summary>
    public void GazeAt(Vector3 worldPos, bool instant = false)
    {
        if (head == null) head = transform;

        Vector3 dir = worldPos - head.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        float targetYaw = Quaternion.LookRotation(dir).eulerAngles.y + yawOffset;

        // 머리의 기존 X/Z(기울기)는 유지하고 Y만 교체
        Vector3 e = head.eulerAngles;
        targetRot = Quaternion.Euler(e.x, targetYaw, e.z);

        if (instant)
        {
            head.rotation = targetRot;
            turning = false;
            return;
        }

        turning = true;
        if (audioSource != null && stoneGrindSfx != null)
            audioSource.PlayOneShot(stoneGrindSfx);
    }

    private void Update()
    {
        if (!turning) return;

        head.rotation = Quaternion.Slerp(head.rotation, targetRot, Time.deltaTime * turnSpeed);

        if (Quaternion.Angle(head.rotation, targetRot) < 0.5f)
        {
            head.rotation = targetRot;
            turning = false;
        }
    }
}
