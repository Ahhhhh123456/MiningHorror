using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class SnapManager : NetworkBehaviour
{
    [Header("Prefabs")]
    public GameObject completePrefab; // Drill (Whole)

    private SnapPoint[] snapPoints;

    public List<GameObject> snappedPieces = new List<GameObject>();

    public List<GameObject> blueprintPieces = new List<GameObject>();

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

        foreach (var sp in snapPoints)
        {
            if (!sp.isOccupied.Value)
            {
                Debug.Log("Snap point not occupied: " + sp.gameObject.name);
                return; // Not all snapped yet
            }
        }

        Debug.Log("All snap points occupied. Replacing with complete prefab.");
        // All snapped, replace prefab
        ReplaceWithComplete();
    }

    public void RegisterSnappedPiece(GameObject snappedObj, GameObject blueprintObj)
    {

        if (snappedObj != null && !snappedPieces.Contains(snappedObj))
            snappedPieces.Add(snappedObj);

        if (blueprintObj != null && !blueprintPieces.Contains(blueprintObj))
            blueprintPieces.Add(blueprintObj);

        Debug.Log("Registered snapped piece: " + snappedObj.name);
        Debug.Log("Registered blueprint piece: " + blueprintObj.name);
    }

    private void ReplaceWithComplete()
    {
        // Only server should do this
        if (!IsServer) return;

        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;

        // Destroy all snapped blueprint pieces
        foreach (GameObject piece in snappedPieces)
        {
            if (piece == null) continue;

            NetworkObject pieceNetObj = piece.GetComponent<NetworkObject>();
            if (pieceNetObj != null && pieceNetObj.IsSpawned)
            {
                Debug.Log("Despawning snapped piece: " + piece.name);
                pieceNetObj.Despawn();
            }
            else
            {
                Destroy(piece);
            }
        }
        snappedPieces.Clear();

        foreach (GameObject blueprint in blueprintPieces)
        {
            if (blueprint == null) continue;

            Debug.Log("Destroying blueprint piece: " + blueprint.name);
            Destroy(blueprint);
        }
        blueprintPieces.Clear();


        // Spawn the complete prefab
        GameObject newDrill = Instantiate(completePrefab, position, rotation);
        NetworkObject netObj = newDrill.GetComponent<NetworkObject>();
        netObj.Spawn();

        // Destroy this broken drill
        NetworkObject oldNetObj = GetComponent<NetworkObject>();
        oldNetObj.Despawn();
    }

}
