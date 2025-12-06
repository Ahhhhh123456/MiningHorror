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
    public GameObject box3And4; // shared prefab for box3 and box4

    public DrillBoxData bodyData;

    public DrillBoxData headData;

    public DrillBoxData wheelData;

    private MarchingCubes caveGenerator;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Called In CreateCave (MarchingCubes.cs)
        //StartCoroutine(SpawnBoxesOnSurface());
        
    }

    void Awake()
    {
        // Get the MarchingCubes component on the same object
        caveGenerator = GetComponent<MarchingCubes>();
        if (caveGenerator == null)
        {
            Debug.LogError("MarchingCubes component not found on this GameObject!");
            return;
        }
    }
    public IEnumerator SpawnBoxesOnSurface()
    {
        if (caveGenerator.densityMap == null)
        {
            Debug.LogWarning("Density map not generated yet.");
            yield break;
        }

        GameObject[] boxes = new GameObject[] { box1, box2, box3And4, box3And4 };
        List<Vector3> floorPositions = new List<Vector3>();

        int width = caveGenerator.caveWidth;
        int height = caveGenerator.caveHeight;
        int depth = caveGenerator.caveDepth;
        float iso = caveGenerator.isoLevel;
        float res = caveGenerator.resolution;

        float[,,] density = caveGenerator.densityMap;

        // --- Scan only for walkable floor ---
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 3; y++)   // leave space for headroom
            {
                for (int z = 1; z < depth - 1; z++)
                {
                    if (!IsWalkableFloor(density, x, y, z, iso))
                        continue;

                    // The box should sit ON the floor, not inside it
                    Vector3 pos = new Vector3(
                        x + 0.5f, 
                        y + 1.05f,    // slightly above floor
                        z + 0.5f
                    ) * res;

                    floorPositions.Add(pos);
                }
            }
        }

        if (floorPositions.Count < boxes.Length)
        {
            Debug.LogWarning("[BoxGen] Not enough walkable floor positions.");
            yield break;
        }

        // Shuffle positions
        for (int i = 0; i < floorPositions.Count; i++)
        {
            int rand = Random.Range(i, floorPositions.Count);
            (floorPositions[i], floorPositions[rand]) = (floorPositions[rand], floorPositions[i]);
        }

        // Spawn boxes
        for (int i = 0; i < boxes.Length; i++)
        {
            Vector3 spawnPos = floorPositions[i];
            GameObject prefab = boxes[i];

            NetworkObject boxInstance = Instantiate(prefab, spawnPos, Quaternion.identity)
                                        .GetComponent<NetworkObject>();

            // NavMesh ignore
            NavMeshModifier mod = boxInstance.GetComponent<NavMeshModifier>();
            if (mod == null) 
                mod = boxInstance.gameObject.AddComponent<NavMeshModifier>();
            mod.ignoreFromBuild = true;

            boxInstance.Spawn();

            // Initialize properties
            NetworkedBoxData netData = boxInstance.GetComponent<NetworkedBoxData>();
            if (prefab == box1)
                netData.InitializeFromDrillBoxData(bodyData);
            else if (prefab == box2)
                netData.InitializeFromDrillBoxData(headData);
            else
                netData.InitializeFromDrillBoxData(wheelData);

            boxInstance.name = prefab.name;

            yield return null;
        }

        Debug.Log("[BoxGen] Finished spawning boxes on walkable cave floor.");
    }



    private bool IsWalkableFloor(float[,,] density, int x, int y, int z, float iso)
    {
        // Must be solid at this voxel = floor
        if (density[x, y, z] <= iso)
            return false;

        // Must be air above = standable
        if (density[x, y + 1, z] > iso)
            return false;

        // Head clearance (optional but recommended)
        if (density[x, y + 2, z] > iso)
            return false;

        return true;
    }



}
