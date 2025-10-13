// using Unity.Netcode;
// using UnityEngine;

// public class PickUpItem : NetworkBehaviour
// {
//     public NetworkVariable<bool> IsPickedUp = new NetworkVariable<bool>(false);

//     public void PickUp()
//     {
//         if (!IsPickedUp.Value)
//             PickUpServerRpc();
//     }

//     [ServerRpc(RequireOwnership = false)]
//     void PickUpServerRpc(ServerRpcParams rpcParams = default)
//     {
//         IsPickedUp.Value = true;
//         NetworkObject.Despawn(); // removes from all clients
//     }

//     public void Drop(Vector3 position)
//     {
//         DropServerRpc(position);
//     }

//     [ServerRpc(RequireOwnership = false)]
//     void DropServerRpc(Vector3 position)
//     {
//         transform.position = position;
//         NetworkObject.Spawn();
//         IsPickedUp.Value = false;
//     }
// }
