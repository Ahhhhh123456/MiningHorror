using UnityEngine;
using Steamworks;
using Unity.Netcode;

public class SteamTransportSetup : MonoBehaviour
{
    void Start()
    {
        var transport = NetworkManager.Singleton.GetComponent<Netcode.Transports.SteamNetworkingSocketsTransport>();
        transport.ConnectToSteamID = SteamUser.GetSteamID().m_SteamID;
        Debug.Log("SteamTransport ConnectToSteamID set to: " + transport.ConnectToSteamID);
    }
}
