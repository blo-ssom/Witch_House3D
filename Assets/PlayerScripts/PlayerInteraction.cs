using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public Camera playerCamera;
    public float interactDistance = 3f;
    public LayerMask interactLayer;

    private PlayerInventory playerInventory;
    private Interactable currentInteractable;

    private void Awake()
    {
        playerInventory = GetComponent<PlayerInventory>();
    }

    private void Update()
    {
        CheckInteractable();

        if (Input.GetKeyDown(KeyCode.E) && currentInteractable != null)
        {
            currentInteractable.Interact(playerInventory);
        }
    }

    private void CheckInteractable()
    {
        currentInteractable = null;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayer))
        {
            Interactable interactable = hit.collider.GetComponentInParent<Interactable>();

            if (interactable != null)
            {
                currentInteractable = interactable;
                GameUI.Instance.ShowInteractText(interactable.GetInteractPrompt());
                return;
            }
        }

        GameUI.Instance.HideInteractText();
    }
}