using TMPro;
using UnityEngine;

public class NoteUI : MonoBehaviour
{
    public static NoteUI Instance;

    public GameObject notePanel;
    public TextMeshProUGUI noteText;

    private bool isOpen = false;

    private void Awake()
    {
        Instance = this;
        CloseNote();
    }

    private void Update()
    {
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseNote();
        }
    }

    public void OpenNote(string text)
    {
        notePanel.SetActive(true);
        noteText.text = text;
        isOpen = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseNote()
    {
        notePanel.SetActive(false);
        isOpen = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public bool IsOpen()
    {
        return isOpen;
    }
}