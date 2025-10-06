using UnityEngine;

public class ObjectSwapper : MonoBehaviour
{
    [Tooltip("The tag of the object that can trigger the swap")]
    [SerializeField] private string triggerTag = "Pickup";
    
    [Tooltip("The object to enable when the trigger is activated")]
    [SerializeField] private GameObject objectToEnable;

    private void Start()
    {
        // Make sure the object to enable is disabled at start
        if (objectToEnable != null)
        {
            objectToEnable.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the entering object has the correct tag
        if (other.CompareTag(triggerTag))
        {
            // Disable the object that entered the trigger
            other.gameObject.SetActive(false);
            
            // Enable the target object if it's assigned
            if (objectToEnable != null)
            {
                objectToEnable.SetActive(true);
            }
            
            // Optional: Play a sound or trigger an effect here
        }
    }
}
