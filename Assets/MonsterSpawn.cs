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
        yield return new WaitForSeconds(1f);

        var map = caveGenerator.densityMap;
        if (map == null) yield break;

        int width = caveGenerator.caveWidth;
        int height = caveGenerator.caveHeight;
        int depth = caveGenerator.caveDepth;
        float iso = caveGenerator.isoLevel;
        float res = caveGenerator.resolution;

        const int step = 2;
        const int yieldEvery = 4000;
        int checks = 0;

        for (int x = 1; x < width - 1; x += step)
        for (int y = 1; y < height - 1; y += step)
        for (int z = 1; z < depth - 1; z += step)
        {
            checks++;
            if (checks % yieldEvery == 0)
                yield return null;

            if (map[x, y, z] <= iso) continue;

            bool surface =
                map[x + 1, y, z] <= iso ||
                map[x - 1, y, z] <= iso ||
                map[x, y + 1, z] <= iso ||
                map[x, y - 1, z] <= iso ||
                map[x, y, z + 1] <= iso ||
                map[x, y, z - 1] <= iso;

            if (!surface || Random.value > spawnChance)
                continue;

            Vector3 pos = new Vector3(x + 0.5f, y + 0.6f, z + 0.5f) * res;
            pos += Random.insideUnitSphere * spawnOffset * res;

            if (!NavMesh.SamplePosition(pos, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
                continue;

            if (!IsServer) yield break;

            NetworkObject obj = Instantiate(monsterPrefab, hit.position, Quaternion.identity)
                                .GetComponent<NetworkObject>();

            obj.Spawn();
            SpawnTracker.Instance.Register(obj);

            yield return null;
        }
    }


}
