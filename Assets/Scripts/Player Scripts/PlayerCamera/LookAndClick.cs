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

    // Loading bar dropping and picking items
    private bool isDropping = false;  // To prevent multiple pickups at once
    private LoadingBar loadingBar;


    // Variables to track mining state and reset if let go
    private MineType mineType;
    private MineType currentMineTarget;

    [Header("Mining Settings")]
    public float mineInterval = 0.005f; // seconds between mining ticks
    private float mineTimer = 0f;

    public MarchingCubes caveGenerator; // drag your cave object here
    public float mineRadius = 1.5f;     // size of mining tool
    public float mineDepth = 1f;        // how much density to subtract per hit

    [Header("Holding Settings")]

    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactableLayer;
    
    private PlayerInventory playerInventory;

    private BoxBreak boxBreak;

    private Dropped dropScript;
    private bool isHoldingItem = false;

    public void Start()
    {
        mineType = FindObjectOfType<MineType>();
        dropScript = FindObjectOfType<Dropped>();
        playerInventory = FindObjectOfType<PlayerInventory>();
        loadingBar = GetComponent<LoadingBar>();
        boxBreak = FindObjectOfType<BoxBreak>();

    }


    void OnEnable()
    {
        if (!IsOwner) return; // 🔒 Only local player listens for input

        clickAction.action.Enable();
        eButtonAction.action.Enable();
        slot1Action.action.Enable();
        slot2Action.action.Enable();
        slot3Action.action.Enable();
        slot4Action.action.Enable();
        dropAction.action.Enable();

        dropAction.action.performed += StartDropping;
        dropAction.action.canceled += StopDropping;
    }

    void OnDisable()
    {
        if (!IsOwner) return; // 🔒 Only local player unbinds input

        clickAction.action.Disable();
        eButtonAction.action.Disable();
        slot1Action.action.Disable();
        slot2Action.action.Disable();
        slot3Action.action.Disable();
        slot4Action.action.Disable();
        dropAction.action.Disable();

        dropAction.action.performed -= StartDropping;
        dropAction.action.canceled -= StopDropping;
    }



    void Update()
    {
        if (!IsOwner) return; // 🧱 Only process input for the local player

        if (eButtonAction.action.WasPressedThisFrame())
        {
            HandleInteraction();
        }

        if (dropAction.action.WasPressedThisFrame())
        {
            DropCurrentItem();
        }

        if (isDropping)
        {
            if (loadingBar != null)
                loadingBar.IncreaseLoadServerRpc(30f * Time.deltaTime);
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

            if (eButtonAction.action.WasPressedThisFrame() && hit.collider.CompareTag("Box"))
            {
                BoxBreak boxBreak = hit.collider.GetComponent<BoxBreak>();
                if (boxBreak != null)
                {
                    Debug.Log("BoxBreak script found");
                    boxBreak.WhoBrokeBox();
                }
            }
        }
    }

    private void StartDropping(InputAction.CallbackContext context)
    {
        Debug.Log("Started dropping item");
        isDropping = true;
    }

    private void StopDropping(InputAction.CallbackContext context)
    {
        Debug.Log("Stopped dropping item");
        isDropping = false;
    }
    public void DropCurrentItem()
    {
        if (playerInventory.currentSlotIndex < 0) return;

        // Remove from networked inventory & spawn dropped object
        DropCurrentItemServerRpc(playerInventory.currentSlotIndex);

        // Clear held item locally immediately (optional, visual snappiness)
        if (playerInventory.currentHeldItem != null)
        {
            Destroy(playerInventory.currentHeldItem);
            playerInventory.currentHeldItem = null;
        }

        // Tell server to clear held item for all clients
        playerInventory.ClearHeldItemServerRpc();
    }


    [ServerRpc(RequireOwnership = false)]
    public void DropCurrentItemServerRpc(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= playerInventory.NetworkItems.Count) return;

        string itemName = playerInventory.NetworkItems[slotIndex].ToString();

        // Remove from networked inventory
        playerInventory.RemoveFromInventory(itemName);

        // Instantiate using PlayerInventory’s logic
        GameObject droppedItem = playerInventory.CreateItemInstance(itemName, playerInventory.holdPosition);
        if (droppedItem == null) return;

        droppedItem.transform.SetParent(null); // make sure it’s world-space

        // Add Rigidbody for physics
        Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
        if (rb == null) rb = droppedItem.AddComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;

        // Add small push
        rb.AddForce(playerInventory.holdPosition.forward * 2f, ForceMode.Impulse);

        // Spawn networked object
        if (droppedItem.TryGetComponent<NetworkObject>(out NetworkObject netObj))
            netObj.Spawn();
    }



    [ClientRpc]
    private void ClearHeldItemClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (playerInventory.currentHeldItem != null)
        {
            Destroy(playerInventory.currentHeldItem);
            playerInventory.currentHeldItem = null;
        }
    }

    private void Mining()
    {
        if (playerInventory.holdPickaxe == true)
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
                            // Passes camera and position of where the player mines
                            currentMineTarget.MiningOre(playerCamera.transform.forward, hit.normal);
                            mineTimer -= mineInterval; // reset timer but keep overflow
                        }
                    }
                    else
                    {
                        // Check if the hit object is the cave mesh
                        MeshysHelper helper = hit.collider.GetComponent<MeshysHelper>();
                        if (helper != null)
                        {
                            Debug.Log("Mining Cave at: " + hit.point);
                            helper.caveGenerator.MineCave(hit.point, mineRadius, mineDepth);
                        }
                        else
                        {
                            Debug.Log("No MineType or MeshysHelper found on hit object.");
                        }
                    }

                }
            }
        }
    }
}
