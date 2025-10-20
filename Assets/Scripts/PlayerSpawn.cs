using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class PlayerSpawn : NetworkBehaviour
{
    [SerializeField] private Transform spawnPoint;

    void Update()
    {
        // ✅ Only let the local player (the owner) read keyboard input
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.J))
        {
            // Send the request to the server
            RequestRespawnServerRpc();
        }
    }

    [ServerRpc]
    private void RequestRespawnServerRpc(ServerRpcParams rpcParams = default)
    {
        if (spawnPoint != null)
        {
            // Move the player’s NetworkObject on the server
            NetworkObject.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);

            // Then sync to all clients
            OnPlayerSpawnClientRpc();
        }
    }

    [ClientRpc]
    private void OnPlayerSpawnClientRpc()
    {
        if (spawnPoint != null)
        {
            NetworkObject.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        }
    }
}
