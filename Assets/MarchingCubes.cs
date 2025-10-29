using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Unity.AI.Navigation;

public class MarchingCubes : NetworkBehaviour
{
    [Header("Cave Settings")]
    public Material material;
    public int caveWidth;  // reduced for testing
    public int caveHeight;
    public int caveDepth;


    [Header("Noise Settings")]
    public float noiseScale;
    public float isoLevel;

    public float resolution;

    [Header("Chunk Settings")]
    public int chunkSizeX;
    public int chunkSizeY;
    public int chunkSizeZ;
    private Dictionary<Vector3Int, GameObject> chunks = new Dictionary<Vector3Int, GameObject>();

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

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            CreateCave(); // spawn chunks here, not Start()
        }
        else if (IsClient)
        {
            // Clients also generate locally so they can see geometry
            StartCoroutine(WaitForServerAndGenerate());

        }
    }

    private IEnumerator WaitForServerAndGenerate()
    {
        // Wait for the cave parameters (like width, height, etc.) to sync if needed
        yield return new WaitForSeconds(0.5f);
        CreateCave();
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
        }
                
    }
    
    private IEnumerator SpawnOresBatched()
    {
        int batchSize = 25;
        List<Vector3> spawnPositions = new List<Vector3>();

        float surfaceChance = oreChance * 0.06f;
        float deepChance = oreChance * 0.12f;

        for (int x = 1; x < caveWidth; x++)
            for (int y = 1; y < caveHeight; y++)
                for (int z = 1; z < caveDepth; z++)
                {
                    float val = densityMap[x, y, z];
                    bool solid = val > isoLevel;
                    if (!solid) continue;

                    // --- Check for nearby air (surface detection)
                    bool nearAir = false;
                    for (int dx = -1; dx <= 1 && !nearAir; dx++)
                        for (int dy = -1; dy <= 1 && !nearAir; dy++)
                            for (int dz = -1; dz <= 1 && !nearAir; dz++)
                            {
                                int nx = x + dx, ny = y + dy, nz = z + dz;
                                if (nx < 0 || ny < 0 || nz < 0 ||
                                    nx > caveWidth || ny > caveHeight || nz > caveDepth)
                                    continue;
                                if (densityMap[nx, ny, nz] <= isoLevel)
                                    nearAir = true;
                            }

                    // --- Choose correct spawn chance
                    float chance = nearAir ? surfaceChance : deepChance;
                    if (Random.value > chance)
                        continue;

                    // --- Compute world-space position
                    Vector3 pos = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f) * resolution;

                    if (nearAir)
                    {
                        // Surface ores: slight offset inward so they appear on walls
                        pos += Random.insideUnitSphere * (resolution * 0.15f);
                    }
                    else
                    {
                        // Deep ores: buried inside rock
                        pos += Random.insideUnitSphere * (resolution * 0.3f);
                    }

                    spawnPositions.Add(pos);
                }

        Debug.Log($"[OreGen] Found {spawnPositions.Count} potential ore spots.");

        // --- Spawn them in small batches ---
        int index = 0;
        while (index < spawnPositions.Count)
        {
            for (int i = 0; i < batchSize && index < spawnPositions.Count; i++, index++)
            {
                Vector3 spawnPos = spawnPositions[index];
                GameObject chosenOre = orePrefabs[Random.Range(0, orePrefabs.Length)];

                // NetworkObject oreInstance = Instantiate(chosenOre, spawnPos, Quaternion.identity)
                //                             .GetComponent<NetworkObject>();

                // oreInstance.Spawn();
                // oreInstance.name = chosenOre.name;
                // OreNameClientRpc(oreInstance.NetworkObjectId, chosenOre.name);
                NetworkObject oreInstance = Instantiate(chosenOre, spawnPos, Quaternion.identity)
                                .GetComponent<NetworkObject>();

                // --- Make sure NavMesh ignores this ore ---
                NavMeshModifier modifier = oreInstance.GetComponent<NavMeshModifier>();
                if (modifier == null)
                {
                    modifier = oreInstance.gameObject.AddComponent<NavMeshModifier>();
                }
                modifier.ignoreFromBuild = true;

                // --- Now spawn it ---
                oreInstance.Spawn();
                oreInstance.name = chosenOre.name;
                OreNameClientRpc(oreInstance.NetworkObjectId, chosenOre.name);
            }

            yield return null;
        }

        Debug.Log("[OreGen] Finished spawning surface + deep ores.");
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
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject chunkObj = Instantiate(meshysPrefab, transform);
        chunkObj.name = $"Meshys_{startX}_{startY}_{startZ}";
        chunkObj.layer = LayerMask.NameToLayer("Ground");
        chunkObj.tag = "Cave";

        // Assign mesh before spawning
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
            netObj.Spawn();
            chunkObj.transform.SetParent(caveParent.transform, true); // true keeps world pos
        }

        // Save reference for mining / updates
        Vector3Int chunkKey = new Vector3Int(startX, startY, startZ);
        chunks[chunkKey] = chunkObj;
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
                    float val = Perlin3D((x + offset) * noiseScale, (y + offset) * noiseScale, (z + offset) * noiseScale);
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
            return new Vector3(x + 0.5f, y + 0.5f, z + 0.5f) * resolution;
        }

        return sum / weightSum;
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
    
    private void AddFaceIfValid(List<Vector3> verts, List<int> tris, int ia, int ib, int ic, int id)
    {
        // all four indices must exist
        if (ia < 0 || ib < 0 || ic < 0 || id < 0) return;

        Vector3 a = verts[ia];
        Vector3 b = verts[ib];
        Vector3 c = verts[ic];
        Vector3 d = verts[id];

        // Quick distance checks to avoid near-duplicates
        const float minSqrDist = 1e-6f;
        if ((a - b).sqrMagnitude < minSqrDist ||
            (b - c).sqrMagnitude < minSqrDist ||
            (c - d).sqrMagnitude < minSqrDist ||
            (d - a).sqrMagnitude < minSqrDist)
            return;

        // Triangles: (a,b,c) and (a,c,d) — check both areas
        float area1 = TriangleAreaSqr(a, b, c);
        float area2 = TriangleAreaSqr(a, c, d);

        const float minAreaSqr = 1e-6f; // adjust upward if needed
        if (area1 < minAreaSqr || area2 < minAreaSqr) return;

        // Add with consistent winding (CCW) — if needed you can flip order to match normals.
        // tris.Add(ia); tris.Add(ib); tris.Add(ic);
        // tris.Add(ia); tris.Add(ic); tris.Add(id);
        tris.Add(ia); tris.Add(ic); tris.Add(ib);
        tris.Add(ia); tris.Add(id); tris.Add(ic);
    }

    private float TriangleAreaSqr(Vector3 a, Vector3 b, Vector3 c)
    {
        return Vector3.Cross(b - a, c - a).sqrMagnitude * 0.25f; // squared area
    }


    [ServerRpc(RequireOwnership = false)]
    public void MineCaveServerRpc(Vector3 worldPos, float radius, float depth, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        Debug.Log($"MineCaveServerRpc called by client {clientId} at position {worldPos}");

        // Update the server density map
        MineCave(worldPos, radius, depth);

        // Tell all clients (including host) to update
        MineCaveClientRpc(worldPos, radius, depth);
    }
    
    [ClientRpc]
    private void MineCaveClientRpc(Vector3 worldPos, float radius, float depth)
    {
        // Skip the server because it already applied it
        if (IsServer) return;

        MineCave(worldPos, radius, depth);
    }
    public void MineCave(Vector3 worldPos, float radius, float depth)
    {
        holdCount++;
        Debug.Log(holdCount);
        if (holdCount == 50)
        {
            holdCount = 0;
            Debug.Log("resetting holdcount");
        }
        if (holdCount == 1)
        {
            Debug.Log("Hold Count 100 Reached - Mining Cave at " + worldPos);
            int x0 = Mathf.Clamp(Mathf.FloorToInt(worldPos.x / resolution), 0, caveWidth);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(worldPos.y / resolution), 0, caveHeight);
            int z0 = Mathf.Clamp(Mathf.FloorToInt(worldPos.z / resolution), 0, caveDepth);

            int r = Mathf.CeilToInt(radius / resolution);

            for (int x = x0 - r; x <= x0 + r; x++)
                for (int y = y0 - r; y <= y0 + r; y++)
                    for (int z = z0 - r; z <= z0 + r; z++)
                    {
                        if (x < 0 || x > caveWidth || y < 0 || y > caveHeight || z < 0 || z > caveDepth) continue;

                        Vector3 voxelCenter = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f) * resolution;
                        if (Vector3.Distance(voxelCenter, worldPos) <= radius)
                        {
                            densityMap[x, y, z] -= depth;
                            densityMap[x, y, z] = Mathf.Clamp(densityMap[x, y, z], 0f, 1f);
                        }
                    }

            // Update affected chunks locally
            UpdateAffectedChunks(worldPos, radius);
        }

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