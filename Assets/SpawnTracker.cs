using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine;

public class SpawnTracker : MonoBehaviour
{
    public static SpawnTracker Instance;

    private List<NetworkObject> trackedObjects = new List<NetworkObject>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // persists across scene loads
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Register(NetworkObject netObj)
    {
        if (netObj != null && !trackedObjects.Contains(netObj))
            trackedObjects.Add(netObj);
    }

    public void DespawnAll()
    {
        foreach (var obj in trackedObjects)
        {
            if (obj != null && obj.IsSpawned)
                obj.Despawn(true); // Destroy across network
        }
        trackedObjects.Clear();
        Debug.Log("[SpawnTracker] Cleared all tracked objects.");
    }
}
