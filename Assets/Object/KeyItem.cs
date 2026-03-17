using UnityEngine;

public class KeyItem : Interactable
{
    public KeyType keyType;

    public override void Interact(PlayerInventory playerInventory)
    {
        if (playerInventory == null) return;

        playerInventory.AddKey(keyType);
        Destroy(gameObject);
    }
}