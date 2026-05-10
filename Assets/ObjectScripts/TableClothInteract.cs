using System.Collections;
using UnityEngine;

/// <summary>
/// 천 덮인 테이블 상호작용. E키 입력 시:
///  1. 숨겨진 열쇠의 Rigidbody 활성화 → 자연스럽게 낙하
///  2. 띠리링 SFX 재생
///  3. pickupDelay 후 KeyItem 픽업 가능
///  4. 자기 자신 비활성 (1회성)
///
/// hiddenKey 셋업 (Unity 에디터):
///  - KeyItem 컴포넌트 + Collider + Rigidbody
///  - Rigidbody: Is Kinematic = ON (시작 시 안 떨어지게, 스크립트가 풀어줌)
///  - Rigidbody: Use Gravity = ON
///  - Rigidbody: Constraints → Freeze Rotation X / Z 권장 (옆으로 굴러가는 거 방지)
///  - Mass 0.05~0.1, Angular Drag 0.5~1 정도면 작은 키 느낌
/// </summary>
public class TableClothInteract : Interactable
{
    [Header("Hidden Key")]
    [Tooltip("KeyItem + Rigidbody가 부착된 열쇠 오브젝트. 시작 시 isKinematic=true 상태로 둘 것.")]
    public GameObject hiddenKey;

    [Header("Drop Settings")]
    [Tooltip("떨어지는 동안 픽업 차단 시간 (s). 이후 KeyItem 픽업 가능.")]
    public float pickupDelay = 0.7f;

    [Header("SFX")]
    public AudioSource audioSource;
    [Tooltip("띠리링 / 키 떨어지는 소리")]
    public AudioClip dropSound;

    private bool isTriggered = false;

    private void Start()
    {
        interactPrompt = "E : 테이블 천을 들추다";

        if (hiddenKey != null)
        {
            // 시작 시 KeyItem 비활성 — 천 들추기 전에는 픽업 불가
            var keyItem = hiddenKey.GetComponent<KeyItem>();
            if (keyItem != null) keyItem.enabled = false;

            // Rigidbody는 안전하게 kinematic으로 강제 (인스펙터 누락 방지)
            var rb = hiddenKey.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
        }
    }

    public override void Interact(PlayerInventory playerInventory)
    {
        if (isTriggered) return;
        isTriggered = true;

        if (audioSource != null && dropSound != null)
            audioSource.PlayOneShot(dropSound);

        if (hiddenKey != null)
            StartCoroutine(DropKey());

        enabled = false;
    }

    private IEnumerator DropKey()
    {
        var rb = hiddenKey.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity  = true;
        }

        // 떨어지는 동안 픽업 막기
        yield return new WaitForSeconds(pickupDelay);

        var keyItem = hiddenKey.GetComponent<KeyItem>();
        if (keyItem != null) keyItem.enabled = true;
    }
}
