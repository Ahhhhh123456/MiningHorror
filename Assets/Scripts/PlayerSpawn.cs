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
        // ✅ Only the server controls player spawn position
        if (!IsServer) return;

        if (SceneSpawnPoint.Instance != null)
        {
            transform.SetPositionAndRotation(
                SceneSpawnPoint.Instance.spawnLocation.position,
                SceneSpawnPoint.Instance.spawnLocation.rotation
            );
        }
    }
}
