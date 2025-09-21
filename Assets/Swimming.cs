using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class Swimming : MonoBehaviour
{
    [Header("Swimming Settings")]
    public float swimUpSpeed = 2f;
    public float swimDownSpeed = 10f;
    public bool isSwimming = false;
    public float drowningTime = 5f;
    public Collider head; // Assign top of head in inspector

    public bool isDrowning = false;
    
    public Rigidbody rb;
    private PlayerMovement playerMovement; // reference to PlayerMovement
    public float drowningTimer = 0f;

    [Header("Input Actions")]
    public InputActionReference moveAction;
    public InputActionReference jumpAction;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void FixedUpdate()
    {
        if (!isSwimming) return;

        Swim();
        if (isDrowning)
        {
            HandleDrowning();
        }
    }

    private void Update()
    {
        if (!isSwimming) return;
    }

    public void Swim()
    {
        if (moveAction == null) return;

        // --- Horizontal input ---
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        Vector3 moveDir = transform.right * input.x + transform.forward * input.y;
        moveDir.y = 0f; // horizontal plane only

        // --- Check for blocking obstacles (like in PlayerMovement) ---
        if (moveDir.sqrMagnitude > 0.01f)
        {
            CapsuleCollider WallCheckerWater = GetComponent<CapsuleCollider>();
            Vector3 point1 = transform.position + WallCheckerWater.center + Vector3.up * (WallCheckerWater.height / 2 - WallCheckerWater.radius);
            Vector3 point2 = transform.position + WallCheckerWater.center - Vector3.up * (WallCheckerWater.height / 2 - WallCheckerWater.radius);
            float checkDistance = 0.1f;

            if (Physics.CapsuleCast(point1, point2, WallCheckerWater.radius, moveDir.normalized, out RaycastHit hit, checkDistance))
            {
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);

                // Cancel horizontal movement if hitting wall / steep slope
                if (slopeAngle > 45f)
                {
                    moveDir = Vector3.zero;
                }
            }
        }

        Vector3 horizontalVelocity = moveDir.normalized * (playerMovement.moveSpeed / 1.5f); // slower in water

        // --- Vertical movement ---
        if (jumpAction.action.IsPressed())
            playerMovement.verticalVelocity = swimUpSpeed;   // ascend
        else
            playerMovement.verticalVelocity = -swimDownSpeed; // descend

        // --- Apply velocity ---
        rb.linearVelocity = new Vector3(horizontalVelocity.x, playerMovement.verticalVelocity, horizontalVelocity.z);
    }


    public void HandleDrowning()
    {
        if (head == null)
        {
            Debug.LogWarning("Head transform not assigned in Swimming script.");
            return;
        }

        // Increment timer by physics delta time (seconds)
        drowningTimer += Time.fixedDeltaTime;
        Debug.Log("Drowning Timer: " + drowningTimer.ToString("F2"));

        if (drowningTimer >= drowningTime)
        {
            Drown();
        }
    }


    private void Drown()
    {
        Debug.Log("Player drowned!");

        // Example: if PlayerMovement has health, reduce it here
        // playerMovement.TakeDamage(999);

        // Or trigger respawn / death manager
    }
}
