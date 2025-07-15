using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;

    public string actionMapName;
    public string moveActionName = "Move";

    private Rigidbody rb;
    private Vector2 moveInput;
    private PlayerControls controls;
    private InputAction moveAction;

    private void Awake()
    {
        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        controls.asset.FindActionMap(actionMapName)?.Enable();
        moveAction = controls.asset.FindActionMap(actionMapName)?.FindAction(moveActionName);

        if (moveAction != null)
        {
            moveAction.performed += OnMove;
            moveAction.canceled += OnCancel;
        }
    }

    private void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.performed -= OnMove;
            moveAction.canceled -= OnCancel;
        }

        controls.asset.FindActionMap(actionMapName)?.Disable();
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        Vector3 move = new Vector3(moveInput.x, 0f, moveInput.y);
        rb.MovePosition(rb.position + move * speed * Time.fixedDeltaTime);
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    private void OnCancel(InputAction.CallbackContext ctx)
    {
        moveInput = Vector2.zero;
    }
}
