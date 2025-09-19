using UnityEngine;

public class First_Person_Camera : MonoBehaviour
{
    [Header("Sensitivity")]
    public float mouseSensitivity = 100f;

    [Header("References")]
    public Transform playerBody;  // Rigidbody player
    public Transform head;        // "Head" GameObject that holds the camera

    private Rigidbody playerRb;
    private float xRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerBody != null)
        {
            playerRb = playerBody.GetComponent<Rigidbody>();
            if (playerRb == null)
                Debug.LogError("Player Body needs a Rigidbody!");
            else
                playerRb.interpolation = RigidbodyInterpolation.Interpolate; // Smooth movement
        }

        if (head == null)
            Debug.LogError("Head reference is missing!");
    }

    void LateUpdate()
    {
        if (playerBody == null || playerRb == null || head == null)
            return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Vertical look (pitch) on the Head
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        head.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Horizontal look (yaw) on the Rigidbody
        Quaternion deltaRotation = Quaternion.Euler(0f, mouseX, 0f);
        playerRb.MoveRotation(playerRb.rotation * deltaRotation);
    }
}
