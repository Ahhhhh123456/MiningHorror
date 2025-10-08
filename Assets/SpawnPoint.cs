using Unity.Netcode;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        // Wait a frame to ensure the player object has spawned
        StartCoroutine(SetSpawnPositionNextFrame(clientId));
    }

    private System.Collections.IEnumerator SetSpawnPositionNextFrame(ulong clientId)
    {
        yield return null; // wait 1 frame

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            yield break;

        var playerObject = client.PlayerObject;
        if (playerObject != null)
        {
            playerObject.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        }
    }
}
