using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public class SceneSpawnPoint : NetworkBehaviour
{
    public static SceneSpawnPoint Instance;

    [Header("Spawn Point Prefab")]
    public GameObject spawnPointPrefab;

    private MarchingCubes caveGenerator;

    public Transform latestSpawnPoint { get; private set; }

    public static event System.Action<Vector3> OnSpawnPointReady;

    private void Awake()
    {
        Instance = this;
        caveGenerator = GetComponent<MarchingCubes>();
        if (caveGenerator == null)
            Debug.LogError("MarchingCubes component not found!");
    }

    public IEnumerator RandomSpawnLocation(System.Action<Vector3> callback)
    {
        if (caveGenerator.densityMap == null)
        {
            Debug.LogWarning("Density map not generated yet.");
            yield break;
        }

        List<Vector3> validSurfacePoints = new List<Vector3>();

        int width = caveGenerator.caveWidth;
        int height = caveGenerator.caveHeight;
        int depth = caveGenerator.caveDepth;
        float iso = caveGenerator.isoLevel;
        float res = caveGenerator.resolution;
        float[,,] density = caveGenerator.densityMap;

        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 3; y++)
            {
                for (int z = 1; z < depth - 1; z++)
                {
                    // Same walkable check
                    if (density[x, y, z] <= iso) continue;
                    if (density[x, y + 1, z] > iso) continue;
                    if (density[x, y + 2, z] > iso) continue;

                    // Approx rough position
                    Vector3 approx = new Vector3(
                        x + 0.5f,
                        y + 3f,          // start ray above expected floor
                        z + 0.5f
                    ) * res;

                    // Raycast down to the REAL mesh
                    if (Physics.Raycast(approx, Vector3.down, out RaycastHit hit, 10f))
                    {
                        if (Vector3.Angle(hit.normal, Vector3.up) < 35f) // slope check
                        {
                            validSurfacePoints.Add(hit.point + Vector3.up * 0.1f);
                        }
                    }
                }
            }
        }

        if (validSurfacePoints.Count == 0)
        {
            Debug.LogWarning("No valid raycast-grounded spawn positions found!");
            yield break;
        }

        Vector3 spawnPos = validSurfacePoints[Random.Range(0, validSurfacePoints.Count)];

        if (spawnPointPrefab != null)
        {
            GameObject obj = Instantiate(spawnPointPrefab, spawnPos, Quaternion.identity);
            obj.name = "SpawnPoint";
            latestSpawnPoint = obj.transform;

            NetworkObject netObj = obj.GetComponent<NetworkObject>();
            if (netObj != null && IsServer)
            {
                netObj.Spawn();
                SpawnTracker.Instance.Register(netObj);

                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    PlayerSpawn ps = client.PlayerObject.GetComponent<PlayerSpawn>();
                    if (ps != null)
                        ps.SetSpawnPosition(spawnPos);
                }
            }

            OnSpawnPointReady?.Invoke(spawnPos);
            Debug.Log($"Spawn point placed at {spawnPos}");
        }
    }
}
