using UnityEngine;

public class SnapPoint : MonoBehaviour
{
    [Header("Snap Settings")]
    public float snapRadius = 0.5f;

    private GameObject snappedItem;

    void Update()
    {
        PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory == null) return;

        // If nothing is snapped and player is holding something
        if (snappedItem == null && playerInventory.currentHeldItem != null)
        {
            GameObject heldItem = playerInventory.currentHeldItem;
            float distance = Vector3.Distance(heldItem.transform.position, transform.position);

            if (distance <= snapRadius)
            {
                SnapItem(heldItem, playerInventory);
            }
        }
        // If something is snapped and player presses E near it, unsnap
        else if (snappedItem != null && Input.GetKeyDown(KeyCode.E))
        {
            float distance = Vector3.Distance(playerInventory.transform.position, transform.position);
            if (distance <= 2f) // player close enough
            {
                UnsnapItem(playerInventory);
            }
        }
    }

    private void SnapItem(GameObject item, PlayerInventory playerInventory)
    {
        string itemName = item.name.Replace("(Clone)", "");
        playerInventory.RemoveFromInventory(itemName);

        // Detach from camera
        item.transform.SetParent(null);

        // Position at snap point
        item.transform.position = transform.position;
        item.transform.rotation = transform.rotation;

        // Lock in place
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        snappedItem = item;
        if (playerInventory.currentHeldItem == item)
            playerInventory.currentHeldItem = null;

        Debug.Log($"Item {itemName} snapped into place!");
    }

    private void UnsnapItem(PlayerInventory playerInventory)
    {
        if (snappedItem == null) return;

        string itemName = snappedItem.name.Replace("(Clone)", "");

        // Unlock physics
        Rigidbody rb = snappedItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        // Give back to player inventory
        playerInventory.UpdateInventory(itemName); // <- make sure this exists
        playerInventory.currentHeldItem = snappedItem;
        snappedItem.transform.SetParent(playerInventory.holdPosition);
        snappedItem.transform.localPosition = Vector3.zero;
        snappedItem.transform.localRotation = Quaternion.identity;

        Debug.Log($"Item {itemName} unsnapped and returned to player!");

        snappedItem = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, snapRadius);
    }
}
