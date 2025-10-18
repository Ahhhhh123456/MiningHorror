using UnityEngine;
using Unity.Netcode;
using Steamworks;
using Netcode.Transports;
using System.Collections;
using System;

public class SteamLobbyManager : MonoBehaviour
{
    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;

    private const string HOST_STEAMID_KEY = "HostAddress";
    private SteamNetworkingSocketsTransport steamTransport;

    void Start()
    {
        // Ensure Steam relay access is ready before anything else
        try
        {
            SteamNetworkingUtils.InitRelayNetworkAccess();
            Debug.Log("Steam relay network initialized.");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Steam relay initialization failed (probably already active): " + e.Message);
        }

        // Grab the Steam transport from the NetworkManager
        steamTransport = NetworkManager.Singleton.GetComponent<SteamNetworkingSocketsTransport>();

        // Hook up Steamworks callbacks
        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);

        // Optional NGO connection debug logging
        NetworkManager.Singleton.OnClientConnectedCallback += (id) =>
        {
            Debug.Log($"Client connected: {id}");
        };
        NetworkManager.Singleton.OnClientDisconnectCallback += (id) =>
        {
            Debug.Log($"Client disconnected: {id}");
        };
    }

    void Update()
    {
        // Press H to create a Steam lobby
        if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("Creating Steam Lobby...");
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, 3); // max 3 players for test
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
        Debug.Log($"Join request received. Entering lobby {callback.m_steamIDLobby}...");
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    // Called when client actually enters the Steam lobby
    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        CSteamID lobbyId = new CSteamID(callback.m_ulSteamIDLobby);

        // If we're the host, we're already running
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
        Debug.Log("Preparing Steam transport connect & starting NGO client...");

        // Ensure transport knows who to connect to
        if (steamTransport == null)
        {
            steamTransport = NetworkManager.Singleton.GetComponent<SteamNetworkingSocketsTransport>();
            if (steamTransport == null)
            {
                Debug.LogError("Steam transport not found on NetworkManager!");
                yield break;
            }
        }

        steamTransport.ConnectToSteamID = hostSteamId;

        // Try to init relay access (harmless if already initted)
        try { SteamNetworkingUtils.InitRelayNetworkAccess(); }
        catch (Exception e) { Debug.LogWarning("InitRelayNetworkAccess: " + e.Message); }

        // Local flag set by the NGO callback below
        bool connected = false;
        void OnConnected(ulong clientId)
        {
            // if this client was connected to a server, the callback will be called on the host too,
            // but checking NetworkManager.Singleton.IsClient helps ensure this is the client side connection.
            if (NetworkManager.Singleton.IsClient)
            {
                connected = true;
                Debug.Log($"NGO OnClientConnectedCallback fired, clientId: {clientId}");
            }
        }

        // Register temporary callback
        NetworkManager.Singleton.OnClientConnectedCallback += OnConnected;

        // Attempt to start the client. StartClient may return false in some error cases.
        bool started = NetworkManager.Singleton.StartClient();
        Debug.Log($"NetworkManager.StartClient() returned: {started}");

        // Wait for connection (timeout)
        float timeout = 6f; // seconds
        float timer = 0f;
        while (timer < timeout && !connected)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        // Cleanup callback subscription
        NetworkManager.Singleton.OnClientConnectedCallback -= OnConnected;

        if (connected)
        {
            Debug.Log("Client successfully connected to host via NGO.");
        }
        else
        {
            Debug.LogError("Client failed to connect within timeout. Check Steam P2P connectivity / AppID / NAT.");
            // you can decide to retry start client here or show UI etc.
        }
    }
}
