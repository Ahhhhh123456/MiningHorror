using Unity.Netcode;
using UnityEngine;

public class SnapPoint : NetworkBehaviour
{
    [Header("Snap Settings")]
    public float snapRadius = 0.5f;

    private NetworkObject snappedItem;

    Transform position => transform;
    public NetworkObject itemPrefab;

    private Vector3 scale;

    void Update()
    {
        if (!IsServer && !IsOwner) return; // Only the owner/client can request snapping

        PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory == null) return;

        if (snappedItem == null && playerInventory.currentHeldItem != null)
        {
            GameObject heldItem = playerInventory.currentHeldItem;
            float distance = Vector3.Distance(heldItem.transform.position, transform.position);

            if (distance <= snapRadius)
            {
                SnapItemServerRpc();
            }
        }
        else if (snappedItem != null && Input.GetKeyDown(KeyCode.E))
        {
            float distance = Vector3.Distance(playerInventory.transform.position, transform.position);
            if (distance <= 2f)
            {
                UnsnapItemServerRpc(snappedItem.NetworkObjectId, playerInventory.OwnerClientId);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SnapItemServerRpc(ServerRpcParams rpcParams = default)
    {

        if (!IsServer) return;

        ulong senderClientId = rpcParams.Receive.SenderClientId;


        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(senderClientId, out var client))
        { 
            Debug.Log($"Player {senderClientId} is snapping an item.");
        }
        

        // NetworkObject netObj = Instantiate(itemPrefab, position).GetComponent<NetworkObject>();
        // netObj.Spawn();
        // if (netObj == null) return;

            //     // Lock in place
            //     Rigidbody rb = netObj.GetComponent<Rigidbody>();
            //     if (rb != null)
            //     {
            //         rb.isKinematic = true;
            //         rb.useGravity = false;
            //     }

            //     netObj.transform.position = transform.position;
            //     netObj.transform.rotation = transform.rotation;

            //     snappedItem = netObj;
            //Debug.Log($"Item {netObj.name} snapped into place (server).");
    }

    [ServerRpc(RequireOwnership = false)]
    private void UnsnapItemServerRpc(ulong itemId, ulong clientId)
    {
        NetworkObject netObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[itemId];
        if (netObj == null) return;

        Rigidbody rb = netObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        PlayerInventory playerInventory = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject
            .GetComponent<PlayerInventory>();

        if (playerInventory != null)
        {
            playerInventory.AddItemServer(netObj.name.Replace("(Clone)", ""));
            playerInventory.currentHeldItem = netObj.gameObject;
            netObj.transform.SetParent(playerInventory.holdPosition);
            netObj.transform.localPosition = Vector3.zero;
            netObj.transform.localRotation = Quaternion.identity;
        }

        snappedItem = null;
        Debug.Log($"Item {netObj.name} unsnapped (server).");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, snapRadius);
    }
}
