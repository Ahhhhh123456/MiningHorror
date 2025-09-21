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

    [Header("Movement Input Actions")]
    public InputActionReference sprintAction;
    public InputActionReference jumpAction;
    public InputActionReference moveAction;

    [Header("Ground Check Settings")]
    public LayerMask groundMask;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        inventory = GetComponent<PlayerInventory>();

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
        isGrounded = Physics.CheckSphere(feetPos, groundCheckRadius, groundMask);

        // Vertical velocity (gravity/jump)
        if (isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f; // stick to ground
        else
            verticalVelocity += Physics.gravity.y * Time.fixedDeltaTime;

        // Horizontal input
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        Vector3 moveDir = transform.right * input.x + transform.forward * input.y;
        moveDir.y = 0f;

        if (moveDir.sqrMagnitude > 0.01f)
        {
            CapsuleCollider WallChecker = GetComponent<CapsuleCollider>();

            Vector3 point1 = transform.position + WallChecker.center + Vector3.up * (WallChecker.height / 2 - WallChecker.radius);
            Vector3 point2 = transform.position + WallChecker.center - Vector3.up * (WallChecker.height / 2 - WallChecker.radius);

            float checkDistance = 0.1f;

            if (Physics.CapsuleCast(point1, point2, WallChecker.radius, moveDir.normalized, out RaycastHit hit, checkDistance))
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

        // Apply horizontal movement
        rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);

        // Apply vertical movement
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, verticalVelocity, rb.linearVelocity.z);
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
        Debug.Log("Sprinting speed: " + (moveSpeed * sprintMultiplier));
        moveSpeed *= sprintMultiplier;
    }

    private void StopSprinting(InputAction.CallbackContext context)
    {
        Debug.Log("Stopping sprinting speed: " + (moveSpeed / sprintMultiplier));
        // moveSpeed /= sprintMultiplier;
        UpdateMoveSpeed();
    }

}
