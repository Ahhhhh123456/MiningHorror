using UnityEngine;

public class Headbob : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private bool enable = true;
    [SerializeField, Range(0, 0.1f)] private float amplitude = 0.015f;
    [SerializeField, Range(0, 30)] private float frequency = 10.0f;

    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private Rigidbody playerBody;  // NEW

    private float toggleSpeed = 3.0f;
    private Vector3 startPos;

    private void Awake()
    {
        startPos = cameraTransform.localPosition;

        if (playerBody == null)
            Debug.LogWarning("Headbob: Player Rigidbody not assigned!");
    }

    private void Update()
    {
        if (!enable || playerBody == null) return;

        CheckMotion();
        ResetPosition();
        cameraTransform.LookAt(FocusTarget());
    }

    private Vector3 FootStepMotion()
    {
        Vector3 pos = Vector3.zero;
        pos.y += Mathf.Sin(Time.time * frequency) * amplitude;
        pos.x += Mathf.Cos(Time.time * frequency / 2f) * amplitude * 2f;
        return pos;
    }

    private void CheckMotion()
    {
        Vector3 horizontalVel = new Vector3(playerBody.linearVelocity.x, 0, playerBody.linearVelocity.z);
        float speed = horizontalVel.magnitude;

        if (speed < toggleSpeed) return;
        // If you have grounded logic, add the check here
        // if (!isGrounded) return;

        PlayMotion(FootStepMotion());
    }

    private void PlayMotion(Vector3 motion)
    {
        cameraTransform.localPosition += motion;
    }

    private Vector3 FocusTarget()
    {
        Vector3 pos = transform.position;
        pos.y += cameraHolder.localPosition.y;
        pos += cameraHolder.forward * 15f;
        return pos;
    }

    private void ResetPosition()
    {
        if (cameraTransform.localPosition == startPos) return;

        cameraTransform.localPosition =
            Vector3.Lerp(cameraTransform.localPosition, startPos, Time.deltaTime);
    }
}
