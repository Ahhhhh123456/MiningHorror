using Unity.Netcode;
using UnityEngine;

public class DrillSpawner : NetworkBehaviour
{
    public NetworkObject bodyPrefab;
    public NetworkObject headPrefab;
    public NetworkObject wheelPrefab;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            SpawnDrill(new Vector3(0, 5, 0), Quaternion.identity);
        }
    }


    public void SpawnDrill(Vector3 position, Quaternion rotation)
    {
        if (!IsServer) return; // Only spawn on server

        // 1. Spawn the body (root)
        NetworkObject bodyInstance = Instantiate(bodyPrefab, position, rotation);
        bodyInstance.Spawn();

        // 2. Spawn the head and attach to body
        NetworkObject headInstance = Instantiate(headPrefab, position + Vector3.up * 1.5f, rotation);
        headInstance.Spawn(true); // true = spawn as child of body?
        headInstance.transform.parent = bodyInstance.transform;

        // 3. Spawn two wheels and attach
        Vector3 wheelOffset1 = new Vector3(1f, 0, 0); // adjust to fit model
        Vector3 wheelOffset2 = new Vector3(-1f, 0, 0);

        NetworkObject wheel1 = Instantiate(wheelPrefab, position + wheelOffset1, rotation);
        wheel1.Spawn();
        wheel1.transform.parent = bodyInstance.transform;

        NetworkObject wheel2 = Instantiate(wheelPrefab, position + wheelOffset2, rotation);
        wheel2.Spawn();
        wheel2.transform.parent = bodyInstance.transform;

        Debug.Log("Drill spawned with all parts!");
    }
}
