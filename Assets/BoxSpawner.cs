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

    void Awake()
    {
        caveGenerator = GetComponent<MarchingCubes>();
        if (caveGenerator == null)
            Debug.LogError("MarchingCubes component missing!");
    }

    public IEnumerator SpawnBoxesOnSurface()
    {
        if (caveGenerator.densityMap == null)
        {
            Debug.LogWarning("Density map not generated yet.");
            yield break;
        }

        GameObject[] boxes = new GameObject[] { box1, box2, box3And4, box3And4 };
        List<Vector3> surfacePoints = new List<Vector3>();

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

                    Vector3 approx = new Vector3(
                        x + 0.5f,
                        y + 3f,
                        z + 0.5f
                    ) * res;

                    if (Physics.Raycast(approx, Vector3.down, out RaycastHit hit, 10f))
                    {
                        if (Vector3.Angle(hit.normal, Vector3.up) < 35f)
                        {
                            surfacePoints.Add(hit.point + Vector3.up * 0.1f);
                        }
                    }
                }
            }
        }

        if (surfacePoints.Count < boxes.Length)
        {
            Debug.LogWarning("Not enough valid floor positions for boxes.");
            yield break;
        }

        // Shuffle
        for (int i = 0; i < surfacePoints.Count; i++)
        {
            int rand = Random.Range(i, surfacePoints.Count);
            (surfacePoints[i], surfacePoints[rand]) = (surfacePoints[rand], surfacePoints[i]);
        }

        // Spawn boxes
        for (int i = 0; i < boxes.Length; i++)
        {
            Vector3 spawnPos = surfacePoints[i];
            GameObject prefab = boxes[i];

            NetworkObject boxInstance = Instantiate(prefab, spawnPos, Quaternion.identity)
                                        .GetComponent<NetworkObject>();

            NavMeshModifier mod = boxInstance.GetComponent<NavMeshModifier>();
            if (mod == null)
                mod = boxInstance.gameObject.AddComponent<NavMeshModifier>();
            mod.ignoreFromBuild = true;

            boxInstance.Spawn();
            SpawnTracker.Instance.Register(boxInstance);

            NetworkedBoxData netData = boxInstance.GetComponent<NetworkedBoxData>();

            if (prefab == box1) netData.InitializeFromDrillBoxData(bodyData);
            else if (prefab == box2) netData.InitializeFromDrillBoxData(headData);
            else netData.InitializeFromDrillBoxData(wheelData);

            boxInstance.name = prefab.name;

            yield return null;
        }

        Debug.Log("Boxes spawned using raycast-grounded placement.");
    }
}
