using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintMultiplier = 2f;
    public float jumpForce = 5f;
    public float moveSpeed;
    public Rigidbody rb;
    public float verticalVelocity = 0f;

    private Camera playerCamera;
    private AudioListener audioListener;
    private PlayerInventory inventory;
    private PlayerStamina stamina;

    [Header("Movement Input Actions")]
    public InputActionReference sprintAction;
    public InputActionReference jumpAction;
    public InputActionReference moveAction;

    [Header("Ground Check Settings")]
    public LayerMask groundMask;
    private bool isGrounded;
    private bool wasGrounded = false; // Track previous grounded state
    private bool isSprinting = false;

    [Header("Fall Damage Settings")]
    public float fallDamageThreshold = -10f; // Minimum downward velocity to start taking damage
    public float fallDamageMultiplier = 2f;  // Damage per unit of velocity beyond threshold
    private float previousVerticalVelocity = 0f; // Track previous frame's velocity


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        inventory = GetComponent<PlayerInventory>();
        stamina = GetComponent<PlayerStamina>();
        playerCamera = GetComponentInChildren<Camera>();
        audioListener = GetComponentInChildren<AudioListener>();

        if (!IsOwner)
        {
            if (playerCamera) playerCamera.enabled = false;
            if (audioListener) audioListener.enabled = false;
        }

        UpdateMoveSpeed();
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
            verticalVelocity = jumpForce;
        }

        if (isSprinting && rb.linearVelocity.magnitude > 0.1f) // only drain if moving
        {
            if (stamina.currentStamina <= 0f)
            {
                Debug.Log("Not enough stamina to sprint.");
                StopSprinting(new InputAction.CallbackContext());
            }
            stamina.UseStamina(100f * Time.deltaTime); // drain 10 per second
        }
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    public void HandleMovement()
    {
        CapsuleCollider groundChecker = GetComponent<CapsuleCollider>();
        float groundCheckRadius = groundChecker.radius * 0.9f;
        float groundCheckOffset = groundChecker.height / 2f - 0.05f;
        Vector3 feetPos = transform.position + Vector3.down * groundCheckOffset;

        // Ground check
        bool wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(feetPos, groundCheckRadius, groundMask);

        // Detect landing
        bool justLanded = !wasGrounded && isGrounded;

        // Apply fall damage if just landed
        if (justLanded && verticalVelocity < fallDamageThreshold)
        {
            FallDamage(verticalVelocity);
        }

        // Gravity / vertical velocity
        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f; // small downward force to stick to ground
        }
        else
        {
            verticalVelocity += Physics.gravity.y * Time.fixedDeltaTime;
        }

        // Horizontal input
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        Vector3 moveDir = transform.right * input.x + transform.forward * input.y;
        moveDir.y = 0f;

        if (moveDir.sqrMagnitude > 0.01f)
        {
            CapsuleCollider wallChecker = GetComponent<CapsuleCollider>();

            Vector3 point1 = transform.position + wallChecker.center + Vector3.up * (wallChecker.height / 2 - wallChecker.radius);
            Vector3 point2 = transform.position + wallChecker.center - Vector3.up * (wallChecker.height / 2 - wallChecker.radius);

            float checkDistance = 0.1f;

            if (Physics.CapsuleCast(point1, point2, wallChecker.radius, moveDir.normalized, out RaycastHit hit, checkDistance))
            {
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);

                // If hitting a wall or steep slope, cancel horizontal input
                if (slopeAngle > 45f)
                {
                    moveDir = Vector3.zero;
                }
            }
        }

        // Calculate horizontal velocity
        Vector3 horizontalVelocity = moveDir.normalized * moveSpeed;

        // Apply movement
        rb.linearVelocity = new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z);
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

    private void FallDamage(float impactVelocity)
    {
        float damage = Mathf.Abs(impactVelocity) * fallDamageMultiplier;
        Debug.Log("Fall damage taken: " + damage);

        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.TakeDamage(damage);
        }
    }
}
