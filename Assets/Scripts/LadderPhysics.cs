using UnityEngine;

public class LadderPhysics : MonoBehaviour
{
    public float physicsDuration = 1f; // time to allow physics
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true; // default to kinematic
        rb.useGravity = false;
    }

    // Call this when the player grabs the ladder
    public void EnablePhysicsTemporarily()
    {
        StopAllCoroutines();
        rb.isKinematic = false;
        rb.useGravity = true;
        StartCoroutine(DisablePhysicsAfterDelay());
    }

    private System.Collections.IEnumerator DisablePhysicsAfterDelay()
    {
        yield return new WaitForSeconds(physicsDuration);
        rb.isKinematic = true;
        rb.useGravity = false;
    }
}
