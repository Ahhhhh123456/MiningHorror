// using UnityEngine;
// using System.Linq;

// public class DrillPart : MonoBehaviour
// {
//     [Header("Attachment Settings")]
//     [SerializeField] private string compatiblePartTag = "DrillPart";
//     [SerializeField] private float connectionRadius = 0.1f; // How close the snap points need to be

//     [Header("References")]
//     [SerializeField] private Transform snapPoint; // The point where other parts will attach to this one
//     [SerializeField] private Transform attachmentPoint; // The point that will attach to other parts' snap points

//     private bool isAttached = false;
//     private Transform attachedTo;
//     private InteractableItem interactableItem;
//     private Collider[] triggerColliders;

//     private void Start()
//     {
//         interactableItem = GetComponent<InteractableItem>();
//         triggerColliders = GetComponentsInChildren<Collider>().Where(c => c.isTrigger).ToArray();
        
//         // Make sure we have required points
//         if (snapPoint == null || attachmentPoint == null)
//         {
//             Debug.LogError("Snap point or attachment point not assigned to " + gameObject.name);
//             enabled = false;
//         }
//     }

//     private void OnTriggerStay(Collider other)
//     {
//         // Skip if already attached, being held, or not a compatible part
//         if (isAttached || (interactableItem != null && interactableItem.isHeld) || 
//             !other.CompareTag(compatiblePartTag) || other.gameObject == gameObject)
//             return;

//         DrillPart otherPart = other.GetComponentInParent<DrillPart>();
//         if (otherPart == null || otherPart == this) return;

//         // Check if we should attach
//         if (ShouldAttach(otherPart))
//         {
//             AttachTo(otherPart);
//         }
//     }

//     private bool ShouldAttach(DrillPart otherPart)
//     {
//         // Check if the other part is being held
//         if (otherPart.interactableItem != null && otherPart.interactableItem.isHeld)
//             return false;

//         // Check distance between this part's attachment point and other part's snap point
//         float distance = Vector3.Distance(attachmentPoint.position, otherPart.snapPoint.position);
//         return distance <= connectionRadius;
//     }

//     private void AttachTo(DrillPart otherPart)
//     {
//         isAttached = true;
//         attachedTo = otherPart.transform;
        
//         // Calculate the position and rotation needed to align attachmentPoint with otherPart's snapPoint
//         Vector3 targetPosition = otherPart.snapPoint.position - (attachmentPoint.position - transform.position);
//         Quaternion targetRotation = otherPart.snapPoint.rotation * Quaternion.Inverse(Quaternion.Inverse(otherPart.transform.rotation) * attachmentPoint.rotation);

//         // Disable physics
//         Rigidbody rb = GetComponent<Rigidbody>();
//         if (rb != null)
//         {
//             rb.isKinematic = true;
//         }
        
//         // Parent to the other part
//         transform.SetParent(otherPart.transform);
        
//         // Position and rotate to align the attachment points
//         transform.position = targetPosition;
//         transform.rotation = targetRotation;
        
//         // Disable triggers to prevent multiple attachments
//         foreach (var col in triggerColliders)
//         {
//             col.enabled = false;
//         }
        
//         // Disable the interactable component
//         if (interactableItem != null)
//         {
//             interactableItem.enabled = false;
//         }
//     }

//     public void Detach()
//     {
//         if (!isAttached) return;
        
//         isAttached = false;
//         transform.SetParent(null);
        
//         // Re-enable physics
//         Rigidbody rb = GetComponent<Rigidbody>();
//         if (rb != null)
//         {
//             rb.isKinematic = false;
//         }
        
//         // Re-enable triggers
//         foreach (var col in triggerColliders)
//         {
//             col.enabled = true;
//         }
        
//         // Re-enable interaction
//         if (interactableItem != null)
//         {
//             interactableItem.enabled = true;
//         }
        
//         attachedTo = null;
//     }

//     private void OnDrawGizmosSelected()
//     {
//         if (snapPoint != null)
//         {
//             Gizmos.color = Color.blue;
//             Gizmos.DrawWireSphere(snapPoint.position, connectionRadius);
//         }
//         if (attachmentPoint != null)
//         {
//             Gizmos.color = Color.green;
//             Gizmos.DrawWireSphere(attachmentPoint.position, 0.05f);
//         }
//     }
// }