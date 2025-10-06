using Unity.Netcode;
using UnityEngine;

public class SnapPoint : NetworkBehaviour
{
    [Header("Snap Settings")]
    public float snapRadius = 0.5f;

    private NetworkObject snappedItem;

    public NetworkVariable<bool> isOccupied = new NetworkVariable<bool>(false);

    Transform position => transform;
    public NetworkObject itemPrefab;

    private Vector3 scale;

    void Update()
    {
        var localClientId = NetworkManager.Singleton.LocalClientId;
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(localClientId, out var client))
        {
            var playerInventory = client.PlayerObject.GetComponent<PlayerInventory>();
            if (playerInventory != null && playerInventory.currentHeldItem != null)
            {
                float distance = Vector3.Distance(
                    playerInventory.currentHeldItem.transform.position,
                    transform.position
                );

                if (distance <= snapRadius)
                {
                    SnapItemServerRpc();
                    RemovePlayerItemSnapServerRpc(playerInventory.currentHeldItem.name);
                }
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]

    private void RemovePlayerItemSnapServerRpc(string itemName, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;


        ulong senderClientId = rpcParams.Receive.SenderClientId;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(senderClientId, out var client))
        {
            PlayerInventory playerInventory = client.PlayerObject.GetComponent<PlayerInventory>();
            Debug.Log($"Removing {itemName} from inventory of client {client.ClientId}");
            if (playerInventory != null)
            {
                playerInventory.RemoveFromInventory(itemName);
                playerInventory.RemoveItemServer(itemName);
                playerInventory.ClearHeldItemClientRpc();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SnapItemServerRpc(ServerRpcParams rpcParams = default)
    {

        if (!IsServer) return;

        if (isOccupied.Value)
        {
            Debug.Log("SnapPoint is already occupied.");
            return;
        }
        isOccupied.Value = true;
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(senderClientId, out var client))
        {
            Transform snapTransform = this.transform;
            Vector3 snapPos = snapTransform.position;
            Quaternion snapRot = snapTransform.rotation;

            NetworkObject snapObj = Instantiate(itemPrefab, snapPos, snapRot);
            snapObj.name = itemPrefab.name;
            Rigidbody rb = snapObj.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = snapObj.gameObject.AddComponent<Rigidbody>();
                Debug.LogWarning("Rigidbody was missing on snapped item, added one.");
            }   
            rb.isKinematic = true;
            rb.useGravity = false;
            Debug.Log($"Item {snapObj.name} snapped into place (server).");
            snapObj.Spawn();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, snapRadius);
    }
}
