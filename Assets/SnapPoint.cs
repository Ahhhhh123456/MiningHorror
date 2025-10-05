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

        if (itemPrefab == null)
        {
            Debug.LogWarning("SnapItemServerRpc: itemPrefab is null!");
            return;
        }

        // 1. Instantiate the item on the server
        NetworkObject netObj = Instantiate(itemPrefab, transform.position, transform.rotation).GetComponent<NetworkObject>();

        // 2. Spawn it to all clients
        netObj.Spawn();

        // 3. Lock physics
        Rigidbody rb = netObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        snappedItem = netObj;

        Debug.Log($"Item {netObj.name} snapped into place (server) and should be visible to all clients.");
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
