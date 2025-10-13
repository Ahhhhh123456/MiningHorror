// using Unity.Netcode;
// using UnityEngine;

// public class BreakableObject : NetworkBehaviour
// {
//     public GameObject breakEffectPrefab; // optional particle effect

//     // Called locally when player interacts
//     public void Break()
//     {
//         // Only request break from the server
//         BreakServerRpc();
//     }

//     // Server handles the authoritative break
//     [ServerRpc(RequireOwnership = false)]
//     void BreakServerRpc(ServerRpcParams rpcParams = default)
//     {
//         // Optional: spawn particle effects for everyone
//         if (breakEffectPrefab != null)
//             PlayBreakEffectClientRpc(transform.position);

//         // Remove object from all clients
//         NetworkObject.Despawn();
//     }

//     // Client-side visual effect
//     [ClientRpc]
//     void PlayBreakEffectClientRpc(Vector3 position)
//     {
//         if (breakEffectPrefab != null)
//             Instantiate(breakEffectPrefab, position, Quaternion.identity);
//     }
// }
