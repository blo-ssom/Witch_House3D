using UnityEngine;

public class DoorInteract : Interactable
{
    public AudioSource audioSource;
    public AudioClip doorOpenSound;
    public AudioClip doorCloseSound;
    public Transform doorPivot;
    public float openAngle = 90f;
    public float openSpeed = 2f;

    public bool isLocked = true;
    public KeyType requiredKey = KeyType.None;   // 추가

    private bool isOpen = false;
    private Quaternion closedRotation;
    private Quaternion openedRotation;

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
        if (isLocked)
        {
            if (playerInventory != null && playerInventory.HasKey(requiredKey))
            {
                isLocked = false;
                OpenDoor(playerInventory);
            }
            else
            {
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