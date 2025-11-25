using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class PlayerSpawn : NetworkBehaviour
{
    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.SceneManager.OnLoadEventCompleted += OnSceneLoaded;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;
    }

    private void OnSceneLoaded(string sceneName, LoadSceneMode mode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!IsServer) return;

        Vector3 spawnPos = SceneSpawnPoint.Instance.spawnLocation.position;
        Quaternion spawnRot = SceneSpawnPoint.Instance.spawnLocation.rotation;

        transform.SetPositionAndRotation(spawnPos, spawnRot); // server
        MovePlayerClientRpc(spawnPos, spawnRot); // clients
    }

    [ClientRpc]
    private void MovePlayerClientRpc(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
    }
}
