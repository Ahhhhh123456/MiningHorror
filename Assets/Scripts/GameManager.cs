using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Netcode;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public TextMeshProUGUI coalCountText;
    public int coalCollected = 50;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            StartCoroutine(WaitForNetworkManager());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator WaitForNetworkManager()
    {
        // Wait until NetworkManager exists
        while (NetworkManager.Singleton == null)
            yield return null;

        // Wait until SceneManager exists
        while (NetworkManager.Singleton.SceneManager == null)
            yield return null;

        // Subscribe once
        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnNetworkSceneLoaded;
    }

    private void OnNetworkSceneLoaded(ulong clientId, string sceneName, LoadSceneMode mode)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        Debug.Log($"[SERVER] Network scene fully loaded: {sceneName}");

        SpawnTracker.Instance?.DespawnAll();

        if (sceneName == "BradyTestScene")
        {
            TeleportAllPlayersToLobby();
        }
    }

    private void TeleportAllPlayersToLobby()
    {
        GameObject spawnObj = GameObject.Find("LobbySpawnPoint");
        if (spawnObj == null)
        {
            Debug.LogError("LobbySpawnPoint not found!");
            return;
        }

        Vector3 pos = spawnObj.transform.position;
        Quaternion rot = spawnObj.transform.rotation;

        TeleportAllPlayersToLobbyClientRpc(pos, rot);
    }


    [ClientRpc]
    private void TeleportAllPlayersToLobbyClientRpc(Vector3 pos, Quaternion rot, ClientRpcParams rpcParams = default)
    {
        var playerObj = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (playerObj == null) return;

        playerObj.transform.SetPositionAndRotation(pos, rot);

        Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} teleported to lobby at position {pos}");
        var rb = playerObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void CollectCoal(int amount)
    {
        coalCollected += amount;

        if (coalCountText != null)
            coalCountText.text = "Fuel: " + coalCollected;
    }
}
