using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode; // if using NetworkObject

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
        if (IsServer)
        {
            // Clients also generate locally so they can see geometry
            StartCoroutine(SpawnBoxesOnSurface());
        }
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
        // Make sure density map exists
        if (caveGenerator.densityMap == null)
        {
            Debug.LogWarning("Density map not generated yet.");
            yield break;
        }

        GameObject[] boxes = new GameObject[] { box1, box2, box3And4, box3And4 };
        List<Vector3> surfacePositions = new List<Vector3>();

        int width = caveGenerator.caveWidth;
        int height = caveGenerator.caveHeight;
        int depth = caveGenerator.caveDepth;
        float iso = caveGenerator.isoLevel;
        float res = caveGenerator.resolution;
        float[,,] density = caveGenerator.densityMap;

        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                for (int z = 1; z < depth - 1; z++)
                {
                    float val = density[x, y, z];
                    if (val > iso) continue; // skip solid

                    bool adjacentToSolid = false;
                    for (int dx = -1; dx <= 1 && !adjacentToSolid; dx++)
                        for (int dy = -1; dy <= 1 && !adjacentToSolid; dy++)
                            for (int dz = -1; dz <= 1 && !adjacentToSolid; dz++)
                            {
                                if (dx == 0 && dy == 0 && dz == 0) continue;
                                int nx = x + dx, ny = y + dy, nz = z + dz;
                                if (density[nx, ny, nz] > iso)
                                {
                                    adjacentToSolid = true;
                                }
                            }

                    if (adjacentToSolid)
                    {
                        // Spawn point is in the air voxel just outside the cave
                        Vector3 pos = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f) * res;
                        surfacePositions.Add(pos);
                    }
                }
            }
        }

        if (surfacePositions.Count < boxes.Length)
        {
            Debug.LogWarning("Not enough surface positions for all boxes!");
            yield break;
        }

        // Shuffle surface positions
        for (int i = 0; i < surfacePositions.Count; i++)
        {
            int randIndex = Random.Range(i, surfacePositions.Count);
            Vector3 temp = surfacePositions[i];
            surfacePositions[i] = surfacePositions[randIndex];
            surfacePositions[randIndex] = temp;
        }

        // Spawn each box (server only)
        for (int i = 0; i < boxes.Length; i++)
        {
            Vector3 spawnPos = surfacePositions[i];
            GameObject chosenBox = boxes[i];

            NetworkObject boxInstance = Instantiate(chosenBox, spawnPos, Quaternion.identity)
                                        .GetComponent<NetworkObject>();

            // Spawn it on the network
            boxInstance.Spawn();

            // Now initialize NetworkVariables safely (server only)
            NetworkedBoxData netData = boxInstance.GetComponent<NetworkedBoxData>();

            if (chosenBox == box1)          // body prefab
                netData.InitializeFromDrillBoxData(bodyData);
            else if (chosenBox == box2)     // head prefab
                netData.InitializeFromDrillBoxData(headData);
            else if (chosenBox == box3And4) // wheel prefab
                netData.InitializeFromDrillBoxData(wheelData);

            boxInstance.name = chosenBox.name;

            yield return null;
        }

        Debug.Log("[BoxGen] Finished spawning boxes on cave surface.");
    }



}
