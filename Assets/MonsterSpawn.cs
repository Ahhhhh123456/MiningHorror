using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.AI;

public class MonsterSpawn : NetworkBehaviour
{
    [Header("Monster Prefab (NetworkObject)")]
    public GameObject monsterPrefab;

    private MarchingCubes caveGenerator;

    [Header("Spawn Settings")]
    public float spawnChance; // Lower = fewer monsters
    public float spawnOffset;  // Pushes monster slightly away from the wall

    void Awake()
    {
        caveGenerator = GetComponent<MarchingCubes>();
        if (caveGenerator == null)
        {
            Debug.LogError("MarchingCubes component not found on this GameObject!");
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Called In CreateCave (MarchingCubes.cs)
        //StartCoroutine(SpawnMonstersOnSurface());
        
    }

    public IEnumerator SpawnMonstersOnSurface()
    {
        // Ensure cave is ready
        yield return null; // wait one frame to ensure NavMesh is updated
        yield return new WaitForSeconds(1f);

        var map = caveGenerator.densityMap;
        if (map == null)
        {
            Debug.LogWarning("MonsterSpawn: Density map not ready.");
            yield break;
        }

        List<Vector3> spawnPositions = new List<Vector3>();

        for (int x = 1; x < caveGenerator.caveWidth; x++)
            for (int y = 1; y < caveGenerator.caveHeight; y++)
                for (int z = 1; z < caveGenerator.caveDepth; z++)
                {
                    float val = map[x, y, z];
                    if (val <= caveGenerator.isoLevel) continue; // Not solid = skip

                    // Check if near air (surface)
                    bool surface = false;
                    for (int dx = -1; dx <= 1 && !surface; dx++)
                        for (int dy = -1; dy <= 1 && !surface; dy++)
                            for (int dz = -1; dz <= 1 && !surface; dz++)
                            {
                                int nx = x + dx;
                                int ny = y + dy;
                                int nz = z + dz;

                                if (nx < 0 || ny < 0 || nz < 0 ||
                                    nx >= caveGenerator.caveWidth ||
                                    ny >= caveGenerator.caveHeight ||
                                    nz >= caveGenerator.caveDepth)
                                    continue;

                                if (map[nx, ny, nz] <= caveGenerator.isoLevel)
                                    surface = true;
                            }

                    if (!surface || Random.value > spawnChance)
                        continue;

                    Vector3 pos = new Vector3(x + 0.5f, y + 0.6f, z + 0.5f) * caveGenerator.resolution;

                    pos += Random.insideUnitSphere * spawnOffset * caveGenerator.resolution;

                    spawnPositions.Add(pos);
                }

        Debug.Log($"[MonsterSpawn] Found {spawnPositions.Count} surface spawn points.");

        // foreach (var pos in spawnPositions)
        // {
        //     GameObject obj = Instantiate(monsterPrefab, pos, Quaternion.identity);
        //     obj.GetComponent<NetworkObject>().Spawn();
        // }
        foreach (var pos in spawnPositions)
        {
            // Check if the position is valid on the NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(pos, out hit, 1f, NavMesh.AllAreas))
            {
                GameObject obj = Instantiate(monsterPrefab, hit.position, Quaternion.identity);
                obj.GetComponent<NetworkObject>().Spawn();
            }
            else
            {
                // Optional: skip or log if no valid NavMesh nearby
                Debug.LogWarning($"MonsterSpawn: No NavMesh found near {pos}");
            }
        }

        Debug.Log("[MonsterSpawn] Finished spawning monsters.");
    }
}
