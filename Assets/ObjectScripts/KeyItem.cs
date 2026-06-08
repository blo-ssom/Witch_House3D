using UnityEngine;
using UnityEngine.Events;

public class KeyItem : Interactable
{
    public KeyType keyType;

    [Header("Pickup SFX")]
    [Tooltip("열쇠를 주울 때 재생되는 사운드. 비워두면 무음.")]
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float pickupVolume = 1f;

    [Header("획득 이벤트 (선택)")]
    [Tooltip("열쇠 획득 시 호출. 예: 지하 촛불 퍼즐 CandlePuzzleManager.OnKeyCollected")]
    public UnityEvent onPickup;

    public override void Interact(PlayerInventory playerInventory)
    {
        if (playerInventory == null) return;

        playerInventory.AddKey(keyType);

        // 픽업 사운드 (Destroy 직후에도 재생되도록 PlayClipAtPoint 사용 — 임시 AudioSource 자동 생성)
        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupVolume);

        // 획득 이벤트 (Destroy 전에 발행 — 구독자는 자신의 GameObject에서 코루틴 처리)
        onPickup?.Invoke();

    // 추가: 방3 열쇠 획득 시 샹들리에 이벤트 발동 + 방1 액자 낙하 트리거
        if (keyType == KeyType.Room3)
        {
            var chandelier = FindObjectOfType<ChandelierEvent>();
            if (chandelier != null) chandelier.TriggerEvent();

            var painting = FindObjectOfType<PaintingFallEvent>();
            if (painting != null) painting.Trigger();

            var paintingRB = FindObjectOfType<PaintingFallEventRB>();
            if (paintingRB != null) paintingRB.Trigger();
        }

    // 추가: 방2 열쇠 획득 시 4개 석상이 추적 시작
        if (keyType == KeyType.Room2)
        {
            var statueManager = FindObjectOfType<Room3StatueManager>();
            if (statueManager != null) statueManager.ActivateAll();
        }

        Destroy(gameObject);
    }
}
