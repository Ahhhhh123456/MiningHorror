using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class NetworkStartTest : MonoBehaviour
{
    private UnityTransport transport;

    void Start()
    {
        transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.ConnectionData.Address = GetLocalIPAddress();
        transport.ConnectionData.Port = 7777;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            // Set host IP to its LAN IP so clients can connect
            //transport.ConnectionData.Address = GetLocalIPAddress();
            //transport.ConnectionData.Port = 7777;
            Debug.Log("Running Host. Ip host is: " + transport.ConnectionData.Address);

            Debug.Log($"Starting Host on {transport.ConnectionData.Address}:{transport.ConnectionData.Port}");
            NetworkManager.Singleton.StartHost();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            // Set client to connect to host IP (replace with actual host IP)
            Debug.Log("Running Client. Ip host is: " + transport.ConnectionData.Address);


            Debug.Log($"Starting Client connecting to {transport.ConnectionData.Address}:{transport.ConnectionData.Port}");
            NetworkManager.Singleton.StartClient();
        }
    }

    string GetLocalIPAddress()
    {
        var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                Debug.Log($"Found local IP address: {ip}");
                return ip.ToString();
            }
        }
        return null; // fallback
    }
}
