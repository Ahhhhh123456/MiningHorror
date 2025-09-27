using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteract : MonoBehaviour
{
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private Transform holdPosition;

    [Header("Input")]
    public InputActionReference interactAction; // assign "E" action in Inspector

    private Camera playerCamera;
    private InteractableItem currentInteractable;
    private bool isHoldingItem = false;

    private void Start()
    {
        playerCamera = Camera.main;
    }

    private void OnEnable()
    {
        interactAction.action.Enable();
    }

    private void OnDisable()
    {
        interactAction.action.Disable();
    }

    private void Update()
    {
        HandleInteraction();
    }

    private void HandleInteraction()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (!isHoldingItem)
        {
            // Check for interactable objects
            if (Physics.Raycast(ray, out hit, interactionDistance, interactableLayer))
            {
                InteractableItem interactable = hit.collider.GetComponent<InteractableItem>();
                if (interactable != null)
                {
                    if (currentInteractable != interactable)
                    {
                        if (currentInteractable != null)
                            currentInteractable.Highlight(false);

                        currentInteractable = interactable;
                        currentInteractable.Highlight(true);
                    }

                    // Check for E button press
                    if (interactAction.action.WasPressedThisFrame())
                    {
                        interactable.PickUp(holdPosition);
                        isHoldingItem = true;
                    }
                }
                else if (currentInteractable != null)
                {
                    currentInteractable.Highlight(false);
                    currentInteractable = null;
                }
            }
            else if (currentInteractable != null)
            {
                currentInteractable.Highlight(false);
                currentInteractable = null;
            }
        }
        else if (interactAction.action.WasReleasedThisFrame() && currentInteractable != null)
        {
            // Release handled in DropItem()
            isHoldingItem = false;
            currentInteractable.DropItem();
        }
    }
}
