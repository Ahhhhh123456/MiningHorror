using UnityEngine;
using Unity.Netcode;
using System.Linq;
public class Dropped : NetworkBehaviour
{
    [Header("Drop Settings")]
    public NetworkObject dropPrefab;
    public float dropScale = 0.3f;
    public Vector3 dropOffset = Vector3.up;
    private Vector3 pendingImpulse;
    private bool hasPendingImpulse = false;

    public OreData oreData;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (hasPendingImpulse && TryGetComponent(out Rigidbody rb))
        {
            rb.AddForce(pendingImpulse, ForceMode.Impulse);
            hasPendingImpulse = false;
        }
    }

    // Called From MineType
    public void DropItem()
    {
        Debug.Log("DropItem Called");
        if (dropPrefab.TryGetComponent<NetworkObject>(out NetworkObject netObj))
        {
            Debug.Log("NetworkObject found");
            DropItemServerRpc();
        }
    }

    // Called From LookAndClick
    public void PickedUp(GameObject item)
    {
        if (item.TryGetComponent<NetworkObject>(out NetworkObject netObj))
        {
            Debug.Log("NetworkObject found");
            PickUpServerRpc(netObj.NetworkObjectId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void PickUpServerRpc(ulong networkId, ServerRpcParams rpcParams = default)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkId, out NetworkObject netObj))
        {
            Debug.LogWarning($"[Server] No spawned object found for ID {networkId}");
            return;
        }

        Dropped droppedScript = netObj.GetComponent<Dropped>();
        if (droppedScript == null)
        {
            Debug.LogWarning($"[Server] Object {networkId} has no Dropped script.");
            return;
        }

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

        // Use server-authoritative add
        if (droppedScript.oreData != null)
        {
            Debug.LogWarning($"[Server] Adding {droppedScript.oreData.oreName} to client {senderClientId}'s inventory.");
            inventoryScript.AddItemServer(droppedScript.oreData.oreName, droppedScript.oreData);
        }
        else
        {
            Debug.LogWarning($"[Server] Adding item by prefab name {netObj.name} to client {senderClientId}'s inventory.");
            inventoryScript.AddItemServer(netObj.name);
        }
        // Despawn the dropped object
        netObj.Despawn();

    }

    [ServerRpc(RequireOwnership = false)]
    public void DropItemServerRpc(ServerRpcParams rpcParams = default)
    {
        if (oreData == null || oreData.dropPrefab == null)
        {
            Debug.LogError("OreData or dropPrefab not assigned!");
            return;
        }

        Debug.LogWarning($"Dropping {oreData.oreName} for client {rpcParams.Receive.SenderClientId}");

        // Instantiate
        NetworkObject droppedItem = Instantiate(oreData.dropPrefab, transform.position + dropOffset, Quaternion.identity);

        // Copy OreData
        if (droppedItem.TryGetComponent(out Dropped droppedScript))
        {
            droppedScript.oreData = oreData;
        }

        droppedItem.gameObject.tag = "Dropped";

        droppedItem.name = oreData.dropPrefab.name;
        droppedItem.transform.localScale *= dropScale;
        droppedItem.Spawn();
        SetNameClientRpc(droppedItem.NetworkObjectId, oreData.dropPrefab.name);

        Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
        if (rb == null)
            rb = droppedItem.gameObject.AddComponent<Rigidbody>();

        rb.mass = 1f;
        rb.useGravity = true; // Turn on gravity now
        rb.isKinematic = false; // Make sure physics is active

        Vector3 force = Vector3.up * 2f + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));

        // Apply on server
        rb.AddForce(force, ForceMode.Impulse);

        // Apply on clients
        ApplyImpulseClientRpc(droppedItem.NetworkObjectId, force);
        SetDroppedTagClientRpc(droppedItem.NetworkObjectId);

        Debug.Log($"Dropped {droppedItem.name} with force {force}");
    }


    [ClientRpc]
    void ApplyImpulseClientRpc(ulong networkId, Vector3 force)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkId, out NetworkObject netObj))
        {
            if (netObj.TryGetComponent(out Rigidbody rb))
            {
                rb.useGravity = true;     // ✅ turn on gravity
                rb.isKinematic = false;   // ensure physics is active
                rb.AddForce(force, ForceMode.Impulse);
            }
            else if (netObj.TryGetComponent(out Dropped dropped))
            {
                // Rigidbody not ready yet; store for OnNetworkSpawn
                dropped.pendingImpulse = force;
                dropped.hasPendingImpulse = true;
            }
        }
    }


    [ClientRpc]
    void SetNameClientRpc(ulong networkId, string newName)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkId, out NetworkObject netObj))
        {
            netObj.name = newName;
        }
    }

    [ClientRpc]
    void SetDroppedTagClientRpc(ulong networkId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkId, out NetworkObject netObj))
        {
            netObj.gameObject.tag = "Dropped";
        }
    }




    
}
