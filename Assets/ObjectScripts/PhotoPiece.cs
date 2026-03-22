using UnityEngine;

/// <summary>
/// 사진 조각 오브젝트에 붙이는 컴포넌트.
/// 줍는 순간 PhotoPuzzleManager에 수집 알림.
///
/// 사용법:
///  1. 사진 조각 큐브에 부착
///  2. pieceID → 0, 1, 2 각각 다르게 설정
///  3. Layer → Interact 설정
/// </summary>
public class PhotoPiece : Interactable
{
    [Header("조각 설정")]
    [Tooltip("0, 1, 2 각각 다르게 설정")]
    public int pieceID = 0;

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip pickupSound;

    private bool collected = false;

    private void Start()
    {
        interactPrompt = "E : 사진 조각을 줍다";
    }

    public override void Interact(PlayerInventory playerInventory)
    {
        if (collected) return;
        collected = true;

        if (audioSource != null && pickupSound != null)
            audioSource.PlayOneShot(pickupSound);

        // 퍼즐 매니저에 수집 알림
        PhotoPuzzleManager.Instance.CollectPiece(pieceID);

        gameObject.SetActive(false);
        Debug.Log($"[PhotoPiece] 조각 {pieceID} 수집");
    }
}
