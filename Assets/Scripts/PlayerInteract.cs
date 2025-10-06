using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private Transform holdPosition;
    
    private Camera playerCamera;
    private InteractableItem currentInteractable;
    private bool isHoldingItem = false;

    private void Start()
    {
        playerCamera = Camera.main;
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
                    
                    // Check for E key hold
                    if (Input.GetKey(KeyCode.E))
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
        else if (Input.GetKeyUp(KeyCode.E) && currentInteractable != null)
        {
            // This will be handled by the InteractableItem's Update
            isHoldingItem = false;
        }
    }
}