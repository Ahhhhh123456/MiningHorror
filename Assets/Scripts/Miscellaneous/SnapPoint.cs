using Unity.Netcode;
using UnityEngine;

public class SnapPoint : NetworkBehaviour
{
    [Header("Snap Settings")]
    public float snapRadius = 0.5f;

    private NetworkObject snappedItem;

    public GameObject blueprintPiece;
    private bool isSnapping = false;

    public NetworkVariable<bool> isOccupied = new NetworkVariable<bool>(false);

    Transform position => transform;
    public NetworkObject itemPrefab;

    private Vector3 scale;

    public event System.Action OnSnapChanged;

    void Update()
    {
        var localClientId = NetworkManager.Singleton.LocalClientId;
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(localClientId, out var client))
        {
            var playerInventory = client.PlayerObject.GetComponent<PlayerInventory>();
            

            // Check if player is holding an item
            if (playerInventory != null && playerInventory.currentHeldItem != null)
            {
                float distance = Vector3.Distance(
                    playerInventory.currentHeldItem.transform.position,
                    transform.position
                );

                Debug.Log($"Player {localClientId} holding {playerInventory.currentHeldItem.name}");

                // Player is close enough to snap
                if (distance <= snapRadius)
                {
                    if (isSnapping) return; // We are already processing a snap, stop.

                    // Check if the held item matches the snap point's tag
                    if (playerInventory.currentHeldItem.name == gameObject.tag.ToString())
                    {
                        isSnapping = true; // Set the cooldown
                        // ✅ MOVED THIS LINE INSIDE THE IF BLOCK
                        RemovePlayerItemSnapServerRpc(playerInventory.currentHeldItem.name); 
                        SnapItemServerRpc(); // Tell server to spawn the snapped item
                        
                    }
                    else
                    {
                        // Item doesn't match, do nothing
                        return;
                    }
                }
                else
                {
                    // Player is holding an item, but is too far away
                    isSnapping = false; // Reset the snap cooldown (This is Fix 3, and it's CORRECT)
                }
            }
            else
            {
                // Player is not holding any item.
                // We DON'T reset 'isSnapping' here (Fix 4).
                // Resetting it here causes the double-spawn bug.
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
                playerInventory.RemoveItemServer(itemName);
                playerInventory.ClearHeldItemClientRpc();
            }
        }
    }

    // [ServerRpc(RequireOwnership = false)]
    // private void SnapItemServerRpc(ServerRpcParams rpcParams = default)
    // {
    //     if (!IsServer) return;

    //     if (isOccupied.Value)
    //     {
    //         Debug.Log("SnapPoint is already occupied.");
    //         return;
    //     }

    //     isOccupied.Value = true;

    //     // Determine spawn position & rotation
    //     Vector3 snapPos = transform.position;
    //     Quaternion snapRot = transform.rotation;

    //     // Instantiate the item prefab
    //     NetworkObject snapObj = Instantiate(itemPrefab, snapPos, snapRot);

    //     snapObj.name = itemPrefab.name;

    //     // Spawn first
    //     snapObj.Spawn();

    //     // Now it's safe to parent
    //     snapObj.transform.SetParent(transform);
    //     snapObj.transform.localPosition = Vector3.zero;
    //     snapObj.transform.localRotation = Quaternion.identity;

    //     // 🔥 Register with SnapManager immediately after occupation
    //     SnapManager snapManager = GetComponentInParent<SnapManager>();
    //     if (snapManager != null)
    //     {
    //         snapManager.RegisterSnappedPiece(snapObj, blueprintPiece.GetComponent<NetworkObject>() );
    //     }

    //     // Make it non-interactable for clients
    //     SetTagClientRpc(snapObj.NetworkObjectId, "Untagged");

    //     // Assign SnapPoint reference
    //     if (snapObj.TryGetComponent<SnappedItem>(out var snappedItemScript))
    //     {
    //         snappedItemScript.snapPoint = this;
    //     }

    //     // Ensure rigidbody is kinematic
    //     Rigidbody rb = snapObj.GetComponent<Rigidbody>();
    //     if (rb == null) rb = snapObj.gameObject.AddComponent<Rigidbody>();
    //     rb.isKinematic = true;
    //     rb.useGravity = false;

    //     Debug.Log($"Item {snapObj.name} snapped into place (server).");

    //     // Notify SnapManager
    //     OnSnapChanged?.Invoke();
    // }

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

        // Instantiate & spawn snapped item
        Vector3 snapPos = transform.position;
        Quaternion snapRot = transform.rotation;
        NetworkObject snapObj = Instantiate(itemPrefab, snapPos, snapRot);
        snapObj.Spawn();
        snapObj.transform.SetParent(transform);
        snapObj.transform.localPosition = Vector3.zero;
        snapObj.transform.localRotation = Quaternion.identity;

        // Register with SnapManager
        SnapManager snapManager = GetComponentInParent<SnapManager>();
        if (snapManager != null && blueprintPiece != null)
            snapManager.RegisterSnappedPiece(snapObj);

        // Make non-interactable
        SetTagClientRpc(snapObj.NetworkObjectId, "Untagged");

        // Assign SnapPoint reference
        if (snapObj.TryGetComponent<SnappedItem>(out var snappedItemScript))
            snappedItemScript.snapPoint = this;

        // Rigidbody
        Rigidbody rb = snapObj.GetComponent<Rigidbody>();
        if (rb == null) rb = snapObj.gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Clear player inventory **server-side**
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(senderClientId, out var client))
        {
            PlayerInventory playerInventory = client.PlayerObject.GetComponent<PlayerInventory>();
            if (playerInventory != null && playerInventory.currentHeldItem != null)
            {
                string itemName = playerInventory.currentHeldItem.name;
                playerInventory.RemoveItemServer(itemName);
                playerInventory.ClearHeldItemClientRpc();
            }
        }

        Debug.Log($"Item {snapObj.name} snapped into place (server).");

        // Notify SnapManager
        OnSnapChanged?.Invoke();
    }



    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, snapRadius);
    }
    

    // Sets tag to untagged for clients so snapped items aren't interactable
    [ClientRpc]
    void SetTagClientRpc(ulong netId, string newTag)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netId, out var netObj))
        {
            netObj.gameObject.tag = newTag;
        }
    }
}
