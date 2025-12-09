using UnityEngine;
using Unity.Netcode;

public class TeleportPlayer : NetworkBehaviour
{
    // fallback position in case the player's PlayerSpawn isn't present or has no value
    [SerializeField] private Vector3 fallbackTeleportPosition = Vector3.zero;

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return; // Only server handles teleport logic

        if (collision.gameObject.CompareTag("Player"))
        {
            NetworkObject netObj = collision.gameObject.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                ulong clientId = netObj.OwnerClientId;
                TeleportPlayerServerRpc(clientId);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void TeleportPlayerServerRpc(ulong clientId)
    {
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId)) return;

        NetworkObject playerObj =
            NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;

        if (playerObj == null) return;

        // Try to read PlayerSpawn.spawnPosition (NetworkVariable) on the server
        PlayerSpawn playerSpawn = playerObj.GetComponent<PlayerSpawn>();
        Vector3 targetPos = fallbackTeleportPosition;

        if (playerSpawn != null)
        {
            // Important: this reads the NetworkVariable directly on the server
            targetPos = playerSpawn.spawnPosition.Value;
        }
        else
        {
            Debug.LogWarning($"PlayerSpawn component missing on player {clientId}. Using fallback position.");
        }

        // Teleport on SERVER (server authoritative)
        Debug.Log($"Teleporting player {clientId} to {targetPos}");

        playerObj.transform.position = targetPos;
        playerObj.transform.rotation = Quaternion.identity;

        TeleportClientRpc(clientId, targetPos);

        // If you rely on NetworkTransform, the transform will synchronize to clients.
    }


    [ClientRpc]
    private void TeleportClientRpc(ulong clientId, Vector3 targetPos)
    {
        if (NetworkManager.Singleton.LocalClientId != clientId)
            return; // Only teleport the correct client

        var playerObj = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (playerObj == null) return;

        playerObj.transform.position = targetPos;
        playerObj.transform.rotation = Quaternion.identity;

        Debug.Log($"[Client] Teleported local player {clientId} to {targetPos}");

        // Reset velocity if using Rigidbody
        if (playerObj.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
