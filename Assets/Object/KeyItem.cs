using UnityEngine;

public class KeyItem : Interactable
{
    public override string GetInteractPrompt()
    {
        return "E : 열쇠 줍기";
    }

    public override void Interact(PlayerInventory playerInventory)
    {
        if (playerInventory == null) return;

        playerInventory.GetKey();
        Destroy(gameObject);
    }
}