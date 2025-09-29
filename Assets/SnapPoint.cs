using UnityEngine;

public class SnapPoint : MonoBehaviour
{
    [Header("Snap Settings")]
    public bool isOccupied = false;
    public float snapRadius = 0.5f;
    public bool releaseHoldAfterSnap = true;

    private void Update()
    {
        if (isOccupied) return;

        PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory == null || playerInventory.currentHeldItem == null) return;

        GameObject heldItem = playerInventory.currentHeldItem;
        float distance = Vector3.Distance(heldItem.transform.position, transform.position);

        if (distance <= snapRadius)
        {
            SnapItem(heldItem, playerInventory);
        }
    }

    private void SnapItem(GameObject item, PlayerInventory playerInventory)
    {
        // Remove from inventory first
        string itemName = item.name.Replace("(Clone)", "");
        playerInventory.RemoveFromInventory(itemName);

        // Detach from camera/hold position
        item.transform.SetParent(null);

        // Move and rotate into place
        item.transform.position = transform.position;
        item.transform.rotation = transform.rotation;

        // Make it kinematic so it stays in place
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        isOccupied = true;

        // Release hold reference
        if (playerInventory.currentHeldItem == item)
            playerInventory.currentHeldItem = null;

        Debug.Log($"Item {itemName} snapped into place and removed from inventory!");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, snapRadius);
    }
}
