using UnityEngine;
using Unity.Netcode;

public class EditorAutoHost : MonoBehaviour
{
    void Start()
    {
#if UNITY_EDITOR
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.StartHost();
        }
#endif
    }
}
