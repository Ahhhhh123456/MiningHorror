using UnityEngine;
using UnityEngine.InputSystem;

public class LookAndClickInteraction : MonoBehaviour
{
    public Camera playerCamera;                // assign your FPS camera in Inspector
    public float interactRange = 1f;           // how far you can look and interact
    public InputActionReference clickAction;   // assign your "Click" action

    public InputActionReference eButtonAction;   // assign your "E button" action

    // Variables to track mining state and reset if let go
    private MineType mineType;
    private MineType currentMineTarget;

    [Header("Mining Settings")]
    public float mineInterval = 0.005f; // seconds between mining ticks
    private float mineTimer = 0f;

    [Header("Holding Settings")]

    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private Transform holdPosition;
    private InteractableItem currentInteractable;

    private Dropped dropScript;
    private bool isHoldingItem = false;

    public void Start()
    {
        mineType = FindObjectOfType<MineType>();
        dropScript = FindObjectOfType<Dropped>();
    }


    void OnEnable()
    {
        clickAction.action.Enable();
        eButtonAction.action.Enable();
    }

    void OnDisable()
    {
        clickAction.action.Disable();
        eButtonAction.action.Disable();
    }


    void Update()
    {
        if (eButtonAction.action.WasPressedThisFrame())
        {
            HandleInteraction();
        }

        Mining();
    }

    private void HandleInteraction()
    {
        if (isHoldingItem && currentInteractable != null)
        {
            currentInteractable.DropItem();
            currentInteractable.Highlight(false);
            currentInteractable = null;
            isHoldingItem = false;
            return;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance, interactableLayer))
        {
            InteractableItem interactable = hit.collider.GetComponent<InteractableItem>();
            if (interactable != null)
            {
                // Highlight switching
                if (currentInteractable != interactable)
                {
                    if (currentInteractable != null)
                        currentInteractable.Highlight(false);

                    currentInteractable = interactable;
                    currentInteractable.Highlight(true);
                }

                // Press E to pick up (auto-swap if holding another)
                if (isHoldingItem && currentInteractable != interactable)
                {
                    currentInteractable.DropItem(); // or SnapItem() if that's your snap logic
                    isHoldingItem = false;
                }

                interactable.PickUpItem(holdPosition);
                dropScript.PickedUp(hit.collider.gameObject);
                
                isHoldingItem = true;
                Debug.Log("Picked up " + interactable.gameObject.name);
                return; // stop here so Dropped doesn't also trigger
            }

            // Pick up dropped objects (only if not part of interactableLayer)
            if (dropScript != null && eButtonAction.action.WasPressedThisFrame())
            {
                dropScript.PickedUp(hit.collider.gameObject);
                Debug.Log("Picked up dropped " + dropScript.gameObject.name);
                return;
            }
        }
        else if (currentInteractable != null)
        {
            currentInteractable.Highlight(false);
            currentInteractable = null;
        }
    }



    private void Mining()
    {
        if (clickAction.action.WasReleasedThisFrame())
        {
            if (currentMineTarget != null)
            {
                currentMineTarget.holdCount = 0;
                Debug.Log("Stopped Mining Cube");
                currentMineTarget = null;
            }
            mineTimer = 0f; // reset timer
        }

        if (clickAction.action.IsPressed())
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactRange))
            {
                MineType mineTypeScript = hit.collider.GetComponent<MineType>();
                if (mineTypeScript != null)
                {
                    currentMineTarget = mineTypeScript;

                    // Update timer
                    mineTimer += Time.deltaTime;

                    // Call Mining only when enough time has passed
                    if (mineTimer >= mineInterval)
                    {
                        currentMineTarget.Mining();
                        mineTimer -= mineInterval; // reset timer but keep overflow
                    }
                }


            }
        }
    }
}
