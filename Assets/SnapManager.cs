using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;

public class SnapManager : NetworkBehaviour
{
    [Header("Prefabs")]
    public GameObject completePrefab; // Drill (Whole)

    private SnapPoint[] snapPoints;

    // Use NetworkList of NetworkObjectReference for networked syncing
    public NetworkList<NetworkObjectReference> snappedPieces = new NetworkList<NetworkObjectReference>();
    public NetworkList<NetworkObjectReference> blueprintPieces = new NetworkList<NetworkObjectReference>();

    void Awake()
    {
        snapPoints = GetComponentsInChildren<SnapPoint>();
        foreach (var sp in snapPoints)
        {
            sp.OnSnapChanged += CheckAllSnapped;
        }

        Debug.Log(snapPoints.Length + " snap points found in SnapManager.");
    }

    public void CheckAllSnapped()
    {
        if (!IsServer) return; // Only server replaces the drill

        foreach (var sp in snapPoints)
        {
            if (!sp.isOccupied.Value)
            {
                Debug.Log("Snap point not occupied: " + sp.gameObject.name);
                return; // Not all snapped yet
            }
        }

        Debug.Log("All snap points occupied. Replacing with complete prefab.");
        ReplaceWithComplete();
    }

    public void RegisterSnappedPiece(NetworkObject snappedObj)
    {
        if (snappedObj != null)
        {
            var netRef = new NetworkObjectReference(snappedObj);
            if (!snappedPieces.Contains(netRef))
                snappedPieces.Add(netRef);
        }
    }

    private void ReplaceWithComplete()
    {
        if (!IsServer) return;

        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;

        // Despawn all snapped pieces
        foreach (var netRef in snappedPieces)
        {
            if (netRef.TryGet(out NetworkObject netObj))
            {
                if (netObj.IsSpawned)
                {
                    //Debug.Log("Despawning snapped piece: " + netObj.name);
                    netObj.Despawn();
                }
            }
        }
        snappedPieces.Clear();

        // Spawn the complete prefab
        GameObject newDrill = Instantiate(completePrefab, position, rotation);
        NetworkObject newNetObj = newDrill.GetComponent<NetworkObject>();
        newNetObj.Spawn();

        // Despawn the broken drill
        NetworkObject oldNetObj = GetComponent<NetworkObject>();
        oldNetObj.Despawn();
    }
}
