using UnityEngine;
using Unity.Netcode;
using Steamworks;
using Netcode.Transports; // namespace of SteamNetworkingSocketsTransport

public class SteamNetworkStart : MonoBehaviour
{
    private SteamNetworkingSocketsTransport steamTransport;

    // Replace with the host's Steam64ID when connecting as client
    public ulong hostSteamId;

    void Start()
    {
        steamTransport = NetworkManager.Singleton.GetComponent<SteamNetworkingSocketsTransport>();
        // Assign local Steam ID automatically
        steamTransport.ConnectToSteamID = SteamUser.GetSteamID().m_SteamID;
        Debug.Log("Local SteamID: " + steamTransport.ConnectToSteamID);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("Starting Host...");
            NetworkManager.Singleton.StartHost();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            if (hostSteamId == 0)
            {
                Debug.LogError("Set hostSteamId before starting client!");
                return;
            }

            steamTransport.ConnectToSteamID = hostSteamId;
            Debug.Log("Connecting to host SteamID: " + hostSteamId);
            NetworkManager.Singleton.StartClient();
        }
    }
}
