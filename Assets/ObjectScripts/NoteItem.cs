using UnityEngine;

public class NoteItem : Interactable
{
    [TextArea]
    public string noteContent;

    public override string GetInteractPrompt()
    {
        return "E : 메모 읽기";
    }

    public override void Interact(PlayerInventory playerInventory)
    {
        if (NoteUI.Instance != null)
        {
            NoteUI.Instance.OpenNote(noteContent);
        }
    }
}