using UnityEngine;
using Unity.Netcode;

public class NetworkStartTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("Starting Host");
            NetworkManager.Singleton.StartHost();
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("Starting Client");
            NetworkManager.Singleton.StartClient();
        }
    }
}
