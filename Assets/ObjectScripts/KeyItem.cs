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

        Destroy(gameObject);
    }
}