using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Unity.AI.Navigation;
using System;
using UnityEngine.SceneManagement;

public class MarchingCubes : NetworkBehaviour
{
    [Header("Cave Settings")]
    public Material material;
    public int caveWidth;  // reduced for testing
    public int caveHeight;
    public int caveDepth;

    [Header("Floor Settings")]
    [Tooltip("Grid Y index of the floor baseline (0 is bottom).")]
    public int floorYGrid;           // which grid layer is the exact floor (0 usually)
    
    [Tooltip("How many grid layers above floorYGrid are forced/ blended solid for a smooth transition.")]
    public int floorBlendThickness;  // blend thickness in grid cells


    [Header("Noise Settings")]
    public float noiseScale;
    public float isoLevel;

    public float resolution;

    [Header("Chunk Settings")]
    public int chunkSizeX;
    public int chunkSizeY;
    public int chunkSizeZ;
    private Dictionary<Vector3Int, GameObject> chunks = new Dictionary<Vector3Int, GameObject>();

    private float lastMineTime = 0f;
    public float mineCooldown = 1.25f; 

    public GameObject caveParent;
    public NavMeshSurface surface; 
    [Header("Ore Settings")]
    public float oreChance;
    public GameObject[] orePrefabs; // Prefabs for ore instantiation
    public NetworkObject[] NetworkOrePrefabs; // Networked versions

    public GameObject meshysPrefab;

    public float[,,] densityMap;

    // Similar to MineType's holdCount
    private int holdCount = 0;

    public ParticleSystem mineParticlePrefab; 


    public static event Action OnCaveFinished;

    private void CaveFinished()
    {
        // Call this when mesh + navmesh is fully generated
        OnCaveFinished?.Invoke();
    }

    // For Local Testing Only
    // public override void OnNetworkSpawn()
    // {
    //     base.OnNetworkSpawn();

    //     if (IsServer)
    //     {
    //         noiseScale = UnityEngine.Random.Range(0.1f, 0.15f);
    //         isoLevel = UnityEngine.Random.Range(0.35f, 0.45f);

    //         // Listen for clients joining
    //         NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    //     }
    // }

    // Non-testing version:
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            NetworkManager.SceneManager.OnLoadComplete += OnNetworkSceneLoaded;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.SceneManager.OnLoadComplete -= OnNetworkSceneLoaded;
        }
    }

    private void OnNetworkSceneLoaded(ulong clientId, string sceneName, LoadSceneMode mode)
    {
        Debug.Log($"[Netcode] Scene loaded for client {clientId}: {sceneName}");

        // Only run logic once the SERVER finishes loading the Cave scene
        if (IsServer)
        {
            noiseScale = UnityEngine.Random.Range(0.1f, 0.15f);
            isoLevel = UnityEngine.Random.Range(0.35f, 0.45f);
            Debug.Log("Cave scene finished loading — initializing marching cubes.");
            SendCaveParametersClientRpc(noiseScale, isoLevel, resolution);

            // StartCoroutine(SceneSpawnPoint.Instance.RandomSpawnLocation((spawnPos) =>
            // {
            //     // Tell all clients the spawn position via PlayerSpawn
            //     // foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            //     // {
            //     //     PlayerSpawn ps = client.PlayerObject.GetComponent<PlayerSpawn>();
            //     //     if (ps != null)
            //     //     {
            //     //         ps.SetSpawnPosition(spawnPos); // <-- updates NetworkVariable
            //     //         Debug.Log($"Set spawn position for client {client.ClientId} to {spawnPos}");
            //     //         //MineCaveServerRpc(spawnPos, 8.5f, 0.4f, ignoreHold: true, raiseAmount: 2f);

            //     //     }

            //     // }
            // }));
        }

        
    }


    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} connected!");

        if (NetworkManager.Singleton.IsServer)
        {
            // send ONLY to this client
            ClientRpcParams rpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            };

            SendCaveParametersClientRpc(noiseScale, isoLevel, resolution, rpcParams);
        }
    }

    [ClientRpc]
    private void SendCaveParametersClientRpc(
        float noiseScale,
        float isoLevel,
        float resolution,
        ClientRpcParams rpcParams = default)
    {
        this.noiseScale = noiseScale;
        this.isoLevel = isoLevel;
        this.resolution = resolution;

        ulong myClientId = NetworkManager.Singleton.LocalClientId;
        Debug.Log($"Client {myClientId} received cave parameters: noiseScale={noiseScale}, isoLevel={isoLevel}, resolution={resolution}");

        StartCoroutine(WaitForServerAndGenerate());
    }


    private IEnumerator WaitForServerAndGenerate()
    {
        yield return new WaitForSeconds(0.5f);

        CreateCave();

        yield return null;

        Physics.SyncTransforms();

        surface.BuildNavMesh();

        // Wait until navmesh is baked
        yield return new WaitUntil(() => surface.navMeshData != null);

        yield return new WaitForSeconds(6.0f);

        // NOW the cave is fully ready on the client
        CaveFinished();  // 🔥 Fire event here instead
    }



    public void ClearCave()
    {
        // Transform meshys = GameObject.Find("Meshys")?.transform;
        // if (meshys != null)
        // {
        //     DestroyImmediate(meshys.gameObject);
        // }
        foreach (var kv in chunks)
        {
            if (kv.Value != null)
                DestroyImmediate(kv.Value);
        }
        chunks.Clear();
    }

    public static float Perlin3D(float x, float y, float z)
    {
        float ab = Mathf.PerlinNoise(x, y);
        float bc = Mathf.PerlinNoise(y, z);
        float ac = Mathf.PerlinNoise(x, z);
        float ba = Mathf.PerlinNoise(y, x);
        float cb = Mathf.PerlinNoise(z, y);
        float ca = Mathf.PerlinNoise(z, x);
        return (ab + bc + ac + ba + cb + ca) / 6f;
    }

    public void CreateCave()
    {
        ClearCave();
        GenerateDensityMap();

        chunks.Clear();

        for (int cx = 0; cx < caveWidth; cx += chunkSizeX)
            for (int cy = 0; cy < caveHeight; cy += chunkSizeY)
                for (int cz = 0; cz < caveDepth; cz += chunkSizeZ)
                {
                    GenerateChunkMesh(cx, cy, cz);
                }

        if (IsServer)
        {
            StartCoroutine(SpawnOresBatched());
            BoxSpawner boxSpawner = GetComponent<BoxSpawner>();
            if (boxSpawner != null && IsServer)
            {
                StartCoroutine(boxSpawner.SpawnBoxesOnSurface());
            }
        
            if (surface != null)
            {
                surface.BuildNavMesh();
            }
            else
            {
                Debug.LogWarning("NavMeshSurface component not found on MarchingCubes GameObject.");
            }

            MonsterSpawn monsterSpawner = GetComponent<MonsterSpawn>();
            if (monsterSpawner != null && IsServer)
            {
                StartCoroutine(monsterSpawner.SpawnMonstersOnSurface());
            }


            SceneSpawnPoint spawnPoint = GetComponent<SceneSpawnPoint>();
            Debug.Log("Checking for SceneSpawnPoint component." + (spawnPoint != null ? " Found." : " Not found."));
            if (spawnPoint != null && IsServer)
            {
                StartCoroutine(spawnPoint.RandomSpawnLocation((pos) => {
                    Debug.Log($"Spawn point generated at {pos}");
                }));
            }
        }
                
    }

    private IEnumerator SpawnOresBatched()
    {
        const int batchSize = 10;
        const int step = 2;                 // scan every 2 voxels
        const int yieldEvery = 5000;         // yield every N voxel checks

        float surfaceChance = oreChance * 0.06f;
        float deepChance    = oreChance * 0.12f;

        int voxelChecks = 0;
        int spawnedThisBatch = 0;

        for (int x = 1; x < caveWidth; x += step)
        for (int y = 1; y < caveHeight; y += step)
        for (int z = 1; z < caveDepth; z += step)
        {
            voxelChecks++;

            // ---- spread cost across frames ----
            if (voxelChecks % yieldEvery == 0)
                yield return null;

            float val = densityMap[x, y, z];
            if (val <= isoLevel)
                continue; // not solid → skip immediately

            // ---- 6-direction surface test (FAST) ----
            bool nearAir =
                densityMap[x + 1, y, z] <= isoLevel ||
                densityMap[x - 1, y, z] <= isoLevel ||
                densityMap[x, y + 1, z] <= isoLevel ||
                densityMap[x, y - 1, z] <= isoLevel ||
                densityMap[x, y, z + 1] <= isoLevel ||
                densityMap[x, y, z - 1] <= isoLevel;

            float chance = nearAir ? surfaceChance : deepChance;
            if (UnityEngine.Random.value > chance)
                continue;

            // ---- compute spawn position ----
            Vector3 pos = new Vector3(
                x + 0.5f,
                y + 0.5f,
                z + 0.5f
            ) * resolution;

            float offsetRadius = nearAir
                ? resolution * 0.15f
                : resolution * 0.3f;

            pos += UnityEngine.Random.insideUnitSphere * offsetRadius;

            // ---- spawn ore ----
            GameObject chosenOre =
                orePrefabs[UnityEngine.Random.Range(0, orePrefabs.Length)];

            NetworkObject oreInstance =
                Instantiate(chosenOre, pos, Quaternion.identity)
                .GetComponent<NetworkObject>();

            NavMeshModifier modifier =
                oreInstance.GetComponent<NavMeshModifier>();
            if (modifier == null)
                modifier = oreInstance.gameObject.AddComponent<NavMeshModifier>();

            modifier.ignoreFromBuild = true;

            oreInstance.Spawn();
            SpawnTracker.Instance.Register(oreInstance);

            oreInstance.name = chosenOre.name;
            OreNameClientRpc(oreInstance.NetworkObjectId, chosenOre.name);

            spawnedThisBatch++;

            // ---- batch spawns across frames ----
            if (spawnedThisBatch >= batchSize)
            {
                spawnedThisBatch = 0;
                yield return null;
            }
        }

        Debug.Log("[OreGen] Ore spawning complete.");
    }

    [ClientRpc]
    void OreNameClientRpc(ulong networkId, string newName)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkId, out NetworkObject netObj))
        {
            netObj.gameObject.name = newName;
        }
        else
        {
            Debug.LogWarning($"OreNameClientRpc: NetworkObject {networkId} not found on client yet.");
        }
    }
    

    private void GenerateChunkMesh(int startX, int startY, int startZ)
    {
        int sizeX = Mathf.Min(chunkSizeX+1, caveWidth - startX);
        int sizeY = Mathf.Min(chunkSizeY+1, caveHeight - startY);
        int sizeZ = Mathf.Min(chunkSizeZ+1, caveDepth - startZ);

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        int[,,] cellVertexIndex = new int[sizeX, sizeY, sizeZ];

        for (int x = 0; x < sizeX; x++)
            for (int y = 0; y < sizeY; y++)
                for (int z = 0; z < sizeZ; z++)
                    cellVertexIndex[x, y, z] = -1;

        // --- same dual marching cubes logic, just limited to this chunk ---
        // for (int x = 0; x < sizeX; x++)
        //     for (int y = 0; y < sizeY; y++)
        //         for (int z = 0; z < sizeZ; z++)
        //         {
        //             bool inside = false, outside = false;
        //             for (int i = 0; i < 8; i++)
        //             {
        //                 int xi = startX + x + ((i & 1) != 0 ? 1 : 0);
        //                 int yi = startY + y + ((i & 2) != 0 ? 1 : 0);
        //                 int zi = startZ + z + ((i & 4) != 0 ? 1 : 0);
        //                 float val = densityMap[xi, yi, zi];
        //                 if (val > isoLevel) inside = true; else outside = true;
        //             }

        //             if (inside && outside)
        //                 cellVertexIndex[x, y, z] = vertices.Count;
        //             vertices.Add(ComputeDualVertex(startX + x, startY + y, startZ + z));
                    
                        
        //         }

        for (int x = 0; x < sizeX; x++)
            for (int y = 0; y < sizeY; y++)
                for (int z = 0; z < sizeZ; z++)
                {
                    bool inside = false, outside = false;
                    for (int i = 0; i < 8; i++)
                    {
                        int xi = startX + x + ((i & 1) != 0 ? 1 : 0);
                        int yi = startY + y + ((i & 2) != 0 ? 1 : 0);
                        int zi = startZ + z + ((i & 4) != 0 ? 1 : 0);
                        float val = densityMap[xi, yi, zi];

                        // --- EDGE WALL / FLOOR / CEILING ONLY ---
                        int thickness = 1; // voxels thick
                        if (xi < thickness || xi >= caveWidth - thickness ||
                            zi < thickness || zi >= caveDepth - thickness ||
                            yi < thickness || yi >= caveHeight - thickness)
                        {
                            val = Mathf.Max(val, 1f); // force solid
                        }

                        densityMap[xi, yi, zi] = val;

                        if (val > isoLevel) inside = true; else outside = true;
                    }

                    if (inside && outside)
                        cellVertexIndex[x, y, z] = vertices.Count;

                    vertices.Add(ComputeDualVertex(startX + x, startY + y, startZ + z));
                }

        // Faces (XY, XZ, YZ) - reuse your existing AddFaceIfValid
        // make sure to offset indices properly for each chunk
        for (int z = 0; z < sizeZ-1; z++)
            for (int x = 0; x < sizeX - 1; x++)
                for (int y = 0; y < sizeY - 1; y++)
                    AddFaceIfValid(vertices, triangles,
                                cellVertexIndex[x, y, z],
                                cellVertexIndex[x + 1, y, z],
                                cellVertexIndex[x + 1, y + 1, z],
                                cellVertexIndex[x, y + 1, z]);

        for (int y = 0; y < sizeY-1; y++)
            for (int x = 0; x < sizeX - 1; x++)
                for (int z = 0; z < sizeZ - 1; z++)
                    AddFaceIfValid(vertices, triangles,
                                cellVertexIndex[x, y, z],
                                cellVertexIndex[x + 1, y, z],
                                cellVertexIndex[x + 1, y, z + 1],
                                cellVertexIndex[x, y, z + 1]);

        for (int x = 0; x < sizeX-1; x++)
            for (int y = 0; y < sizeY - 1; y++)
                for (int z = 0; z < sizeZ - 1; z++)
                    AddFaceIfValid(vertices, triangles,
                                cellVertexIndex[x, y, z],
                                cellVertexIndex[x, y + 1, z],
                                cellVertexIndex[x, y + 1, z + 1],
                                cellVertexIndex[x, y, z + 1]);

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);

        FixTriangleWindingUsingDensity(mesh);
        // mesh.RecalculateNormals();
        // mesh.RecalculateBounds();

        GameObject chunkObj = Instantiate(meshysPrefab); // instantiate as root (world)
        chunkObj.name = $"Meshys_{startX}_{startY}_{startZ}";
        chunkObj.layer = LayerMask.NameToLayer("Ground");
        chunkObj.tag = "Cave";

        MeshFilter mf = chunkObj.GetComponent<MeshFilter>();
        mf.mesh = mesh;
        MeshRenderer mr = chunkObj.GetComponent<MeshRenderer>();
        mr.material = material;
        MeshCollider mc = chunkObj.GetComponent<MeshCollider>();
        mc.sharedMesh = mesh;
        mc.convex = false;

        Physics.SyncTransforms();

        MeshysHelper helper = chunkObj.GetComponent<MeshysHelper>();
        helper.caveGenerator = this;

        NetworkObject netObj = chunkObj.GetComponent<NetworkObject>();

        if (IsServer)
        {
            // Spawn first
            netObj.Spawn();

            // Now safe to parent to caveParent (server)
            if (caveParent != null)
            {
                NetworkObject caveParentNetObj = caveParent.GetComponent<NetworkObject>();
                if (caveParentNetObj != null && caveParentNetObj.IsSpawned)
                {
                    try
                    {
                        netObj.TrySetParent(caveParentNetObj, true);
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
        }
        else
        {
            // This is for the client.
            if (caveParent != null)
            {
                NetworkObject caveParentNetObj = caveParent.GetComponent<NetworkObject>();
                if (caveParentNetObj != null && caveParentNetObj.IsSpawned)
                {
                    try
                    {
                        netObj.TrySetParent(caveParentNetObj, true);
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
        }

        // Save reference for mining / updates
        Vector3Int chunkKey = new Vector3Int(startX, startY, startZ);
        chunks[chunkKey] = chunkObj;
    }

    private void FixTriangleWindingUsingDensity(Mesh mesh)
    {
        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;

        // ---- helpers ----

        float SampleDensity(Vector3 gridPos)
        {
            int x0 = Mathf.Clamp((int)gridPos.x, 0, densityMap.GetLength(0) - 2);
            int y0 = Mathf.Clamp((int)gridPos.y, 0, densityMap.GetLength(1) - 2);
            int z0 = Mathf.Clamp((int)gridPos.z, 0, densityMap.GetLength(2) - 2);

            int x1 = x0 + 1;
            int y1 = y0 + 1;
            int z1 = z0 + 1;

            float xd = gridPos.x - x0;
            float yd = gridPos.y - y0;
            float zd = gridPos.z - z0;

            float c000 = densityMap[x0, y0, z0];
            float c100 = densityMap[x1, y0, z0];
            float c010 = densityMap[x0, y1, z0];
            float c110 = densityMap[x1, y1, z0];
            float c001 = densityMap[x0, y0, z1];
            float c101 = densityMap[x1, y0, z1];
            float c011 = densityMap[x0, y1, z1];
            float c111 = densityMap[x1, y1, z1];

            float c00 = Mathf.Lerp(c000, c100, xd);
            float c10 = Mathf.Lerp(c010, c110, xd);
            float c01 = Mathf.Lerp(c001, c101, xd);
            float c11 = Mathf.Lerp(c011, c111, xd);

            float c0 = Mathf.Lerp(c00, c10, yd);
            float c1 = Mathf.Lerp(c01, c11, yd);

            return Mathf.Lerp(c0, c1, zd);
        }

        Vector3 EstimateGradient(Vector3 gridPos)
        {
            float hx = 0.5f;
            float hy = 0.5f;
            float hz = 0.5f;

            float dx = SampleDensity(gridPos + new Vector3(hx, 0, 0)) -
                    SampleDensity(gridPos - new Vector3(hx, 0, 0));
            float dy = SampleDensity(gridPos + new Vector3(0, hy, 0)) -
                    SampleDensity(gridPos - new Vector3(0, hy, 0));
            float dz = SampleDensity(gridPos + new Vector3(0, 0, hz)) -
                    SampleDensity(gridPos - new Vector3(0, 0, hz));

            return new Vector3(dx, dy, dz) * 0.5f;
        }

        // ---- fix triangles ----

        for (int i = 0; i < tris.Length; i += 3)
        {
            int a = tris[i];
            int b = tris[i + 1];
            int c = tris[i + 2];

            Vector3 p0 = verts[a];
            Vector3 p1 = verts[b];
            Vector3 p2 = verts[c];

            Vector3 normal = Vector3.Cross(p1 - p0, p2 - p0).normalized;

            // Convert world-space position to grid-space
            Vector3 centroidGrid = (p0 + p1 + p2) / 3f / resolution;

            Vector3 grad = EstimateGradient(centroidGrid);

            // outward = direction of DECREASING density
            Vector3 outward = -grad;

            if (outward.sqrMagnitude < 1e-6f)
                continue;

            outward.Normalize();

            // If triangle faces into solid, flip it
            if (Vector3.Dot(normal, outward) < 0f)
            {
                // swap b and c
                tris[i + 1] = c;
                tris[i + 2] = b;
            }
        }

        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private void GenerateDensityMap()
    {
        densityMap = new float[caveWidth + 1, caveHeight + 1, caveDepth + 1];
        int offset = 996;

        float min = float.MaxValue;
        float max = float.MinValue;

        for (int x = 0; x <= caveWidth; x++)
            for (int y = 0; y <= caveHeight; y++)
                for (int z = 0; z <= caveDepth; z++)
                {
                    // float val = Perlin3D((x + offset) * noiseScale, (y + offset) * noiseScale, (z + offset) * noiseScale);
                    // densityMap[x, y, z] = val;
                    float rawVal = Perlin3D((x + offset) * noiseScale, (y + offset) * noiseScale, (z + offset) * noiseScale);

                    // enforce / blend floor so marching cubes will create a solid, flat floor at lower Y
                    float val = rawVal;

                    // clamp floor indices
                    int floorTop = Mathf.Clamp(floorYGrid + floorBlendThickness, 0, caveHeight);

                    // If below or equal exact floor, force solid (high density).
                    if (y <= floorYGrid)
                    {
                        val = Mathf.Max(val, 1.0f); // fully solid
                    }
                    else if (y <= floorTop)
                    {
                        // Smooth blend from fully solid at floorYGrid to slightly above isoLevel at floorTop
                        // t=0 at floorYGrid -> floorVal=1, t=1 at floorTop -> floorVal ~= isoLevel + epsilon
                        float t = (float)(y - floorYGrid) / Mathf.Max(1, floorBlendThickness);
                        float floorVal = Mathf.Lerp(1.0f, isoLevel + 0.01f, t); // small epsilon above iso so it remains solid-ish
                        val = Mathf.Max(val, floorVal);
                    }

                    densityMap[x, y, z] = val;
                    if (val < min) min = val;
                    if (val > max) max = val;

                }

        Debug.Log($"Density map range: min={min:F3}, max={max:F3}, isoLevel={isoLevel}");
    }


    private Vector3 VertexInterp(Vector3 p1, Vector3 p2, float valp1, float valp2)
    {
        float diff = valp2 - valp1;
        if (Mathf.Abs(diff) < 1e-6f)
            return (p1 + p2) * 0.5f; // midpoint if almost identical

        float t = Mathf.Clamp01((isoLevel - valp1) / diff);
        return Vector3.Lerp(p1, p2, t);
    }

    private Vector3 ComputeDualVertex(int x, int y, int z)
    {
        // Sample cube corners
        float[] cube = new float[8];
        Vector3[] cubePos = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            int xi = x + ((i & 1) != 0 ? 1 : 0);
            int yi = y + ((i & 2) != 0 ? 1 : 0);
            int zi = z + ((i & 4) != 0 ? 1 : 0);
            cube[i] = densityMap[xi, yi, zi];
            cubePos[i] = new Vector3(xi, yi, zi) * resolution;
        }

        // Average edge intersections (weighted by edge gradient magnitude)
        Vector3 sum = Vector3.zero;
        float weightSum = 0f;

        for (int i = 0; i < 12; i++)
        {
            int c0 = MarchingTable.Edges[i, 0];
            int c1 = MarchingTable.Edges[i, 1];
            float v0 = cube[c0];
            float v1 = cube[c1];

            bool crosses = (v0 > isoLevel && v1 < isoLevel) || (v1 > isoLevel && v0 < isoLevel);
            if (!crosses) continue;

            float t = Mathf.InverseLerp(v0, v1, isoLevel);
            Vector3 p = Vector3.Lerp(cubePos[c0], cubePos[c1], t);

            // approximate local gradient magnitude as |v1-v0|
            float w = Mathf.Abs(v1 - v0) + 1e-5f;
            sum += p * w;
            weightSum += w;
        }

        if (weightSum <= 0f)
        {
            // fallback to center of cube in world-space
            Vector3 fallback = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f) * resolution;

            // snap fallback to floor if it's at/below floor
            float floorWorldY = floorYGrid * resolution;
            if (fallback.y <= floorWorldY + resolution * 0.1f)
                fallback.y = floorWorldY;
            return fallback;
        }

        Vector3 result = sum / weightSum;

        // If computed vertex is very close to the floor, snap its Y to exact floor to get a perfectly flat plane
        float floorWorldY2 = floorYGrid * resolution;
        if (result.y <= floorWorldY2 + resolution * 0.1f)
            result.y = floorWorldY2;

        return result;
    }

    private void AddQuad(List<int> tris, int v0, int v1, int v2, int v3, List<Vector3> verts)
    {
        if (v0 < 0 || v1 < 0 || v2 < 0 || v3 < 0) return;

        Vector3 p0 = verts[v0];
        Vector3 p1 = verts[v1];
        Vector3 p2 = verts[v2];
        Vector3 p3 = verts[v3];

        // skip degenerate or overlapping quads
        if ((p0 - p1).sqrMagnitude < 1e-6f ||
            (p1 - p2).sqrMagnitude < 1e-6f ||
            (p2 - p3).sqrMagnitude < 1e-6f ||
            (p3 - p0).sqrMagnitude < 1e-6f)
            return;

        // ensure normal direction consistency
        tris.Add(v0);
        tris.Add(v1);
        tris.Add(v2);

        tris.Add(v0);
        tris.Add(v2);
        tris.Add(v3);
    }
    
    private float TriangleAreaSqr(Vector3 a, Vector3 b, Vector3 c)
    {
        return Vector3.Cross(b - a, c - a).sqrMagnitude * 0.25f; // squared area
    }

    private void AddFaceIfValid(List<Vector3> verts, List<int> tris, int ia, int ib, int ic, int id)
    {
        // Quick index checks
        if (ia < 0 || ib < 0 || ic < 0 || id < 0) return;

        Vector3 a = verts[ia];
        Vector3 b = verts[ib];
        Vector3 c = verts[ic];
        Vector3 d = verts[id];

        // Reject NaN/Inf
        if (!IsFinite(a) || !IsFinite(b) || !IsFinite(c) || !IsFinite(d)) return;

        // Local snap for stable comparisons (does NOT write back to verts)
        a = SnapForCompare(a);
        b = SnapForCompare(b);
        c = SnapForCompare(c);
        d = SnapForCompare(d);

        // If any two corners collapse, try the other diagonal before rejecting
        const float minSqrDist = 1e-8f; // small but not too large
        bool abClose = (a - b).sqrMagnitude < minSqrDist;
        bool bcClose = (b - c).sqrMagnitude < minSqrDist;
        bool cdClose = (c - d).sqrMagnitude < minSqrDist;
        bool daClose = (d - a).sqrMagnitude < minSqrDist;

        if (abClose && bcClose && cdClose && daClose)
            return; // entire quad collapsed

        // We'll pick the diagonal that yields larger total triangle area:
        // Diagonal AC -> triangles (A,B,C) + (A,C,D)
        // Diagonal BD -> triangles (B,C,D) + (B,D,A)
        float areaDiagAC = TriangleAreaSqr(a, b, c) + TriangleAreaSqr(a, c, d);
        float areaDiagBD = TriangleAreaSqr(b, c, d) + TriangleAreaSqr(b, d, a);

        // If both diagonals give negligible area, drop it
        const float minTotalArea = 1e-10f;
        if (areaDiagAC < minTotalArea && areaDiagBD < minTotalArea) return;

        // Choose diagonal with larger area
        if (areaDiagAC >= areaDiagBD)
        {
            // Ensure each triangle is not degenerate before adding
            if (TriangleAreaSqr(a, b, c) >= minTotalArea)
                AddTriWithConsistentWinding(tris, ia, ib, ic, a, b, c);
            if (TriangleAreaSqr(a, c, d) >= minTotalArea)
                AddTriWithConsistentWinding(tris, ia, ic, id, a, c, d);
        }
        else
        {
            if (TriangleAreaSqr(b, c, d) >= minTotalArea)
                AddTriWithConsistentWinding(tris, ib, ic, id, b, c, d);
            if (TriangleAreaSqr(b, d, a) >= minTotalArea)
                AddTriWithConsistentWinding(tris, ib, id, ia, b, d, a);
        }
    }

    // Helper: adds a triangle but ensures consistent winding (CCW) relative to its local normal
    private void AddTriWithConsistentWinding(List<int> tris, int i0, int i1, int i2, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        // Compute normal; if it's zero-length we skip (shouldn't happen due to area checks)
        Vector3 n = Vector3.Cross(p1 - p0, p2 - p0);
        if (n.sqrMagnitude < 1e-12f) return;

        // We want CCW winding in object space. Choose that convention and add indices accordingly.
        // The order (i0, i1, i2) is assumed to be CCW; if it's not, flip it.
        // Determine current winding by computing the sign of a scalar (arbitrary but consistent):
        // We'll use the Y component of normal as a cheap heuristic for flip detection only if it's significant;
        // otherwise fall back to using the full normal and a consistent "out" direction (Vector3.up).
        // This is intentionally conservative to avoid flipping valid triangles across seams.
        if (Vector3.Dot(n, Vector3.up) < 0f)
        {
            // flip winding
            tris.Add(i0); tris.Add(i2); tris.Add(i1);
        }
        else
        {
            tris.Add(i0); tris.Add(i1); tris.Add(i2);
        }
    }

    // small helpers
    private bool IsFinite(Vector3 v)
    {
        return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }

    private Vector3 SnapForCompare(Vector3 v)
    {
        const float s = 1e-4f; // small snap step for stable comparisons only
        return new Vector3(
            Mathf.Round(v.x / s) * s,
            Mathf.Round(v.y / s) * s,
            Mathf.Round(v.z / s) * s
        );
    }


    [ServerRpc(RequireOwnership = false)]
    public void MineCaveServerRpc(Vector3 worldPos, float radius, float depth, bool ignoreHold, float raiseAmount)
    {
        MineCave(worldPos, radius, depth, ignoreHold, raiseAmount);
        MineCaveClientRpc(worldPos, radius, depth, ignoreHold, raiseAmount);
    }

    [ClientRpc]
    private void MineCaveClientRpc(Vector3 worldPos, float radius, float depth, bool ignoreHold, float raiseAmount)
    {
        MineCave(worldPos, radius, depth, ignoreHold, raiseAmount);
    }

    public void MineCave(Vector3 worldPos, float radius, float depth, bool ignoreHold = false, float raiseAmount = 0f)
    {
        // --- Explosion bypasses all rate limits ---
        if (!ignoreHold)
        {
            // HOLD SYSTEM FIRST
            holdCount++;
            Debug.Log($"HoldCount: {holdCount}");

            if (holdCount >= 50) holdCount = 0;

            // Only mine when holdCount hits 1
            if (holdCount != 1)
            {
                Debug.Log("Hold system: skipping mining");
                return;
            }

            // --- Now apply cooldown ---
            if (Time.time - lastMineTime < mineCooldown)
            {
                Debug.Log("Cooldown active - skipping mining");
                return;
            }

            lastMineTime = Time.time; // consume cooldown
        }
        else
        {
            Debug.Log("Explosion Mining Cave at " + worldPos);
        }

        // --- SHIFT MINING UPWARD SO PLAYER DOESN'T FALL ---
        //float raiseAmount = radius * 1.25f;  // adjust if needed
        Vector3 adjustedPos = worldPos + Vector3.up * raiseAmount;

        // --- Perform carving ---
        int x0 = Mathf.Clamp(Mathf.FloorToInt(adjustedPos.x / resolution), 0, caveWidth);
        int y0 = Mathf.Clamp(Mathf.FloorToInt(adjustedPos.y / resolution), 0, caveHeight);
        int z0 = Mathf.Clamp(Mathf.FloorToInt(adjustedPos.z / resolution), 0, caveDepth);

        int r = Mathf.CeilToInt(radius / resolution);

        int maxFloorY = Mathf.Clamp(floorYGrid + floorBlendThickness, 0, caveHeight);
        int minCeilingY = caveHeight - floorBlendThickness; // dynamic ceiling blend

        int wallBlendThickness = 2; // number of voxels from edges

        for (int x = x0 - r; x <= x0 + r; x++)
            for (int y = y0 - r; y <= y0 + r; y++)
                for (int z = z0 - r; z <= z0 + r; z++)
                {
                    if (x < 0 || x > caveWidth || y < 0 || y > caveHeight || z < 0 || z > caveDepth)
                        continue;

                    Vector3 voxelCenter = new Vector3(
                        x + 0.5f,
                        y + 0.5f,
                        z + 0.5f
                    ) * resolution;

                    // --- USE ADJUSTED POSITION FOR RADIUS CHECK ---
                    if (Vector3.Distance(voxelCenter, adjustedPos) <= radius)
                    {
                        float newDensity = densityMap[x, y, z] - depth;

                        // --- Floor blend ---
                        if (y <= maxFloorY)
                        {
                            float minDensity = Mathf.Lerp(
                                1.0f,
                                isoLevel + 0.01f,
                                (y - floorYGrid) / Mathf.Max(1, floorBlendThickness)
                            );
                            newDensity = Mathf.Max(newDensity, minDensity);
                        }

                        // --- Ceiling blend ---
                        if (y >= minCeilingY)
                        {
                            float minDensity = Mathf.Lerp(
                                1.0f,
                                isoLevel + 0.01f,
                                (caveHeight - y) / Mathf.Max(1, floorBlendThickness)
                            );
                            newDensity = Mathf.Max(newDensity, minDensity);
                        }

                        // --- Walls blend ---
                        if (x < wallBlendThickness)
                            newDensity = Mathf.Max(newDensity, 0.8f);
                        if (x > caveWidth - wallBlendThickness)
                            newDensity = Mathf.Max(newDensity, 0.8f);
                        if (z < wallBlendThickness)
                            newDensity = Mathf.Max(newDensity, 0.8f);
                        if (z > caveDepth - wallBlendThickness)
                            newDensity = Mathf.Max(newDensity, 0.8f);

                        densityMap[x, y, z] = Mathf.Clamp(newDensity, 0f, 1f);
                    }
                }

        // These still use the original worldPos for visual effects & chunk updates
        UpdateAffectedChunks(worldPos, radius);

        PlayMineEffectsClientRpc(worldPos);

        if (surface != null)
            StartCoroutine(DelayedNavMeshRebuild());
    }


    
    


    [ClientRpc]
    private void PlayMineEffectsClientRpc(Vector3 position)
    {
        // SOUND
        AudioManager.instance.PlaySFXClip("mine" + UnityEngine.Random.Range(1, 5), transform);

        // PARTICLES (assign via Inspector)
        // if (mineParticlePrefab != null)
        // {
        //     var ps = Instantiate(mineParticlePrefab, position, Quaternion.identity);
        //     ps.Play();
        //     Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
        // }
    }


    private IEnumerator DelayedNavMeshRebuild()
    {
        // Wait one frame so destroyed meshes are deleted and new ones are generated
        yield return null;
        yield return null; // sometimes 1 frame is enough, sometimes 2 is safer

        if (surface != null && IsServer)
            surface.UpdateNavMesh(surface.navMeshData);
    }

    private void UpdateAffectedChunks(Vector3 worldPos, float radius)
    {
        int minX = Mathf.Max(0, Mathf.FloorToInt((worldPos.x - radius) / chunkSizeX) * chunkSizeX);
        int minY = Mathf.Max(0, Mathf.FloorToInt((worldPos.y - radius) / chunkSizeY) * chunkSizeY);
        int minZ = Mathf.Max(0, Mathf.FloorToInt((worldPos.z - radius) / chunkSizeZ) * chunkSizeZ);

        int maxX = Mathf.Min(caveWidth, Mathf.CeilToInt((worldPos.x + radius) / chunkSizeX) * chunkSizeX);
        int maxY = Mathf.Min(caveHeight, Mathf.CeilToInt((worldPos.y + radius) / chunkSizeY) * chunkSizeY);
        int maxZ = Mathf.Min(caveDepth, Mathf.CeilToInt((worldPos.z + radius) / chunkSizeZ) * chunkSizeZ);

        for (int cx = minX; cx < maxX; cx += chunkSizeX)
            for (int cy = minY; cy < maxY; cy += chunkSizeY)
                for (int cz = minZ; cz < maxZ; cz += chunkSizeZ)
                {
                    Vector3Int key = new Vector3Int(cx, cy, cz);
                    if (chunks.TryGetValue(key, out GameObject chunkObj))
                    {
                        // Destroy old mesh and regenerate it using the current densityMap
                        Destroy(chunkObj);
                        GenerateChunkMesh(cx, cy, cz);
                    }
                }
    }
    
}