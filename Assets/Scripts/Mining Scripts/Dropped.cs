using UnityEngine;
using Unity.Netcode;
public class Dropped : NetworkBehaviour
{
    [Header("Drop Settings")]
    public NetworkObject dropPrefab;
    public float dropScale = 0.3f;
    public Vector3 dropOffset = Vector3.up;

    public OreData oreData;

    public void DropItem()
    {
        Debug.Log("DropItem Called");
        if (dropPrefab.TryGetComponent<NetworkObject>(out NetworkObject netObj))
        {
            Debug.Log("NetworkObject found");
            DropItemServerRpc();
        }
    }

    public void PickedUp(GameObject item)
    {
        Debug.Log("PickUp Called");
        if (item.TryGetComponent<NetworkObject>(out NetworkObject netObj))
        {
            Debug.Log("NetworkObject found");
            PickUpServerRpc(netObj.NetworkObjectId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void PickUpServerRpc(ulong networkId, ServerRpcParams rpcParams = default)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkId, out NetworkObject netObj))
        {
            Dropped droppedScript = netObj.GetComponent<Dropped>();
            if (droppedScript == null)
            {
                Debug.LogWarning($"[Server] Object {networkId} has no Dropped script.");
                return;
            }

            // Identify which client asked to pick up
            ulong senderClientId = rpcParams.Receive.SenderClientId;
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(senderClientId, out var client))
            {
                Debug.LogWarning($"[Server] Could not find client {senderClientId}");
                return;
            }

            PlayerInventory inventoryScript = client.PlayerObject.GetComponent<PlayerInventory>();
            if (inventoryScript == null)
            {
                Debug.LogWarning($"[Server] Client {senderClientId} has no PlayerInventory.");
                return;
            }

            // Limit inventory size (example)
            if (inventoryScript.InventoryItems.Count >= 4)
            {
                Debug.Log($"[Server] Inventory full for client {senderClientId}");
                return;
            }

            // Case 1: Ore
            if (droppedScript.oreData != null)
            {
                inventoryScript.UpdateInventory(droppedScript.oreData.oreName, droppedScript.oreData);
                Debug.Log($"[Server] Ore {droppedScript.oreData.oreName} picked up by client {senderClientId}");
            }
            // Case 2: General item
            else
            {
                string itemName = netObj.name; // make sure prefab has proper name
                if (inventoryScript.itemType.itemDatabase.ContainsKey(itemName))
                {
                    inventoryScript.UpdateInventory(itemName); // this will hit your general item branch
                    Debug.Log($"[Server] Item {itemName} picked up by client {senderClientId}");
                }
                else
                {
                    Debug.LogWarning($"[Server] Item {itemName} not found in itemDatabase.");
                }
            }

            // Despawn after adding to inventory
            netObj.Despawn();
        }
        else
        {
            Debug.LogWarning($"[Server] No spawned object found for ID {networkId}");
        }
    }



    [ServerRpc(RequireOwnership = false)]
    public void DropItemServerRpc(ServerRpcParams rpcParams = default)
    {
        if (dropPrefab == null) return;

        Debug.LogWarning($"Dropping item for client {rpcParams.Receive.SenderClientId}");

        NetworkObject droppedItem = Instantiate(dropPrefab, transform.position + dropOffset, Quaternion.identity);

        // Copy OreData from mined rock
        Dropped droppedScript = droppedItem.GetComponent<Dropped>();
        if (droppedScript != null)
        {
            droppedScript.oreData = this.GetComponent<MineType>().oreData;

        }

        droppedItem.name = dropPrefab.name;
        droppedItem.transform.localScale *= dropScale;

        // Spawn for all clients
        // droppedItem.Spawn();

        // Assign tag on all clients
        SetDroppedTagClientRpc(droppedItem.NetworkObjectId);

        // Rigidbody for physics
        Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
        if (rb == null)
            rb = droppedItem.gameObject.AddComponent<Rigidbody>();

        rb.mass = 1f;
        rb.useGravity = true;
        rb.AddForce(Vector3.up * 2f + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)), ForceMode.Impulse);
        
        droppedItem.Spawn();
    }
    

    // [ServerRpc(RequireOwnership = false)]
    // public void DropItemServerRpc()
    // {
    //     if (dropPrefab == null) return;

    //     // Instantiate the prefab
    //     NetworkObject droppedItem = Instantiate(dropPrefab, transform.position + dropOffset, Quaternion.identity);

    //     // Rename immediately to remove (Clone)
    //     droppedItem.name = dropPrefab.name;

    //     // Assign tag BEFORE spawning
    //     droppedItem.gameObject.tag = "Dropped";

    //     // Scale
    //     droppedItem.transform.localScale *= dropScale;

    //     // Rigidbody and physics
    //     Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
    //     if (rb == null)
    //         rb = droppedItem.gameObject.AddComponent<Rigidbody>();
    //     rb.mass = 1f;
    //     rb.useGravity = true;
    //     rb.AddForce(Vector3.up * 2f + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)), ForceMode.Impulse);

    //     // Network spawn for all clients
    //     droppedItem.Spawn();

    //     Debug.Log($"Dropped {droppedItem.name}");
    // }





    [ClientRpc]
    void SetDroppedTagClientRpc(ulong networkId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkId, out NetworkObject netObj))
        {
            netObj.gameObject.tag = "Dropped";
        }
    }




    
}
