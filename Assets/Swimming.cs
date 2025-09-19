using UnityEngine;

public class Swimming : MonoBehaviour
{
    public float waterDrag = 5f;        // slows movement in water
    public float waterAngularDrag = 5f; // slows rotation in water
    public float defaultDrag = 0f;      // default values on land
    public float defaultAngularDrag = 0.05f;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearDamping = waterDrag;
                rb.angularDamping = waterAngularDrag;
                Debug.Log("Player entered water - drag applied");
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearDamping = defaultDrag;
                rb.angularDamping = defaultAngularDrag;
                Debug.Log("Player left water - drag reset");
            }
        }
    }
}
