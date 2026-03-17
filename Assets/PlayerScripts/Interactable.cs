using UnityEngine;

public class Interactable : MonoBehaviour
{
    [TextArea]
    public string interactPrompt = "E : 상호작용";
    public virtual string GetInteractPrompt()
    {
        return interactPrompt;
    }
    public virtual void Interact(PlayerInventory playerInventory)
    {
        Debug.Log(gameObject.name + " 상호작용");
    }
}