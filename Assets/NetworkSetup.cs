using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class NetworkSetup : MonoBehaviour
{
    private UnityTransport transport;

    void Start()
    {
        transport = GetComponent<UnityTransport>();

        // Host machine uses its LAN IP so clients can connect
        transport.ConnectionData.Address = GetLocalIPAddress();
        transport.ConnectionData.Port = 7777; // same port everywhere
        Debug.Log($"[NetworkSetup] Host listening on {transport.ConnectionData.Address}:{transport.ConnectionData.Port}");
    }

    string GetLocalIPAddress()
    {
        var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "127.0.0.1"; // fallback if no LAN IP found
    }
}
