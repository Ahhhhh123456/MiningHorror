using UnityEngine;
using Unity.Netcode;
using Steamworks;
using Netcode.Transports;

public class SteamLobbyManager : MonoBehaviour
{
    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;

    private const string HOST_STEAMID_KEY = "HostAddress";
    private SteamNetworkingSocketsTransport steamTransport;

    void Start()
    {
        // Grab the Steam transport on the NetworkManager
        steamTransport = NetworkManager.Singleton.GetComponent<SteamNetworkingSocketsTransport>();

        // Hook up the Steamworks callbacks
        if (lobbyCreated == null)
            lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        if (gameLobbyJoinRequested == null)
            gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        if (lobbyEntered == null)
            lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("Creating Steam Lobby...");
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, 2); 
            // 2 = max players. Change as needed
        }
    }

    // Called when the host successfully creates a lobby
    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError("Failed to create lobby!");
            return;
        }

        Debug.Log("Lobby created. Starting host...");

        // Store host SteamID in the lobby metadata
        CSteamID lobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        SteamMatchmaking.SetLobbyData(lobbyId, HOST_STEAMID_KEY, SteamUser.GetSteamID().ToString());

        // Start hosting
        NetworkManager.Singleton.StartHost();
    }

    // Triggered when someone receives a Steam "Join Game" request
    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        Debug.Log("Join request received. Entering lobby...");
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    // Called when the client actually enters the lobby
    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        CSteamID lobbyId = new CSteamID(callback.m_ulSteamIDLobby);

        // If we're the host, we don’t need to connect
        if (NetworkManager.Singleton.IsHost) return;

        // Fetch the host SteamID from lobby data
        string hostAddress = SteamMatchmaking.GetLobbyData(lobbyId, HOST_STEAMID_KEY);
        if (string.IsNullOrEmpty(hostAddress))
        {
            Debug.LogError("No host SteamID found in lobby!");
            return;
        }

        // Set the transport to connect to the host
        steamTransport.ConnectToSteamID = ulong.Parse(hostAddress);

        Debug.Log("Client joined lobby. Connecting to host SteamID: " + hostAddress);

        // Start the client
        NetworkManager.Singleton.StartClient();
    }
}
