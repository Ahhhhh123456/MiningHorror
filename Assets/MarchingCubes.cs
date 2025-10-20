using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;


public class MarchingCubes : NetworkBehaviour
{
    [Header("Cave Settings")]
    public Material material;
    public int caveWidth = 50;  // reduced for testing
    public int caveHeight = 20;
    public int caveDepth = 50;
    private float noiseScale = 0.05f;
    private float isoLevel = 0.5f; // threshold for surface

    [Header("Ore Settings")]
    public float oreChance = 0.1f;
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
                    Debug.LogWarning($"Degenerate tri #{degenerateCount} at triIndex {i/3}: v0={v0}, v1={v1}, v2={v2}, area2={area2}");
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
        List<Vector2> uvs = new List<Vector2>();

        for (int x = 0; x < caveWidth; x++)
            for (int y = 0; y < caveHeight; y++)
                for (int z = 0; z < caveDepth; z++)
                    MarchCube(x, y, z, vertices, triangles, uvs);

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void MarchCube(int x, int y, int z, List<Vector3> verts, List<int> tris, List<Vector2> uvs)
    {
        float[] cube = new float[8];
        Vector3[] cubePos = new Vector3[8];

        for (int i = 0; i < 8; i++)
        {
            int xi = x + ((i & 1) != 0 ? 1 : 0);
            int yi = y + ((i & 2) != 0 ? 1 : 0);
            int zi = z + ((i & 4) != 0 ? 1 : 0);
            cube[i] = densityMap[xi, yi, zi];
            cubePos[i] = new Vector3(xi, yi, zi);
        }

        int cubeIndex = 0;
        for (int i = 0; i < 8; i++)
            if (cube[i] > isoLevel) cubeIndex |= 1 << i;

        if (cubeIndex == 0 || cubeIndex == 255) return;

        int[,] edges = MarchingTable.Edges;
        int[,] triTable = MarchingTable.Triangles;

        Vector3[] edgeVertex = new Vector3[12];

        for (int i = 0; i < 12; i++)
        {
            int c0 = MarchingTable.Edges[i, 0];
            int c1 = MarchingTable.Edges[i, 1];

            edgeVertex[i] = VertexInterp(cubePos[c0], cubePos[c1], cube[c0], cube[c1]);
        }


        
        // Check if this cube index has any triangle entries
        if (triTable[cubeIndex, 0] == -1)
        {
            Debug.LogWarning($"CubeIndex {cubeIndex} has no triangles in table!");
            return; // skip early since there's nothing to draw
        }

        // Generate triangles
        for (int i = 0; triTable[cubeIndex, i] != -1; i += 3)
        {
            int a = verts.Count;
            Vector3 v0 = edgeVertex[triTable[cubeIndex, i]];
            Vector3 v1 = edgeVertex[triTable[cubeIndex, i + 1]];
            Vector3 v2 = edgeVertex[triTable[cubeIndex, i + 2]];

            // Skip degenerate or zero-area triangles
            if (Vector3.Distance(v0, v1) < 0.0001f ||
                Vector3.Distance(v1, v2) < 0.0001f ||
                Vector3.Distance(v2, v0) < 0.0001f)
                continue;

            Vector3 cross = Vector3.Cross(v1 - v0, v2 - v0);
            if (cross.sqrMagnitude < 0.00001f)
                continue;

            verts.Add(v0);
            uvs.Add(new Vector2(v0.x, v0.z)); // placeholder UV
            verts.Add(v1);
            uvs.Add(new Vector2(v1.x, v1.z));
            verts.Add(v2);
            uvs.Add(new Vector2(v2.x, v2.z));
            tris.Add(a); tris.Add(a + 1); tris.Add(a + 2);
        }
    }
    private Vector3 VertexInterp(Vector3 p1, Vector3 p2, float valp1, float valp2)
    {
        float diff = valp2 - valp1;
        if (Mathf.Abs(diff) < 1e-4f) 
            return (p1 + p2) * 0.5f; // midpoint if almost identical

        float t = Mathf.Clamp01((isoLevel - valp1) / diff);
        return Vector3.Lerp(p1, p2, t);
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


