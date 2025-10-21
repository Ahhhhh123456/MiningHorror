using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;


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

    [Header("Ore Settings")]
    public float oreChance;
    public GameObject[] orePrefabs; // Prefabs for ore instantiation
    public NetworkObject[] NetworkOrePrefabs; // Networked versions

    private float[,,] densityMap;

    void Start()
    {
        CreateCave();
    }
    public void ClearCave()
    {
        Transform meshys = GameObject.Find("Meshys")?.transform;
        if (meshys != null)
        {
            DestroyImmediate(meshys.gameObject);
        }
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
        Mesh mesh = GenerateMeshFromDensity();

        Debug.Log($"Generated mesh vertices: {mesh.vertexCount}, triangles: {mesh.triangles.Length / 3}");

        // ensure bounds/normals are up-to-date
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // Validate mesh for NaN/Infinity and degenerate triangles
        bool valid = ValidateMesh(mesh, out string validationMessage);

        GameObject g = new GameObject("Meshys");
        g.transform.position = Vector3.zero;
        MeshFilter mf = g.AddComponent<MeshFilter>();
        MeshRenderer mr = g.AddComponent<MeshRenderer>();
        mr.material = material;
        mf.mesh = mesh;
    
        if (valid)
        {
            MeshCollider mc = g.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            mc.convex = false; // default is false
            g.layer = LayerMask.NameToLayer("Ground");
            Debug.Log("MeshCollider assigned successfully.");
        }
        else
        {
            Debug.LogError($"Mesh validation failed: {validationMessage}. Skipping MeshCollider and adding fallback BoxCollider.");
            // Fallback collider so physics still works while we debug geometry
            BoxCollider bc = g.AddComponent<BoxCollider>();
            // use mesh bounds to size the box
            bc.center = mesh.bounds.center;
            bc.size = mesh.bounds.size;
            g.layer = LayerMask.NameToLayer("Ground");
        }
    }

    // Validate mesh: NaN/Infinity check + detect zero-area triangles
    private bool ValidateMesh(Mesh mesh, out string msg)
    {
        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;

        // 1) check for NaN/Infinity
        for (int i = 0; i < verts.Length; i++)
        {
            var v = verts[i];
            if (float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z))
            {
                msg = $"NaN in vertex {i}: {v}";
                return false;
            }
            if (float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z))
            {
                msg = $"Infinity in vertex {i}: {v}";
                return false;
            }
        }

        // 2) triangle index sanity
        if (tris.Length % 3 != 0)
        {
            msg = $"Triangles length not multiple of 3: {tris.Length}";
            return false;
        }
        for (int i = 0; i < tris.Length; i++)
        {
            if (tris[i] < 0 || tris[i] >= verts.Length)
            {
                msg = $"Triangle index out of range at tris[{i}] = {tris[i]} (vertex count {verts.Length})";
                return false;
            }
        }

        // 3) count degenerate triangles (zero area)
        int degenerateCount = 0;
        const float areaEpsilon = 1e-6f; // threshold - adjust if needed
        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 v0 = verts[tris[i]];
            Vector3 v1 = verts[tris[i + 1]];
            Vector3 v2 = verts[tris[i + 2]];

            // area ~ length of cross((v1-v0),(v2-v0)) / 2
            var cross = Vector3.Cross(v1 - v0, v2 - v0);
            float area2 = cross.sqrMagnitude; // squared area*4
            if (area2 <= areaEpsilon)
            {
                degenerateCount++;
                if (degenerateCount <= 5)
                {
                    Debug.LogWarning($"Degenerate tri #{degenerateCount} at triIndex {i / 3}: v0={v0}, v1={v1}, v2={v2}, area2={area2}");
                }
            }
        }

        if (degenerateCount > 0)
        {
            msg = $"Found {degenerateCount} degenerate triangles (zero/near-zero area).";
            return false;
        }

        msg = "OK";
        return true;
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

    private Mesh GenerateMeshFromDensity()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // Store dual vertex index for each cell (use -1 as "none")
        int[,,] cellVertexIndex = new int[caveWidth, caveHeight, caveDepth];
        for (int xi = 0; xi < caveWidth; xi++)
            for (int yi = 0; yi < caveHeight; yi++)
                for (int zi = 0; zi < caveDepth; zi++)
                    cellVertexIndex[xi, yi, zi] = -1;

        // 1) Create one vertex per cell that intersects the surface
        for (int x = 0; x < caveWidth; x++)
        {
            for (int y = 0; y < caveHeight; y++)
            {
                for (int z = 0; z < caveDepth; z++)
                {
                    bool inside = false, outside = false;
                    for (int i = 0; i < 8; i++)
                    {
                        int xi = x + ((i & 1) != 0 ? 1 : 0);
                        int yi = y + ((i & 2) != 0 ? 1 : 0);
                        int zi = z + ((i & 4) != 0 ? 1 : 0);
                        float val = densityMap[xi, yi, zi];
                        if (val > isoLevel) inside = true; else outside = true;
                    }

                    if (inside && outside)
                    {
                        Vector3 dualV = ComputeDualVertex(x, y, z);
                        cellVertexIndex[x, y, z] = vertices.Count;
                        vertices.Add(dualV);
                    }
                }
            }
        }

        // 2) For each grid face, connect the four adjacent cell centers into two triangles.
        // We'll iterate internal faces on XY, XZ, YZ planes to avoid duplicates.
        // XY faces (constant z) -> cells: (x,y,z),(x+1,y,z),(x+1,y+1,z),(x,y+1,z)
        for (int z = 0; z < caveDepth; z++)
        {
            for (int x = 0; x < caveWidth - 1; x++)
            {
                for (int y = 0; y < caveHeight - 1; y++)
                {
                    int a = cellVertexIndex[x, y, z];
                    int b = cellVertexIndex[x + 1, y, z];
                    int c = cellVertexIndex[x + 1, y + 1, z];
                    int d = cellVertexIndex[x, y + 1, z];
                    AddFaceIfValid(vertices, triangles, a, b, c, d);
                }
            }
        }

        // XZ faces (constant y) -> cells: (x,y,z),(x+1,y,z),(x+1,y,z+1),(x,y,z+1)
        for (int y = 0; y < caveHeight; y++)
        {
            for (int x = 0; x < caveWidth - 1; x++)
            {
                for (int z = 0; z < caveDepth - 1; z++)
                {
                    int a = cellVertexIndex[x, y, z];
                    int b = cellVertexIndex[x + 1, y, z];
                    int c = cellVertexIndex[x + 1, y, z + 1];
                    int d = cellVertexIndex[x, y, z + 1];
                    AddFaceIfValid(vertices, triangles, a, b, c, d);
                }
            }
        }

        // YZ faces (constant x) -> cells: (x,y,z),(x,y+1,z),(x,y+1,z+1),(x,y,z+1)
        for (int x = 0; x < caveWidth; x++)
        {
            for (int y = 0; y < caveHeight - 1; y++)
            {
                for (int z = 0; z < caveDepth - 1; z++)
                {
                    int a = cellVertexIndex[x, y, z];
                    int b = cellVertexIndex[x, y + 1, z];
                    int c = cellVertexIndex[x, y + 1, z + 1];
                    int d = cellVertexIndex[x, y, z + 1];
                    AddFaceIfValid(vertices, triangles, a, b, c, d);
                }
            }
        }

        // 3) Build final mesh
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private void MarchCube(int x, int y, int z, List<Vector3> verts, List<int> tris, List<Vector2> uvs, Dictionary<long, int> vertexCache)
    {
        float[] cube = new float[8];
        Vector3[] cubePos = new Vector3[8];

        // Gather cube corner positions and density values
        for (int i = 0; i < 8; i++)
        {
            int xi = x + ((i & 1) != 0 ? 1 : 0);
            int yi = y + ((i & 2) != 0 ? 1 : 0);
            int zi = z + ((i & 4) != 0 ? 1 : 0);
            cube[i] = densityMap[xi, yi, zi];
            cubePos[i] = new Vector3(xi, yi, zi) * resolution;
        }

        // Determine cube configuration
        int cubeIndex = 0;
        for (int i = 0; i < 8; i++)
            if (cube[i] > isoLevel) cubeIndex |= 1 << i;

        if (cubeIndex == 0 || cubeIndex == 255)
            return; // completely inside or outside

        int[,] edges = MarchingTable.Edges;
        int[,] triTable = MarchingTable.Triangles;

        // --- Helper function: get or create cached vertex ---
        int GetOrCreateVertex(int edgeIndex)
        {
            int c0 = edges[edgeIndex, 0];
            int c1 = edges[edgeIndex, 1];

            // Build a unique 64-bit key for this edge in world-space
            // This avoids duplicates between neighboring cubes.
            //long key = (((long)x & 0xFFFF) << 48 | (((long)y & 0xFFFF) << 32) | (((long)z & 0xFFFF) << 16) | (long)edgeIndex);
            Vector3Int edgeVerticesMin = new Vector3Int(
                Mathf.Min(c0 % 2 + x, c1 % 2 + x),
                Mathf.Min((c0 / 2) % 2 + y, (c1 / 2) % 2 + y),
                Mathf.Min((c0 / 4) % 2 + z, (c1 / 4) % 2 + z)
            );
            long key = ((long)edgeVerticesMin.x << 48) | ((long)edgeVerticesMin.y << 32) | (long)edgeVerticesMin.z << 16 | edgeIndex;


            if (vertexCache.TryGetValue(key, out int cachedIndex))
                return cachedIndex;

            Vector3 pos = VertexInterp(cubePos[c0], cubePos[c1], cube[c0], cube[c1]);
            int newIndex = verts.Count;
            verts.Add(pos);
            uvs.Add(new Vector2(pos.x, pos.z)); // placeholder UVs

            vertexCache[key] = newIndex;
            return newIndex;
        }

        // --- Generate triangles using cached/shared vertices ---
        for (int i = 0; triTable[cubeIndex, i] != -1; i += 3)
        {
            int a = GetOrCreateVertex(triTable[cubeIndex, i]);
            int b = GetOrCreateVertex(triTable[cubeIndex, i + 1]);
            int c = GetOrCreateVertex(triTable[cubeIndex, i + 2]);

            // Skip degenerate triangles
            Vector3 v0 = verts[a];
            Vector3 v1 = verts[b];
            Vector3 v2 = verts[c];
            if (Vector3.Distance(v0, v1) < 0.0001f ||
                Vector3.Distance(v1, v2) < 0.0001f ||
                Vector3.Distance(v2, v0) < 0.0001f)
                continue;

            Vector3 cross = Vector3.Cross(v1 - v0, v2 - v0);
            if (cross.sqrMagnitude < 0.00001f)
                continue;

            tris.Add(a);
            tris.Add(b);
            tris.Add(c);
        }
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
        tris.Add(v2);
        tris.Add(v1);

        tris.Add(v0);
        tris.Add(v3);
        tris.Add(v2);
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
        tris.Add(ia); tris.Add(ib); tris.Add(ic);
        tris.Add(ia); tris.Add(ic); tris.Add(id);
    }

    private float TriangleAreaSqr(Vector3 a, Vector3 b, Vector3 c)
    {
        return Vector3.Cross(b - a, c - a).sqrMagnitude * 0.25f; // squared area
    }

}




    // private IEnumerator SpawnOresBatched()
    // {
    //     int batchSize = 50; // spawn 50 ores per frame
    //     int index = 0;

    //     while (index < blockPositions.Count)
    //     {
    //         for (int i = 0; i < batchSize && index < blockPositions.Count; i++, index++)
    //         {
    //             if (Random.value < oreChance)
    //             {
    //                 GameObject chosenOre = orePrefabs[Random.Range(0, orePrefabs.Length)];
    //                 NetworkObject oreInstance = Instantiate(chosenOre, blockPositions[index], Quaternion.identity)
    //                                             .GetComponent<NetworkObject>();

    //                 oreInstance.Spawn(); // Spawn first
    //                 oreInstance.name = chosenOre.name; // rename on server

    //                 // Now safely call the ClientRpc
    //                 OreNameClientRpc(oreInstance.NetworkObjectId, chosenOre.name);
    //             }
    //         }
    //         yield return null; // wait a frame to avoid flooding the client
    //     }

    //     Debug.Log("Finished spawning ores in batches.");
    // }

    // [ClientRpc]
    // void OreNameClientRpc(ulong networkId, string newName)
    // {
    //     if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkId, out NetworkObject netObj))
    //     {
    //         netObj.gameObject.name = newName;
    //     }
    //     else
    //     {
    //         Debug.LogWarning($"OreNameClientRpc: NetworkObject {networkId} not found on client yet.");
    //     }
    // }

    // public override void OnNetworkSpawn()
    // {
    //     base.OnNetworkSpawn();

    //     if (IsServer)
    //     {
    //         NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
    //         {
    //             if (clientId == NetworkManager.Singleton.LocalClientId) return; // skip host

    //             Debug.Log("Host: Client connected, generating cave...");
    //             StartCoroutine(SpawnOresBatched()); // generate NetworkObjects for ores safely
    //         };
    //     }
    // }


