using TMPro;
using UnityEngine;

public class GameUI : MonoBehaviour
{
    public static GameUI Instance;

    public TextMeshProUGUI interactText;

    private void Awake()
    {
        Instance = this;
        HideInteractText();
    }

    public void ShowInteractText(string message)
    {
        if (interactText == null) return;

        interactText.text = message;
        interactText.gameObject.SetActive(true);
    }

    public void HideInteractText()
    {
        if (interactText == null) return;

        interactText.text = "";
        interactText.gameObject.SetActive(false);
    }
}