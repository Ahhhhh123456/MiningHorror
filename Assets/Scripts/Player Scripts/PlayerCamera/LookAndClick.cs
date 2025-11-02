using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
public class LookAndClickInteraction : NetworkBehaviour
{
    public Camera playerCamera;                // assign your FPS camera in Inspector
    public float interactRange;           // how far you can look and interact
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

    // public MarchingCubes caveGenerator; // drag your cave object here
    public float mineRadius = 100f;     // size of mining tool
    public float mineDepth = 200f;        // how much density to subtract per hit

    [Header("Holding Settings")]

    [SerializeField] private float interactionDistance;
    [SerializeField] private LayerMask interactableLayer;
    
    private PlayerInventory playerInventory;

    private BoxBreak boxBreak;

    private Dropped dropScript;

    private Explode explodeScript;
    //private TrackBoxes trackBoxes;
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

        CompassUpdate();
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

        if (itemName == "Dynamite")
        {
            Explode explodeScript = droppedItem.GetComponent<Explode>();
            if (explodeScript != null)
            {
                Debug.Log("Found Explode script on dropped item.");

                // 🔥 Call explosion via RPC, not local function
                explodeScript.ExplosionServerRpc();
            }
            else
            {
                Debug.LogWarning("Dropped Dynamite has no Explode script!");
            }
        }
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

    private void CompassUpdate()
    {
        TrackBoxes compassTracker = GetComponent<TrackBoxes>();
        if (playerInventory.IsHoldingCompass)
        {
            if (compassTracker == null)
            {
                Debug.Log("compassTracker is null");
            }
        }

    }

    private void Mining()
    {
        if (!playerInventory.holdPickaxe) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, interactRange);

        // Stop mining when releasing click
        if (clickAction.action.WasReleasedThisFrame())
        {
            if (currentMineTarget != null)
            {
                currentMineTarget.holdCount = 0;
                Debug.Log("Stopped Mining Cube");
                currentMineTarget = null;
            }
            mineTimer = 0f;
        }

        if (!clickAction.action.IsPressed()) return;

        if (hits.Length == 0) return;

        // STEP 1 — PICK PRIORITIZED TARGET
        RaycastHit? caveHit = null;
        RaycastHit? oreHit = null;

        foreach (var h in hits)
        {
            GameObject obj = h.collider.gameObject;

            if (obj.CompareTag("Cave"))
            {
                caveHit = h;
                break; // Cave wins outright
            }
            else if (obj.CompareTag("Dropped"))
            {
                // Ignore dropped items entirely
                continue;
            }
            else if (h.collider.TryGetComponent(out MineType mt))
            {
                // Catch possible ore
                // only choose the *closest* ore if multiple
                if (oreHit == null || h.distance < oreHit.Value.distance)
                    oreHit = h;
            }
        }

        // STEP 2 — PERFORM ACTIONS BASED ON PRIORITY
        if (caveHit.HasValue)
        {
            var helper = caveHit.Value.collider.GetComponent<MeshysHelper>();
            if (helper != null)
            {
                mineTimer += Time.deltaTime;
                if (mineTimer >= mineInterval)
                {
                    helper.caveGenerator.MineCaveServerRpc(
                        caveHit.Value.point, mineRadius, mineDepth, false);
                    mineTimer -= mineInterval;
                }
            }
            return;
        }

        if (oreHit.HasValue)
        {
            var hit = oreHit.Value;
            var mineTypeScript = hit.collider.GetComponent<MineType>();

            currentMineTarget = mineTypeScript;
            mineTimer += Time.deltaTime;

            if (mineTimer >= mineInterval)
            {
                currentMineTarget.MiningOre(playerCamera.transform.forward, hit.normal);
                mineTimer -= mineInterval;
            }
            return;
        }

        // If we get here, nothing valid to mine
    }


}
