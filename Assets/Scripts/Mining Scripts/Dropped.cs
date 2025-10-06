using UnityEngine;

public class Dropped : MonoBehaviour
{
    [Header("Drop Settings")]
    public float dropScale = 0.3f;
    public Vector3 dropOffset = Vector3.up;


    public void DropItem(GameObject item)
    {
        
        GameObject droppedItem = Instantiate(item, transform.position + dropOffset, Quaternion.identity);
        droppedItem.name = item.name;
        droppedItem.tag = "Dropped";
        droppedItem.transform.localScale *= dropScale;

        Rigidbody rb = droppedItem.AddComponent<Rigidbody>();
        rb.mass = 1f;              // optional
        rb.useGravity = true;
        rb.AddForce(Vector3.up * 2f + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)), ForceMode.Impulse);

    }

    public void PickedUp(GameObject item)
    {
        Debug.Log("Picked Up Item");
        PlayerInventory inventoryScript = FindObjectOfType<PlayerInventory>();
        if (inventoryScript != null)
        {
            Debug.Log(item.name);
            inventoryScript.UpdateInventory(item.name);
        }
        
        Destroy(item);

    }

}
