using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;

    // Keep a public moveSpeed for compatibility with existing scripts
    public float moveSpeed = 5f;

    public float jumpForce = 5f;
    public float gravity = -9.81f;
    public float groundCheckDistance = 0.4f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    private Camera playerCamera;
    private AudioListener audioListener;

    private PlayerInventory inventory;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        inventory = GetComponent<PlayerInventory>();

        playerCamera = GetComponentInChildren<Camera>();
        audioListener = GetComponentInChildren<AudioListener>();

        // Disable camera/audio for non-local players (avoids multiple AudioListeners)
        if (!IsOwner)
        {
            if (playerCamera) playerCamera.enabled = false;
            if (audioListener) audioListener.enabled = false;
        }

        // initialize moveSpeed and sync with inventory if present
        if (moveSpeed <= 0f) moveSpeed = walkSpeed;
        updateMoveSpeed();
    }

    // Backwards-compatible method your inventory calls
    public void updateMoveSpeed()
    {
        if (inventory == null)
        {
            moveSpeed = walkSpeed;
            return;
        }

        // preserve the weight-based behaviour you had
        if (inventory.playerWeight > 10f)
            moveSpeed = 1f;
        else if (inventory.playerWeight > 5f)
            moveSpeed = 3f;
        else
            moveSpeed = walkSpeed;
    }

    void Update()
    {
        if (!IsOwner) return; // only local player processes input

        HandleMovement();
    }

    private void HandleMovement()
    {
        // Ground check
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance);

        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f; // stable grounding

        // Jump
        if (Input.GetKeyDown(KeyCode.Space))
            velocity.y = jumpForce;

        // Gravity
        velocity.y += gravity * Time.deltaTime;

        // Movement input
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        controller.Move(move * moveSpeed * Time.deltaTime + velocity * Time.deltaTime);
    }
}
