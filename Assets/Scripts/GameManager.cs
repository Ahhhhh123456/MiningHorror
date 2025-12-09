using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Netcode;

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

            // Subscribe to scene loaded event ONCE
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("Loaded scene: " + scene.name);

        // Despawn old cave objects
        SpawnTracker.Instance?.DespawnAll();

        // Check if we are loading the Lobby
        if (scene.name == "BradyTestScene") // replace with your lobby scene name
        {
            // Find the Spawn object in the scene
            GameObject spawnObj = GameObject.Find("LobbySpawnPoint");
            if (spawnObj == null)
            {
                Debug.LogWarning("No Spawn object found in the lobby scene!");
                return;
            }

            Vector3 spawnPos = spawnObj.transform.position;
            Quaternion spawnRot = spawnObj.transform.rotation;

            // Move all connected players to the spawn location
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                var playerObj = client.PlayerObject;
                if (playerObj != null)
                {
                    // Teleport the player
                    playerObj.transform.position = spawnPos;
                    playerObj.transform.rotation = spawnRot;

                    Debug.Log($"Teleported player {client.ClientId} to lobby spawn point. Position: {spawnPos}, Rotation: {spawnRot}");

                    // Optional: reset Rigidbody velocities
                    Rigidbody rb = playerObj.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                }
            }
        }
    }


    public void CollectCoal(int amount)
    {
        coalCollected += amount;

        if (coalCountText != null)
            coalCountText.text = "Fuel: " + coalCollected;
    }
}
