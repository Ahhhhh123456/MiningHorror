using UnityEngine;
using Unity.Netcode;

public class PlayerSpawn : NetworkBehaviour
{
    // The server sets this spawn position, clients automatically receive updates
    public NetworkVariable<Vector3> spawnPosition = new NetworkVariable<Vector3>(
        writePerm: NetworkVariableWritePermission.Server);

    private void OnEnable()
    {
        // Subscribe to value changes
        spawnPosition.OnValueChanged += OnSpawnPositionChanged;
    }

    private void OnDisable()
    {
        spawnPosition.OnValueChanged -= OnSpawnPositionChanged;
    }

    /// <summary>
    /// This is called on all clients (including owner) when the server sets the spawn position
    /// </summary>
    private void OnSpawnPositionChanged(Vector3 oldPos, Vector3 newPos)
    {
        transform.position = newPos;
        transform.rotation = Quaternion.identity; // Or desired rotation
        Debug.Log($"Player moved to spawn position {newPos}");
    }

    /// <summary>
    /// Called by J key or other triggers to request moving to spawn
    /// </summary>
    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.J))
        {
            // Request server to move this player to spawn
            MoveToSpawnServerRpc();
        }
    }

    /// <summary>
    /// Ask the server to move this client’s player
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void MoveToSpawnServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        NetworkObject playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;

        if (playerObj != null)
        {
            playerObj.transform.position = spawnPosition.Value;
            playerObj.transform.rotation = Quaternion.identity;
            Debug.Log($"[Server] Moved player {clientId} to spawn {spawnPosition.Value}");
        }
    }

    /// <summary>
    /// Server should call this when the spawn point is ready
    /// </summary>
    public void SetSpawnPosition(Vector3 pos)
    {
        if (IsServer)
        {
            spawnPosition.Value = pos;
            Debug.Log($"[Server] Spawn position set to {pos}");
        }
    }
}
