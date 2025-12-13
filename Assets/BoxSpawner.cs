using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode; 
using Unity.AI.Navigation;

public class BoxSpawner : NetworkBehaviour
{
    [Header("Prefabs")]
    public GameObject box1;
    public GameObject box2;
    public GameObject box3And4;

    public DrillBoxData bodyData;
    public DrillBoxData headData;
    public DrillBoxData wheelData;

    private MarchingCubes caveGenerator;

    private bool boxesSpawned = false;

    void Awake()
    {
        caveGenerator = GetComponent<MarchingCubes>();
        if (caveGenerator == null)
            Debug.LogError("MarchingCubes component missing!");
    }
    
    public IEnumerator SpawnBoxesOnSurface()
    {
        if (!IsServer || boxesSpawned)
            yield break;

        boxesSpawned = true;

        if (caveGenerator.densityMap == null)
            yield break;

        GameObject[] boxes = { box1, box2, box3And4, box3And4 };
        List<Vector3> surfacePoints = new List<Vector3>(boxes.Length);

        var density = caveGenerator.densityMap;
        int width = caveGenerator.caveWidth;
        int height = caveGenerator.caveHeight;
        int depth = caveGenerator.caveDepth;

        float iso = caveGenerator.isoLevel;
        float res = caveGenerator.resolution;

        const int step = 3;
        const int yieldEvery = 3000;
        int checks = 0;

        for (int x = 1; x < width - 1; x += step)
        for (int y = 1; y < height - 3; y += step)
        for (int z = 1; z < depth - 1; z += step)
        {
            checks++;
            if (checks % yieldEvery == 0)
                yield return null;

            if (density[x, y, z] <= iso) continue;
            if (density[x, y + 1, z] > iso) continue;
            if (density[x, y + 2, z] > iso) continue;

            Vector3 approx = new Vector3(x + 0.5f, y + 3f, z + 0.5f) * res;

            if (!Physics.Raycast(approx, Vector3.down, out RaycastHit hit, 10f))
                continue;

            if (Vector3.Angle(hit.normal, Vector3.up) > 35f)
                continue;

            surfacePoints.Add(hit.point + Vector3.up * 0.1f);

            if (surfacePoints.Count >= boxes.Length)
                goto SPAWN;
        }

    SPAWN:
        if (surfacePoints.Count < boxes.Length)
            yield break;

        for (int i = 0; i < boxes.Length; i++)
        {
            NetworkObject box = Instantiate(boxes[i], surfacePoints[i], Quaternion.identity)
                                .GetComponent<NetworkObject>();

            var mod = box.GetComponent<NavMeshModifier>() ??
                    box.gameObject.AddComponent<NavMeshModifier>();
            mod.ignoreFromBuild = true;

            box.Spawn();
            SpawnTracker.Instance.Register(box);

            var data = box.GetComponent<NetworkedBoxData>();
            if (boxes[i] == box1) data.InitializeFromDrillBoxData(bodyData);
            else if (boxes[i] == box2) data.InitializeFromDrillBoxData(headData);
            else data.InitializeFromDrillBoxData(wheelData);

            yield return null;
        }
    }


}
