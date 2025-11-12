// using System.Collections;
// using System.Collections.Generic;
// using NUnit.Framework;
// using UnityEngine;

// public class NetworkPlayer : MonoBehaviour
// {
//     [SerializeField] Rigidbody rigidbody3D;
//     [SerializeField] ConfigurableJoint mainJoint;
//     [SerializeField] Animator animator;

//     // Input
//     Vector2 moveInputVector = Vector2.zero;
//     bool isJumpButtonPressed = false;

//     // Controller Settings
//     float maxSpeed = 3;

//     // States
//     bool isGrounded = false;

//     // Raycasts
//     RaycastHit[] raycastHits = new RaycastHit[10];

//     // Syncing of physics objects
//     SyncPhysicsObject[] syncPhysicsObjects;

//     void Awake()
//     {
//         syncPhysicsObjects = GetComponentsInChildren<SyncPhysicsObject>();
//     }

//     // Start is called once before the first execution of Update after the MonoBehaviour is created
//     void Start()
//     {

//     }

//     // Update is called once per frame
//     void Update()
//     {
//         // Move input
//         moveInputVector.x = Input.GetAxis("Horizontal");
//         moveInputVector.y = Input.GetAxis("Vertical");

//         if (Input.GetKeyDown(KeyCode.Space))
//             isJumpButtonPressed = true;
//     }

//     void FixedUpdate()
//     {
//         // Assume that we are not grounded
//         isGrounded = false;

//         // Check if grounded
//         int numberofHits = Physics.SphereCastNonAlloc(rigidbody3D.position, 0.1f, transform.up * -1, raycastHits, 0.5f);

//         // Check for all valid results
//         for (int i = 0; i < numberofHits; i++)
//         {
//             // Ignore self hits
//             if (raycastHits[i].transform.root == transform)
//                 continue;

//             isGrounded = true;

//             break;
//         }

//         // Aply extra gravity to character to make less floaty
//         if (!isGrounded)
//             rigidbody3D.AddForce(Vector3.down * 10);

//         float inputMagnitude = moveInputVector.magnitude;

//         Vector3 localVelocityVsForward = transform.forward * Vector3.Dot(transform.forward, rigidbody3D.linearVelocity);

//         float localForwardVelocity = localVelocityVsForward.magnitude;

//         if (inputMagnitude != 0)
//         {
//             Quaternion desiredDirection = Quaternion.LookRotation(new Vector3(moveInputVector.x, 0, moveInputVector.y * -1), transform.up);

//             // Rotate target towards direction
//             mainJoint.targetRotation = Quaternion.RotateTowards(mainJoint.targetRotation, desiredDirection, Time.fixedDeltaTime * 300);

//             if (localForwardVelocity < maxSpeed)
//             {
//                 // Move the character in the direction it is facing
//                 rigidbody3D.AddForce(transform.forward * inputMagnitude * 30);
//             }
//         }

//         if (isGrounded && isJumpButtonPressed)
//         {
//             rigidbody3D.AddForce(Vector3.up * 20, ForceMode.Impulse);

//             isJumpButtonPressed = false;
//         }

//         animator.SetFloat("movementSpeed", localForwardVelocity * 1f);

//         // Updates the joints rotation based on the animations
//         for (int i = 0; i < syncPhysicsObjects.Length; i++)
//         {
//             syncPhysicsObjects[i].UpdateJointFromAnimation();
//         }
//     }
// }

using UnityEngine;

public class NetworkPlayer : MonoBehaviour
{
    [SerializeField] Rigidbody rigidbody3D;
    [SerializeField] ConfigurableJoint mainJoint;
    [SerializeField] Animator animator;

    [Header("Camera")]
    [SerializeField] Transform cameraPivot; // Camera pitch pivot
    [SerializeField] Vector3 cameraOffset = new Vector3(0, 1.6f, 0);
    [SerializeField] float mouseSensitivity = 150f;

    // Input
    Vector2 moveInputVector = Vector2.zero;
    bool isJumpButtonPressed = false;

    // Movement settings
    float maxSpeed = 3f;

    // States
    bool isGrounded = false;

    // Raycast Hits
    RaycastHit[] raycastHits = new RaycastHit[10];

    // Syncing physics objects
    SyncPhysicsObject[] syncPhysicsObjects;

    float pitch = 0f;

    void Awake()
    {
        syncPhysicsObjects = GetComponentsInChildren<SyncPhysicsObject>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // --- Mouse look ---
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Rotate player body (yaw)
        transform.Rotate(Vector3.up, mouseX);

        // Rotate camera pivot (pitch)
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -50f, 50f);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        // --- Movement input ---
        moveInputVector.x = Input.GetAxis("Horizontal");
        moveInputVector.y = Input.GetAxis("Vertical");

        if (Input.GetKeyDown(KeyCode.Space))
            isJumpButtonPressed = true;
    }

    void FixedUpdate()
    {
        // --- Ground check ---
        isGrounded = false;
        int numberofHits = Physics.SphereCastNonAlloc(rigidbody3D.position, 0.4f, Vector3.down, raycastHits, 0.1f);
        
        Debug.DrawRay(rigidbody3D.position, Vector3.up * 0.4f, Color.green); // Top of sphere
        Debug.DrawRay(rigidbody3D.position, Vector3.down * 0.4f, Color.green); // Bottom of sphere

        // Check for all valid results
        for (int i = 0; i < numberofHits; i++)
        {
            // Ignore self hits
            if (raycastHits[i].transform.root == transform)
                continue;

            isGrounded = true;

            break;
        }

        if (!isGrounded)
            rigidbody3D.AddForce(Vector3.down * 10f);

        // --- Movement direction based on camera forward/right ---
        Vector3 camForward = cameraPivot.forward;
        camForward.y = 0;
        camForward.Normalize();

        Vector3 camRight = cameraPivot.right;
        camRight.y = 0;
        camRight.Normalize();

        Vector3 desiredMoveDirection = camForward * moveInputVector.y + camRight * moveInputVector.x;
        float inputMagnitude = desiredMoveDirection.magnitude;
        float currentForwardSpeed = Vector3.Dot(rigidbody3D.linearVelocity, desiredMoveDirection);

        // Rotate rig toward camera yaw (so animation bones stay oriented)
        Quaternion desiredYawRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        mainJoint.targetRotation = Quaternion.RotateTowards(
            mainJoint.targetRotation,
            Quaternion.Inverse(desiredYawRotation),
            Time.fixedDeltaTime * 300f
        );

        // Apply movement force
        if (inputMagnitude > 0.01f && currentForwardSpeed < maxSpeed)
        {
            rigidbody3D.AddForce(desiredMoveDirection.normalized * 30f);
        }

        // Jump
        if (isGrounded && isJumpButtonPressed)
        {
            rigidbody3D.AddForce(Vector3.up * 10f, ForceMode.Impulse);
            isJumpButtonPressed = false;
        }

        // Animator
        animator.SetFloat("movementSpeed", rigidbody3D.linearVelocity.magnitude);

        // Sync physics-based bones with animation
        for (int i = 0; i < syncPhysicsObjects.Length; i++)
            syncPhysicsObjects[i].UpdateJointFromAnimation();
    }

    void LateUpdate()
    {
        // --- Camera follows player position, pitch is separate ---
        cameraPivot.position = transform.position + cameraOffset;
    }
}