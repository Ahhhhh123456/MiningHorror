using UnityEngine;
using Unity.Netcode;
using Steamworks;
using Netcode.Transports;
using System.Collections;

public class SteamLobbyManager : MonoBehaviour
{
    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;

    private const string HOST_STEAMID_KEY = "HostAddress";
    private SteamNetworkingSocketsTransport steamTransport;

    void Start()
    {
        // Grab the Steam transport from the NetworkManager
        steamTransport = NetworkManager.Singleton.GetComponent<SteamNetworkingSocketsTransport>();

        // Hook up Steamworks callbacks
        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);

        // Optional: NGO connection debug logging
        NetworkManager.Singleton.OnClientConnectedCallback += (id) =>
        {
            Debug.Log($"Client connected: {id}");
        };
        NetworkManager.Singleton.OnClientDisconnectCallback += (id) =>
        {
            Debug.Log($"Client disconnected: {id}");
        };

        // Initialize Steam relay (safe to call multiple times)
        try
        {
            SteamNetworkingUtils.InitRelayNetworkAccess();
            Debug.Log("Steam relay network initialized.");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Steam relay initialization failed (probably already active): " + e.Message);
        }
    }

    void Update()
    {
        // Press H to create a Steam lobby (host)
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (!NetworkManager.Singleton.IsListening)
            {
                Debug.Log("Creating Steam Lobby...");
                SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, 2); // Max 2 players
            }
            else
            {
                Debug.LogWarning("NetworkManager is already running!");
            }
        }
    }

    // Called when host successfully creates a lobby
    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError("Failed to create Steam lobby!");
            return;
        }

        CSteamID lobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        Debug.Log($"Lobby created successfully! Lobby ID: {lobbyId}");

        // Store host SteamID in the lobby metadata
        SteamMatchmaking.SetLobbyData(lobbyId, HOST_STEAMID_KEY, SteamUser.GetSteamID().ToString());

        // Start hosting via NGO
        NetworkManager.Singleton.StartHost();
        Debug.Log("Started host session.");
    }

    // Triggered when someone receives a Steam "Join Game" request
    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        Debug.Log($"Join request received. Joining lobby {callback.m_steamIDLobby}...");
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    // Called when client actually enters the Steam lobby
    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        CSteamID lobbyId = new CSteamID(callback.m_ulSteamIDLobby);

        // If we're the host, skip client connection
        if (NetworkManager.Singleton.IsHost)
        {
            Debug.Log("Host entered their own lobby.");
            return;
        }

        // Get host SteamID from lobby data
        string hostAddress = SteamMatchmaking.GetLobbyData(lobbyId, HOST_STEAMID_KEY);
        if (string.IsNullOrEmpty(hostAddress))
        {
            Debug.LogError("No host SteamID found in the lobby metadata!");
            return;
        }

        ulong hostSteamId = ulong.Parse(hostAddress);
        Debug.Log($"Client entered lobby. Preparing to connect to host SteamID: {hostSteamId}");

        // Start client after Steam P2P initializes
        StartCoroutine(WaitForSteamConnectionAndStartClient(hostSteamId));
    }

    private IEnumerator WaitForSteamConnectionAndStartClient(ulong hostSteamId)
    {
        // Ensure transport is assigned
        if (steamTransport == null)
        {
            steamTransport = NetworkManager.Singleton.GetComponent<SteamNetworkingSocketsTransport>();
            if (steamTransport == null)
            {
                Debug.LogError("Steam transport not found on NetworkManager!");
                yield break;
            }
        }

        // Set host SteamID for the transport
        steamTransport.ConnectToSteamID = hostSteamId;

        // Wait a few frames to allow Steam P2P to initialize
        float waitTime = 0.5f;
        float timer = 0f;
        while (timer < waitTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        // Track client connection
        bool connected = false;
        void OnConnected(ulong clientId)
        {
            if (NetworkManager.Singleton.IsClient)
            {
                connected = true;
                Debug.Log($"NGO OnClientConnectedCallback fired, clientId: {clientId}");
            }
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnConnected;

        // Start the client
        bool started = NetworkManager.Singleton.StartClient();
        Debug.Log($"NetworkManager.StartClient() returned: {started}");
        if (!started)
        {
            Debug.LogError("NGO StartClient() failed immediately. Steam transport may not be ready or NAT blocked.");
        }

        // Wait for connection or timeout
        float timeout = 8f;
        timer = 0f;
        while (!connected && timer < timeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        NetworkManager.Singleton.OnClientConnectedCallback -= OnConnected;

        if (connected)
        {
            Debug.Log("Client successfully connected to host via NGO.");
        }
        else
        {
            Debug.LogError("Client failed to connect within timeout. Check Steam P2P connectivity, AppID, firewall, or NAT.");
        }
    }
}



// public class SteamLobbyManager : MonoBehaviour
// {
//     protected Callback<LobbyCreated_t> lobbyCreated;
//     protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
//     protected Callback<LobbyEnter_t> lobbyEntered;

//     private const string HOST_STEAMID_KEY = "HostAddress";
//     private SteamNetworkingSocketsTransport steamTransport;

//     void Start()
//     {
//         // Grab the Steam transport on the NetworkManager
//         steamTransport = NetworkManager.Singleton.GetComponent<SteamNetworkingSocketsTransport>();

//         // Hook up the Steamworks callbacks
//         if (lobbyCreated == null)
//             lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);

//         if (gameLobbyJoinRequested == null)
//             gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);

//         if (lobbyEntered == null)
//             lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
//     }

//     void Update()
//     {
//         if (Input.GetKeyDown(KeyCode.H))
//         {
//             Debug.Log("Creating Steam Lobby...");
//             SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, 2); // 2 = max players. Change as needed
//         }
//     }

//     // Called when the host successfully creates a lobby
//     private void OnLobbyCreated(LobbyCreated_t callback)
//     {
//         if (callback.m_eResult != EResult.k_EResultOK)
//         {
//             Debug.LogError("Failed to create lobby!");
//             return;
//         }

//         Debug.Log("Lobby created. Starting host...");

//         // Store host SteamID in the lobby metadata
//         CSteamID lobbyId = new CSteamID(callback.m_ulSteamIDLobby);
//         SteamMatchmaking.SetLobbyData(lobbyId, HOST_STEAMID_KEY, SteamUser.GetSteamID().ToString());

//         // Start hosting
//         NetworkManager.Singleton.StartHost();
//     }

//     // Triggered when someone receives a Steam "Join Game" request
//     private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
//     {
//         Debug.Log("Join request received. Entering lobby...");
//         SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
//     }

//     // Called when the client actually enters the lobby
//     private void OnLobbyEntered(LobbyEnter_t callback)
//     {
//         CSteamID lobbyId = new CSteamID(callback.m_ulSteamIDLobby);

//         // If we're the host, we don’t need to connect
//         if (NetworkManager.Singleton.IsHost) return;

//         // Fetch the host SteamID from lobby data
//         string hostAddress = SteamMatchmaking.GetLobbyData(lobbyId, HOST_STEAMID_KEY);

//         if (string.IsNullOrEmpty(hostAddress))
//         {
//             Debug.LogError("No host SteamID found in lobby!");
//             return;
//         }

//         // Set the transport to connect to the host
//         steamTransport.ConnectToSteamID = ulong.Parse(hostAddress);
//         Debug.Log("Client joined lobby. Connecting to host SteamID: " + hostAddress);

//         // Start the client
//         NetworkManager.Singleton.StartClient();
//     }
// }
