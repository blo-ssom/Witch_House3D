using UnityEngine;

public class DoorInteract : Interactable
{
    public AudioSource audioSource;
    public AudioClip doorOpenSound;
    public AudioClip doorCloseSound;
    public AudioClip doorLockedSound;
    public Transform doorPivot;
    public float openAngle = 90f;
    public float openSpeed = 4f;

    public bool isLocked = true;
    public KeyType requiredKey = KeyType.None;   // 추가
    public bool isSealed = false;                // 영구 봉인(시작 문 등) — 어떤 열쇠로도 안 열림

    private bool isOpen = false;
    private Quaternion closedRotation;
    private Quaternion openedRotation;

    public override string GetInteractPrompt()
    {
        return "[E]";
    }

    private void Start()
    {
        if (doorPivot == null)
            doorPivot = transform;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        closedRotation = doorPivot.localRotation;
    }

    private void Update()
    {
        Quaternion targetRotation = isOpen ? openedRotation : closedRotation;

        doorPivot.localRotation = Quaternion.Slerp(
            doorPivot.localRotation,
            targetRotation,
            Time.deltaTime * openSpeed
        );
    }

    public override void Interact(PlayerInventory playerInventory)
    {
        // 영구 봉인된 문은 어떤 상호작용도 막음
        if (isSealed)
        {
            if (audioSource != null && doorLockedSound != null)
                audioSource.PlayOneShot(doorLockedSound);
            if (GameUI.Instance != null) GameUI.Instance.ShowMessage("열리지 않게 봉인되어 있다.");
            Debug.Log($"{name}: 봉인되어 열리지 않는다.");
            return;
        }

        if (isLocked)
        {
            if (playerInventory != null && playerInventory.HasKey(requiredKey))
            {
                isLocked = false;
                OpenDoor(playerInventory);
            }
            else
            {
                if (audioSource != null && doorLockedSound != null)
                    audioSource.PlayOneShot(doorLockedSound);
                if (GameUI.Instance != null) GameUI.Instance.ShowMessage("잠겨 있다. 열쇠가 필요하다.");
                Debug.Log($"{requiredKey} 열쇠가 필요하다.");
            }

            return;
        }

        if (isOpen)
            CloseDoor();
        else
            OpenDoor(playerInventory);
    }

    private void OpenDoor(PlayerInventory playerInventory)
    {
        // 플레이어가 문의 앞쪽에 있으면 앞으로, 뒤쪽에 있으면 뒤로 열림
        Transform playerTransform = playerInventory != null
            ? playerInventory.transform
            : Camera.main.transform;

        Vector3 toDoor = doorPivot.position - playerTransform.position;
        float dot = Vector3.Dot(doorPivot.forward, toDoor);
        float direction = dot > 0f ? 1f : -1f;

        openedRotation = closedRotation * Quaternion.Euler(0f, openAngle * direction, 0f);

        isOpen = true;
        Debug.Log("문 열기 실행");

        if (audioSource != null && doorOpenSound != null)
        {
            audioSource.PlayOneShot(doorOpenSound);
            Debug.Log("열기 사운드 재생");
        }
        else
        {
            Debug.LogWarning("열기 사운드 또는 AudioSource가 비어 있음");
        }
    }

    public void ForceOpen(Transform opener)
    {
        if (isOpen) return;

        isLocked = false;

        Vector3 toDoor = doorPivot.position - opener.position;
        float dot = Vector3.Dot(doorPivot.forward, toDoor);
        float direction = dot > 0f ? 1f : -1f;

        openedRotation = closedRotation * Quaternion.Euler(0f, openAngle * direction, 0f);

        isOpen = true;

        if (audioSource != null && doorOpenSound != null)
            audioSource.PlayOneShot(doorOpenSound);
    }

    public void ForceUnlock()
    {
        isLocked = false;
        Debug.Log($"{name} 잠금 해제됨");
    }

    /// <summary>
    /// 문을 닫고 게임 끝까지 영구 봉인. (시작 문 등 되돌아갈 수 없게)
    /// </summary>
    public void CloseAndSeal()
    {
        isOpen = false;     // Update가 closedRotation으로 슬러프 → 자동으로 닫힘
        isLocked = true;
        isSealed = true;

        if (audioSource != null && doorCloseSound != null)
            audioSource.PlayOneShot(doorCloseSound);

        // 닫히는 동안 회전하는 문 콜라이더가 플레이어를 밀어내지 않도록
        // 물리 콜라이더를 잠시 껐다가, 거의 다 닫히면 다시 켠다(봉인 유지).
        StartCoroutine(DisableBlockingWhileClosing());

        Debug.Log($"{name}: 닫고 봉인됨");
    }

    private System.Collections.IEnumerator DisableBlockingWhileClosing()
    {
        var cols = doorPivot.GetComponentsInChildren<Collider>();
        var toRestore = new System.Collections.Generic.List<Collider>();
        foreach (var c in cols)
        {
            // 물리(비트리거) 콜라이더만 끔 — 상호작용용 트리거 콜라이더는 보존
            if (c != null && !c.isTrigger && c.enabled)
            {
                c.enabled = false;
                toRestore.Add(c);
            }
        }

        // 거의 다 닫힐 때까지 대기 (안전장치로 최대 3초)
        float t = 0f;
        while (Quaternion.Angle(doorPivot.localRotation, closedRotation) > 1f && t < 3f)
        {
            t += Time.deltaTime;
            yield return null;
        }

        foreach (var c in toRestore)
            if (c != null) c.enabled = true;
    }

    private void CloseDoor()
    {
        isOpen = false;
        Debug.Log("문 닫기 실행");

        if (audioSource != null && doorCloseSound != null)
        {
            audioSource.PlayOneShot(doorCloseSound);
            Debug.Log("닫기 사운드 재생");
        }
        else
        {
            Debug.LogWarning("닫기 사운드 또는 AudioSource가 비어 있음");
        }
    }
}