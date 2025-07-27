using UnityEngine;
using System.Linq;

public class DrillPart : MonoBehaviour
{
    [Header("Attachment Settings")]
    [SerializeField] private string compatiblePartTag = "DrillPart";
    [SerializeField] private Vector3 snapOffset;
    [SerializeField] private Vector3 snapRotation;

    [Header("Trigger Settings")]
    [SerializeField] private Transform snapPoint; // Assign this to an empty GameObject marking the attachment point
    [SerializeField] private float connectionRadius = 0.1f; // How close the snap points need to be

    private bool isAttached = false;
    private Transform attachedTo;
    private InteractableItem interactableItem;
    private Collider[] triggerColliders;

    private void Start()
    {
        interactableItem = GetComponent<InteractableItem>();
        triggerColliders = GetComponentsInChildren<Collider>().Where(c => c.isTrigger).ToArray();
        
        // Make sure we have a snap point
        if (snapPoint == null)
        {
            Debug.LogError("No snap point assigned to " + gameObject.name);
            enabled = false;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Skip if already attached, being held, or not a compatible part
        if (isAttached || (interactableItem != null && interactableItem.isHeld) || 
            !other.CompareTag(compatiblePartTag) || other.gameObject == gameObject)
            return;

        DrillPart otherPart = other.GetComponentInParent<DrillPart>();
        if (otherPart == null || otherPart == this) return;

        // Check if we should attach
        if (ShouldAttach(otherPart))
        {
            AttachTo(otherPart.transform);
        }
    }

    private bool ShouldAttach(DrillPart otherPart)
    {
        // Check if the other part is being held
        if (otherPart.interactableItem != null && otherPart.interactableItem.isHeld)
            return false;

        // Check distance between snap points
        float distance = Vector3.Distance(snapPoint.position, otherPart.snapPoint.position);
        return distance <= connectionRadius;
    }

    private void AttachTo(Transform otherPart)
    {
        isAttached = true;
        attachedTo = otherPart;
        
        // Disable physics
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        
        // Parent to the other part and apply offset/rotation
        transform.SetParent(otherPart);
        transform.localPosition = snapOffset;
        transform.localEulerAngles = snapRotation;
        
        // Disable triggers to prevent multiple attachments
        foreach (var col in triggerColliders)
        {
            col.enabled = false;
        }
        
        // Disable the interactable component
        if (interactableItem != null)
        {
            interactableItem.enabled = false;
        }
    }

    public void Detach()
    {
        if (!isAttached) return;
        
        isAttached = false;
        transform.SetParent(null);
        
        // Re-enable physics
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }
        
        // Re-enable triggers
        foreach (var col in triggerColliders)
        {
            col.enabled = true;
        }
        
        // Re-enable interaction
        if (interactableItem != null)
        {
            interactableItem.enabled = true;
        }
        
        attachedTo = null;
    }

    // Visualize the connection point in the editor
    private void OnDrawGizmosSelected()
    {
        if (snapPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(snapPoint.position, connectionRadius);
        }
    }
}