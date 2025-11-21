using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine.InputSystem;
using UnityEngine.UIElements.Experimental;
using NUnit.Framework.Internal.Filters;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintMultiplier = 2f;
    public float jumpForce;
    public float moveSpeed;
    public Rigidbody rb;
    public float verticalVelocity = 0f;

    private Camera playerCamera;
    private AudioListener audioListener;
    private PlayerInventory inventory;
    private PlayerStamina stamina;

    private LoadingBar loadingBar;

    [Header("Movement Input Actions")]
    public InputActionReference sprintAction;
    public InputActionReference jumpAction;
    public InputActionReference moveAction;

    [Header("Ground Check Settings")]
    public LayerMask groundMask;
    private bool isGrounded;
    private bool wasGrounded = false; // Track previous grounded state
    private bool isSprinting = false;

    [Header("Ragdoll Settings")]
    // [SerializeField] private ConfigurableJoint mainJoint;
    SyncPhysicsObject[] syncPhysicsObjects;
    [SerializeField] private Rigidbody[] ragdollRigidbodies;
    [SerializeField] private Collider[] ragdollColliders;

    public NetworkVariable<bool> isRagdollActive = new NetworkVariable<bool>(false);

    // [Header("Fall Damage Settings")]
    // public float fallDamageThreshold = -10f; // Minimum downward velocity to start taking damage
    // public float fallDamageMultiplier = 2f;  // Damage per unit of velocity beyond threshold
    // private float previousVerticalVelocity = 0f; // Track previous frame's velocity

    [Header("Ladder Climbing Settings")]
    public float ladderDetectionRadius; // How close the player needs to be to grab the ladder
    public float ladderClimbSpeed;     // Speed at which the player climbs

    private bool isClimbing = false;
    private Transform currentLadder;



    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        inventory = GetComponent<PlayerInventory>();
        stamina = GetComponent<PlayerStamina>();
        loadingBar = GetComponent<LoadingBar>();
        playerCamera = GetComponentInChildren<Camera>();
        audioListener = GetComponentInChildren<AudioListener>();

        if (!IsOwner)
        {
            if (playerCamera) playerCamera.enabled = false;
            if (audioListener) audioListener.enabled = false;
        }

        isRagdollActive.OnValueChanged += (oldVal, newVal) =>
        {
            ApplyRagdoll(newVal);
        };  

        UpdateMoveSpeed();


    }
    

    void Awake()
    {
        syncPhysicsObjects = GetComponentsInChildren<SyncPhysicsObject>();
    }

    void OnEnable()
    {
        sprintAction.action.Enable();
        sprintAction.action.performed += StartSprinting;
        sprintAction.action.canceled += StopSprinting;

        moveAction.action.Enable();
        jumpAction.action.Enable();
    }

    void OnDisable()
    {
        sprintAction.action.Disable();
        sprintAction.action.performed -= StartSprinting;
        sprintAction.action.canceled -= StopSprinting;

        moveAction.action.Disable();
        jumpAction.action.Disable();
    }

    void Update()
    {
        if (!IsOwner) return;


        // Jump input
        if (jumpAction.action.triggered && isGrounded)
        {
            float jumpVelocity = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * jumpForce);
            rb.AddForce(Vector3.up * jumpVelocity * rb.mass, ForceMode.Impulse);
        }

        if (isSprinting && rb.linearVelocity.magnitude > 0.1f) // only drain if moving
        {
            if (stamina.currentStamina.Value <= 0f)
            {
                Debug.Log("Not enough stamina to sprint.");
                StopSprinting(new InputAction.CallbackContext());
            }
            // 🔹 Test loading bar fill while sprinting
            if (loadingBar != null)
            {
                loadingBar.IncreaseLoadServerRpc(50f * Time.deltaTime); 
                // adjust 50f for speed of fill
            }
            stamina.UseStaminaServerRpc(25f * Time.deltaTime); // drain 10 per second
        }


    }

    // private void SetRagdollActive(bool active)
    // {
    //     foreach (var limbRb in ragdollRigidbodies)
    //         limbRb.isKinematic = !active; // ragdoll physics ON → non-kinematic

    //     foreach (var col in ragdollColliders)
    //         col.enabled = active; // optional: disable colliders when ragdoll off

    //     rb.isKinematic = active; // main Rigidbody should be kinematic when ragdoll active
    // }
    [ServerRpc(RequireOwnership = false)]
    public void SetRagdollServerRpc(bool active, ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        isRagdollActive.Value = active;

        Debug.Log($"Client {senderClientId} requested ragdoll state: {active}");

        ApplyRagdoll(active);
    }


    private void ApplyRagdoll(bool active)
    {
        foreach (var limbRb in ragdollRigidbodies)
            limbRb.isKinematic = !active;

        foreach (var col in ragdollColliders)
            col.enabled = active;

        rb.isKinematic = false; // keep root non-kinematic so it can move

        Debug.Log("Ragdoll state changed: " + active);
    }

    void FixedUpdate()
    {
        HandleMovement();

        if (!IsOwner) return;

        // Optional: Update ragdoll limbs after everything
        for (int i = 0; i < syncPhysicsObjects.Length; i++)
        {
            syncPhysicsObjects[i].UpdateJointFromAnimation();
        }
    }

    private bool IsRagdollActive()
    {
        foreach (var limbRb in ragdollRigidbodies)
        {
            if (!limbRb.isKinematic) return true;
        }
        return false;
    }

    public void HandleMovement()
    {
        LadderClimb();

        CapsuleCollider groundChecker = GetComponent<CapsuleCollider>();
        float groundCheckRadius = groundChecker.radius * 0.9f;
        float groundCheckOffset = groundChecker.height / 2f - 0.05f;
        Vector3 feetPos = transform.position + Vector3.down * groundCheckOffset;

        // Ground check
        bool wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(feetPos, groundCheckRadius, groundMask & ~LayerMask.GetMask("Ragdoll"));

        // Detect landing
        bool justLanded = !wasGrounded && isGrounded;

        // Apply fall damage if just landed
        // if (justLanded && verticalVelocity < fallDamageThreshold)
        // {
        //     FallDamage(verticalVelocity);
        // }

        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -0.1f; // keep the player grounded without drifting
        }
        else
        {
            verticalVelocity += Physics.gravity.y * Time.fixedDeltaTime;
        }

        // Horizontal input
        Vector2 input = moveAction.action.ReadValue<Vector2>();

        // Deadzone check
        if (input.sqrMagnitude < 0.01f) input = Vector2.zero;

        Vector3 moveDir = transform.right * input.x + transform.forward * input.y;
        moveDir.y = 0f;

        // ---- SLOPE HANDLING ----
        if (isGrounded && moveDir.sqrMagnitude > 0.01f)
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit groundHit, 2f, groundMask))
            {
                Vector3 groundNormal = groundHit.normal;
                moveDir = Vector3.ProjectOnPlane(moveDir, groundNormal).normalized;
            }
        }

        // ---- Only apply horizontal velocity if there is input ----
        Vector3 horizontalVelocity = Vector3.zero;
        if (moveDir.sqrMagnitude > 0.01f)
        {
            horizontalVelocity = moveDir.normalized * moveSpeed;
        }

        // Apply movement
        Vector3 velocity = rb.linearVelocity;
        velocity.x = horizontalVelocity.x;
        velocity.z = horizontalVelocity.z;
        rb.linearVelocity = velocity;
    }



    public void UpdateMoveSpeed()
    {
        if (inventory == null)
        {
            moveSpeed = walkSpeed;
            return;
        }

        if (inventory.playerWeight > 10f)
            moveSpeed = 3f;
        else if (inventory.playerWeight > 5f)
            moveSpeed = 4.5f;
        else
            moveSpeed = walkSpeed;
    }

    private void StartSprinting(InputAction.CallbackContext context)
    {
        //Debug.Log("Sprinting speed: " + (moveSpeed * sprintMultiplier));
        moveSpeed *= sprintMultiplier;
        isSprinting = true;

    }

    private void StopSprinting(InputAction.CallbackContext context)
    {
        Debug.Log("Stopping sprinting speed: " + (moveSpeed / sprintMultiplier));
        // moveSpeed /= sprintMultiplier;
        isSprinting = false;
        UpdateMoveSpeed();
    }



    // private void FallDamage(float impactVelocity)
    // {
    //     float damage = Mathf.Abs(impactVelocity) * fallDamageMultiplier;
    //     Debug.Log("Fall damage taken: " + damage);

    //     PlayerHealth health = GetComponent<PlayerHealth>();
    //     if (health != null)
    //     {
    //         health.TakeDamageServerRpc(damage);
    //     }
    // }

    private void LadderClimb()
    {
        // Detect nearby ladders
        isClimbing = false;
        currentLadder = null;
        Collider[] hits = Physics.OverlapSphere(transform.position, ladderDetectionRadius);
        foreach (var hit in hits)
        {
            if (hit.name.StartsWith("Ladder"))
            {
                isClimbing = true;
                currentLadder = hit.transform;
                break;
            }
        }

        if (!isClimbing || currentLadder == null) return;

        // Disable jumping/gravity while climbing
        verticalVelocity = 0f;

        // Get vertical input (W/S or up/down)
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        float climbY = input.y * ladderClimbSpeed * Time.fixedDeltaTime;

        // Move the player smoothly along the ladder
        Vector3 climbPosition = transform.position + new Vector3(0f, climbY, 0f);
        rb.MovePosition(climbPosition);

        // Optional: zero out Y velocity in Rigidbody to prevent conflicts
        Vector3 v = rb.linearVelocity;
        v.y = 0f;
        rb.linearVelocity = v;
    }


    
}
