using UnityEngine;

public class Swimming : MonoBehaviour
{
    public float buoyancyForce = 2f;   // how strong the float feels
    public float damping = 1f;         // slows descent

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Counter gravity
                Vector3 force = Vector3.up * buoyancyForce;

                // Add force smoothly
                rb.AddForce(force, ForceMode.Acceleration);

                // Optional: add drag to make movement smooth
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y * (1f - Time.deltaTime * damping), rb.linearVelocity.z);
            }
        }
    }
}
