using UnityEngine;
using Unity.Netcode;
using System.Linq;
public class Dropped : NetworkBehaviour
{
    [Header("Drop Settings")]
    public NetworkObject dropPrefab;
    public float dropScale = 0.1f;
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
    public void DropItem(Vector3 cameraForward, Vector3 hitNormal)
    {
        Debug.Log("DropItem Called");
        if (dropPrefab.TryGetComponent<NetworkObject>(out NetworkObject netObj))
        {
            Debug.Log("NetworkObject found");
            DropItemServerRpc(cameraForward, hitNormal);
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

        if (inventoryScript.NetworkItems.Count == inventoryScript.maxSlots)
        {
            Debug.Log($"[Server] Client {senderClientId} picked up {(droppedScript.oreData != null ? droppedScript.oreData.oreName : netObj.name)}");
            return;
        }

        if (inventoryScript == null)
        {
            Debug.LogWarning($"[Server] Client {senderClientId} has no PlayerInventory.");
            return;
        }

        // Use server-authoritative add
        if (droppedScript.oreData != null)
        {
            inventoryScript.AddItemServer(droppedScript.oreData.oreName, droppedScript.oreData);
        }
        else
        {
            inventoryScript.AddItemServer(netObj.name);
        }
        // Despawn the dropped object
        if (inventoryScript)
        Debug.Log($"[Server] Client {senderClientId} is picking up { (droppedScript.oreData != null ? droppedScript.oreData.oreName : netObj.name) }");
        netObj.Despawn();

    }

    [ServerRpc(RequireOwnership = false)]
    public void DropItemServerRpc(Vector3 cameraForward, Vector3 hitNormal, ServerRpcParams rpcParams = default)
    {
        if (oreData == null || oreData.dropPrefab == null)
        {
            Debug.LogError("OreData or dropPrefab not assigned!");
            return;
        }

        Debug.LogWarning($"Dropping {oreData.oreName} for client {rpcParams.Receive.SenderClientId}");
        Debug.Log($"Camera forward: {cameraForward}, Hit normal: {hitNormal}");

        // Spawn slightly above the ore so it doesn't intersect
        Vector3 spawnPos = transform.position + hitNormal * 0.2f + Vector3.up * 0.2f;
        NetworkObject droppedItem = Instantiate(oreData.dropPrefab, spawnPos, Quaternion.identity);

        if (droppedItem.TryGetComponent(out Dropped droppedScript))
            droppedScript.oreData = oreData;

        droppedItem.gameObject.tag = "Dropped";
        droppedItem.name = oreData.dropPrefab.name;
        droppedItem.transform.localScale *= dropScale;
        droppedItem.Spawn();
        SetNameClientRpc(droppedItem.NetworkObjectId, oreData.dropPrefab.name);

        Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
        if (rb == null) rb = droppedItem.gameObject.AddComponent<Rigidbody>();

        rb.mass = 1f;
        rb.useGravity = true;
        rb.isKinematic = false;

        // Decide force direction based on camera vs surface normal
        Vector3 forceDir;
        float dot = Vector3.Dot(cameraForward.normalized, hitNormal.normalized);

        if (dot < -0.5f)
        {
            // Camera pointing roughly at the surface → push away from surface
            forceDir = hitNormal;
        }
        else if (dot > 0.5f)
        {
            // Camera pointing roughly opposite → push toward player
            forceDir = -hitNormal;
        }
        else
        {
            // Side angles → just use camera direction
            forceDir = cameraForward;
        }

        Vector3 force = forceDir.normalized * 1.5f + Vector3.up * 2f;

        // Apply force on server
        rb.AddForce(force, ForceMode.Impulse);

        // Apply same force on clients
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
