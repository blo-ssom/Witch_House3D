using UnityEngine;
using System;

public class NoteItem : Interactable
{
    [TextArea]
    public string noteContent;

    [Tooltip("이 노트의 고유 ID (이벤트 트리거용, 빈 문자열이면 무시)")]
    public string noteID = "";

    public static event Action<string> OnNoteRead;

    public override string GetInteractPrompt()
    {
        return "E : 메모 읽기";
    }

    public override void Interact(PlayerInventory playerInventory)
    {
        if (NoteUI.Instance != null)
        {
            NoteUI.Instance.OpenNote(noteContent);
            OnNoteRead?.Invoke(noteID);
        }
    }
}