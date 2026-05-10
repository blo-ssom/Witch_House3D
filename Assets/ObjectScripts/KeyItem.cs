using UnityEngine;

public class KeyItem : Interactable
{
    public KeyType keyType;

    public override void Interact(PlayerInventory playerInventory)
    {
        
        if (playerInventory == null) return;

        playerInventory.AddKey(keyType);
    // 추가: 방3 열쇠 획득 시 샹들리에 이벤트 발동
        if (keyType == KeyType.Room3)
        {
            var chandelier = FindObjectOfType<ChandelierEvent>();
            if (chandelier != null) chandelier.TriggerEvent();
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