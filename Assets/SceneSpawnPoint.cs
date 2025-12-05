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

    // Event to notify when the spawn point is ready
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

        List<Vector3> floorPositions = new List<Vector3>();
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
                    if (density[x, y, z] <= iso) continue;
                    if (density[x, y + 1, z] > iso) continue;
                    if (density[x, y + 2, z] > iso) continue;

                    floorPositions.Add(new Vector3(x + 0.5f, y + 1.05f, z + 0.5f) * res);
                }
            }
        }

        if (floorPositions.Count == 0)
        {
            Debug.LogWarning("No walkable positions found!");
            yield break;
        }

        Vector3 spawnPos = floorPositions[Random.Range(0, floorPositions.Count)];

        // Instantiate the prefab
        if (spawnPointPrefab != null)
        {
            GameObject obj = Instantiate(spawnPointPrefab, spawnPos, Quaternion.identity);
            obj.name = "PlayerSpawnPoint";
            latestSpawnPoint = obj.transform;

            // Invoke the event
            OnSpawnPointReady?.Invoke(spawnPos);
            Debug.Log($"Spawn point instantiated at {spawnPos}");
        }
        else
        {
            Debug.LogWarning("SpawnPointPrefab is not assigned!");
        }

        callback?.Invoke(spawnPos);
    }
}
