using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float gravity = -9.81f;
    public float jumpForce = 5f;
    public float groundCheck = 0.4f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        // Check ground
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheck);

        // Jump
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            velocity.y = jumpForce;
        }

        // Gravity
        if (!isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }

        // Movement
        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");
        
        float currentSpeed = moveSpeed;
        Vector3 move = transform.right * moveX + transform.forward * moveY; // Movement direction relative to camera

        // Move character
        controller.Move(move * currentSpeed * Time.deltaTime + velocity * Time.deltaTime);
    }

}
