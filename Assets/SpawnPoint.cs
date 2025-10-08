using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class SpawnPointManager : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;

    private void Start()
    {
        // Only the server needs to handle positioning
        if (!NetworkManager.Singleton.IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        // Also move host immediately
        SetHostSpawnPosition();
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void SetHostSpawnPosition()
    {
        if (NetworkManager.Singleton.IsServer && NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            NetworkManager.Singleton.LocalClient.PlayerObject.transform.SetPositionAndRotation(
                spawnPoint.position, spawnPoint.rotation);
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        StartCoroutine(SetClientSpawnPositionWhenReady(clientId));
    }

    private IEnumerator SetClientSpawnPositionWhenReady(ulong clientId)
    {
        NetworkClient client;

        // Wait until the player object exists
        while (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out client) || client.PlayerObject == null)
        {
            yield return null;
        }

        // Set the position on the server (replicates to client)
        client.PlayerObject.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
    }
}
