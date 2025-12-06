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


    private void OnSpawnPositionChanged(Vector3 oldPos, Vector3 newPos)
    {
        transform.position = newPos;
        transform.rotation = Quaternion.identity; // Or desired rotation
        Debug.Log($"Player moved to spawn position {newPos}");
    }


    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.J))
        {
            // Request server to move this player to spawn
            MoveToSpawnServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void MoveToSpawnServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        NetworkObject playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;

        if (playerObj != null)
        {
            //playerObj.transform.position = spawnPosition.Value;
            //playerObj.transform.rotation = Quaternion.identity;
            MoveSpawnClientRpc(spawnPosition.Value);

    
        }
    }

    [ClientRpc]

    private void MoveSpawnClientRpc(Vector3 pos, ClientRpcParams rpcParams = default)
    {
        if (IsOwner)
        {
            transform.position = pos;
            transform.rotation = Quaternion.identity;
        }
    }


    public void SetSpawnPosition(Vector3 pos)
    {
        if (IsServer)
        {
            spawnPosition.Value = pos;
            Debug.Log($"[Server] Spawn position set to {pos}");
        }
    }
}
