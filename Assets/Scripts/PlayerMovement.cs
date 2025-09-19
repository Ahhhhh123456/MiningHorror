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
    private float moveSpeed;
    public Rigidbody rb;
    private float verticalVelocity = 0f;

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

    [Header("Swimming Settings")]
    public float swimUpSpeed = 2f;     // ascend speed when pressing jump
    public float swimDownSpeed = 10f;   // slow sinking speed
    public bool isSwimming = false;

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

        if (isSwimming)
        {
            Swimming();
        }
        else
        {
            HandleMovement(); // normal walking/jumping
        }
    }

    public void HandleMovement()
    {
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        float groundCheckRadius = capsule.radius * 0.9f;
        float groundCheckOffset = capsule.height / 2f - 0.05f;
        Vector3 feetPos = transform.position + Vector3.down * groundCheckOffset;
        isGrounded = Physics.CheckSphere(feetPos, groundCheckRadius, groundMask);

        // Vertical velocity
        if (isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f; // stick to ground
        else
            verticalVelocity += Physics.gravity.y * Time.fixedDeltaTime;

        // Horizontal input
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        Vector3 moveDir = transform.right * input.x + transform.forward * input.y;
        moveDir.y = 0f;
        Vector3 horizontalVelocity = moveDir.normalized * moveSpeed;

        // Apply horizontal velocity while letting collisions slide naturally
        rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);

        // Apply vertical velocity (jump/gravity)
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

    public void Swimming()
    {
        // Horizontal movement
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        Vector3 moveDir = transform.right * input.x + transform.forward * input.y;
        moveDir.y = 0f;
        Vector3 horizontalVelocity = moveDir.normalized * (moveSpeed/ 1.5f); // slower in water

        // Vertical movement
        if (jumpAction.action.IsPressed())
            verticalVelocity = swimUpSpeed;   // ascend
        else
            verticalVelocity = -swimDownSpeed; // descend slowly

        // Apply final velocity without overwriting physics
        rb.linearVelocity = new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z);
    }
}
