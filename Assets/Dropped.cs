using UnityEngine;

public class Dropped : MonoBehaviour
{
    [Header("Drop Settings")]
    public float dropScale = 0.3f;
    public Vector3 dropOffset = Vector3.up;


    public void DropItem(GameObject item)
    {
        //Debug.Log("Dropping Item");
        GameObject droppedItem = Instantiate(item, transform.position + dropOffset, Quaternion.identity);
        droppedItem.transform.localScale *= dropScale;

        Rigidbody rb = droppedItem.AddComponent<Rigidbody>();
        rb.mass = 1f;              // optional
        rb.useGravity = true;
        rb.AddForce(Vector3.up * 2f + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)), ForceMode.Impulse);

    }


}
