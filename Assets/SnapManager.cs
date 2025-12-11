using Unity.Netcode;
using UnityEngine;

public class SnapManager : NetworkBehaviour
{
    [Header("Prefabs")]
    public GameObject completePrefab; // Drill (Whole)

    private SnapPoint[] snapPoints;

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

    private void ReplaceWithComplete()
    {
        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;

        // Spawn the complete prefab
        GameObject newDrill = Instantiate(completePrefab, position, rotation);
        NetworkObject netObj = newDrill.GetComponent<NetworkObject>();
        netObj.Spawn();

        // Destroy the broken drill
        NetworkObject oldNetObj = GetComponent<NetworkObject>();
        oldNetObj.Despawn();
    }
}
