// using UnityEngine;
// using Unity.Netcode;
// using Steamworks;

// public class SteamMultiplayerManager : MonoBehaviour
// {
//     private SteamNetworkingSocketsTransport transport;

//     void Awake()
//     {
//         transport = NetworkManager.Singleton.GetComponent<SteamNetworkingSocketsTransport>();

//         // Set the local Steam ID for P2P
//         transport.localSteamId = SteamUser.GetSteamID();
//         Debug.Log("Local Steam ID set for transport: " + transport.localSteamId);
//     }

//     public void StartHost()
//     {
//         NetworkManager.Singleton.StartHost();
//     }

//     public void StartClient()
//     {
//         NetworkManager.Singleton.StartClient();
//     }
// }




















// using UnityEngine;
// using Unity.Netcode;
// using Steamworks;
// using Steamworks.Data;

// public class SteamMultiplayerManager : MonoBehaviour
// {
//     [Header("Networked Prefabs")]
//     [SerializeField] private NetworkObject playerPrefab; // assign your player prefab here

//     private Lobby? currentLobby;

//     void Update()
//     {
//         // Press H to start host
//         if (Input.GetKeyDown(KeyCode.H))
//         {
//             StartHost();
//         }

//         // Press C to start client
//         if (Input.GetKeyDown(KeyCode.C))
//         {
//             if (currentLobby.HasValue)
//             {
//                 StartClient(currentLobby.Value.Id);
//             }
//             else
//             {
//                 Debug.LogWarning("No lobby available to join! Host must create one first.");
//             }
//         }
//     }

//     /// <summary>
//     /// Host: Create lobby and start NGO host
//     /// </summary>
//     public async void StartHost()
//     {
//         // Create Steam lobby
//         currentLobby = await SteamMatchmaking.CreateLobbyAsync(4); // max 4 players
//         if (!currentLobby.HasValue)
//         {
//             Debug.LogError("Failed to create Steam lobby.");
//             return;
//         }

//         Debug.Log("Steam Lobby Created: " + currentLobby.Value.Id);

//         // Start NGO host
//         NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
//         NetworkManager.Singleton.StartHost();

//         // Spawn host player
//         SpawnPlayer(NetworkManager.Singleton.LocalClientId);
//     }

//     /// <summary>
//     /// Client: Join lobby and start NGO client
//     /// </summary>
//     public async void StartClient(SteamId lobbyId)
//     {
//         await SteamMatchmaking.JoinLobbyAsync(lobbyId);
//         Debug.Log("Joined lobby: " + lobbyId);

//         NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
//         NetworkManager.Singleton.StartClient();
//     }

//     /// <summary>
//     /// Callback when a client connects to spawn their player
//     /// </summary>
//     private void OnClientConnected(ulong clientId)
//     {
//         SpawnPlayer(clientId);
//     }

//     /// <summary>
//     /// Spawn a networked player prefab for a client
//     /// </summary>
//     private void SpawnPlayer(ulong clientId)
//     {
//         if (playerPrefab == null)
//         {
//             Debug.LogError("Player prefab not assigned!");
//             return;
//         }

//         // Only the host should instantiate and spawn NetworkObjects
//         if (NetworkManager.Singleton.IsHost)
//         {
//             var player = Instantiate(playerPrefab);
//             player.SpawnAsPlayerObject(clientId, true);
//             Debug.Log($"Spawned player for client {clientId}");
//         }
//     }
// }
