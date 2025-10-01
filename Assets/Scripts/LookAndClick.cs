using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
public class LookAndClickInteraction : NetworkBehaviour
{
    public Camera playerCamera;                // assign your FPS camera in Inspector
    public float interactRange = 1f;           // how far you can look and interact
    public InputActionReference clickAction;   // assign your "Click" action

    public InputActionReference eButtonAction;   // assign your "E button" action

    public InputActionReference slot1Action; // assign "1" key in Input Actions
    public InputActionReference slot2Action; // assign "2" key
    public InputActionReference slot3Action; // assign "3" key
    public InputActionReference slot4Action; // assign "4" key
    public InputActionReference dropAction; // assign "G" key in Input Actions

    // Variables to track mining state and reset if let go
    private MineType mineType;
    private MineType currentMineTarget;

    [Header("Mining Settings")]
    public float mineInterval = 0.005f; // seconds between mining ticks
    private float mineTimer = 0f;

    [Header("Holding Settings")]

    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactableLayer;
    
    private PlayerInventory playerInventory;

    private Dropped dropScript;
    private bool isHoldingItem = false;

    public void Start()
    {
        mineType = FindObjectOfType<MineType>();
        dropScript = FindObjectOfType<Dropped>();
        playerInventory = FindObjectOfType<PlayerInventory>();
    }


    void OnEnable()
    {
        clickAction.action.Enable();
        eButtonAction.action.Enable();
        slot1Action.action.Enable();
        slot2Action.action.Enable();
        slot3Action.action.Enable();
        slot4Action.action.Enable();
        dropAction.action.Enable();
    }

    void OnDisable()
    {
        clickAction.action.Disable();
        eButtonAction.action.Disable();
        slot1Action.action.Disable();
        slot2Action.action.Disable();
        slot3Action.action.Disable();
        slot4Action.action.Disable();
        dropAction.action.Disable();
    }


    void Update()
    {
        if (eButtonAction.action.WasPressedThisFrame())
        {
            HandleInteraction();
        }

        if (dropAction.action.WasPressedThisFrame())
        {
            DropCurrentItem();
        }

        ChooseInventorySlot();

        Mining();
    }


    private void ChooseInventorySlot()
    {  
        if (slot1Action.action.WasPressedThisFrame())
        {
            playerInventory.SelectSlot(0); // first item in list
        }
        if (slot2Action.action.WasPressedThisFrame())
        {
            playerInventory.SelectSlot(1); // second item in list
        }
        if (slot3Action.action.WasPressedThisFrame())
        {
            playerInventory.SelectSlot(2); // third item in list
        }
        if (slot4Action.action.WasPressedThisFrame())
        {
            playerInventory.SelectSlot(3); // fourth item in list
        }
    }
    private void HandleInteraction()
    {

        Debug.Log("E button pressed");

        if (isHoldingItem)
        {
            Debug.Log("Already holding an item, can't pick up another.");
            return; // exit early
        }
        
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance, interactableLayer))
        {
            // if (dropScript != null && eButtonAction.action.WasPressedThisFrame() && hit.collider.CompareTag("Dropped"))
            // {
            //     // Instead of picking up locally, call the networked method
            //     Debug.Log("Picking up item: " + hit.collider.gameObject.name);
            //     dropScript.PickedUp(hit.collider.gameObject);
            //     return;
            // }
            if (eButtonAction.action.WasPressedThisFrame() && hit.collider.CompareTag("Dropped"))
            {
                Dropped hitDrop = hit.collider.GetComponent<Dropped>();
                if (hitDrop != null)
                {
                    Debug.Log("Picking up item: " + hit.collider.gameObject.name);
                    hitDrop.PickedUp(hit.collider.gameObject);
                    return;
                }
            }
            else
            {
                Debug.Log("Hit object is not a dropped item or dropScript is null.");
            }
        }
    }


    public void DropCurrentItem()
    {
        if (playerInventory.currentHeldItem != null)
        {
            string itemName = playerInventory.currentHeldItem.name.Replace("(Clone)", ""); // clean up name

            // Remove from inventory
            playerInventory.RemoveFromInventory(itemName);

            // Detach from player
            playerInventory.currentHeldItem.transform.SetParent(null);

            // Enable physics so it drops
            Rigidbody rb = playerInventory.currentHeldItem.GetComponent<Rigidbody>();
            if (rb == null) rb = playerInventory.currentHeldItem.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = true;

            // Optional: give it a small forward push
            rb.AddForce(Camera.main.transform.forward * 2f, ForceMode.Impulse);

            // Clear reference
            playerInventory.currentHeldItem = null;

            Debug.Log($"Dropped {itemName}");
        }
        else
        {
            Debug.Log("No item to drop");
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
                        currentMineTarget.MiningOre();
                        mineTimer -= mineInterval; // reset timer but keep overflow
                    }
                }


            }
        }
    }
}
