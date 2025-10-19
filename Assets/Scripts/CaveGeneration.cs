using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class CaveGeneration : NetworkBehaviour
{
    [Header("Cave Settings")]
    public GameObject blockPrefab;
    public Material material;
    private float noiseScale = 0.05f;
    private int caveWidth = 200;
    private int caveHeight = 20;
    private int caveDepth = 200;

    [Header("Ore Settings")]
    public float oreChance = 0.1f;
    public GameObject[] orePrefabs; // Prefabs for ore instantiation
    public NetworkObject[] NetworkOrePrefabs; // Networked versions

    private List<CombineInstance> combine = new List<CombineInstance>(); // For combining block meshes
    private List<Vector3> blockPositions = new List<Vector3>(); // Store positions for ore spawning
    private int tempCount = 0;

    void Start()
    {
        // Optional: Uncomment to generate cave in editor mode
        CreateCave();
    }

public override void OnNetworkSpawn()
{
    base.OnNetworkSpawn();

    if (IsServer)
    {
        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
        {
            if (clientId == NetworkManager.Singleton.LocalClientId) return; // skip host

            Debug.Log("Host: Client connected, generating cave...");
            StartCoroutine(SpawnOresBatched()); // generate NetworkObjects for ores safely
        };
    }
}



    public void CreateCave()
    {
        // Create a single mesh filter to copy meshes into CombineInstance
        MeshFilter blockMesh = Instantiate(blockPrefab, Vector3.zero, Quaternion.identity).GetComponent<MeshFilter>();

        int offset = 996; // fixed offset for Perlin noise
        Debug.Log("Offset: " + offset);

        // Loop through all positions
        for (int x = 0; x < caveWidth; x++)
        {
            for (int y = 0; y < caveHeight; y++)
            {
                for (int z = 0; z < caveDepth; z++)
                {
                    float noiseValue = Perlin3D((x + offset) * noiseScale / 2, (y + offset) * noiseScale, (z + offset) * noiseScale / 2);

                    if ((noiseValue < 0.45f || noiseValue > 0.55f) ||
                        (y == 0 || y == caveHeight - 1) ||
                        (x == 0 || x == caveWidth - 1) ||
                        (z == 0 || z == caveDepth - 1))
                    {
                        blockMesh.transform.position = new Vector3(x, y, z);

                        combine.Add(new CombineInstance
                        {
                            mesh = blockMesh.sharedMesh,
                            transform = blockMesh.transform.localToWorldMatrix
                        });

                        blockPositions.Add(new Vector3(x, y, z));
                    }
                }
            }
        }

        // Split large meshes into submeshes under 65k vertices
        List<List<CombineInstance>> combineLists = new List<List<CombineInstance>>();
        int vertexCount = 0;
        combineLists.Add(new List<CombineInstance>());

        for (int i = 0; i < combine.Count; i++)
        {
            vertexCount += combine[i].mesh.vertexCount;
            if (vertexCount > 65000)
            {
                vertexCount = 0;
                combineLists.Add(new List<CombineInstance>());
                i--;
            }
            else
            {
                combineLists[combineLists.Count - 1].Add(combine[i]);
            }
        }

        // Create combined meshes
        Transform meshysParent = new GameObject("Meshys").transform;
        foreach (var list in combineLists)
        {
            GameObject g = new GameObject("Meshy");
            g.transform.parent = meshysParent;

            MeshFilter mf = g.AddComponent<MeshFilter>();
            MeshRenderer mr = g.AddComponent<MeshRenderer>();
            mr.material = material;

            mf.mesh.CombineMeshes(list.ToArray());
            g.AddComponent<MeshCollider>();
            g.layer = LayerMask.NameToLayer("Ground"); // Temporary layer
        }
    }


    
    private IEnumerator SpawnOresBatched()
    {
        int batchSize = 50; // spawn 50 ores per frame
        int index = 0;

        while (index < blockPositions.Count)
        {
            for (int i = 0; i < batchSize && index < blockPositions.Count; i++, index++)
            {
                if (Random.value < oreChance)
                {
                    GameObject chosenOre = orePrefabs[Random.Range(0, orePrefabs.Length)];
                    NetworkObject oreInstance = Instantiate(chosenOre, blockPositions[index], Quaternion.identity)
                                                .GetComponent<NetworkObject>();

                    oreInstance.Spawn(); // Spawn first
                    oreInstance.name = chosenOre.name; // rename on server

                    // Now safely call the ClientRpc
                    OreNameClientRpc(oreInstance.NetworkObjectId, chosenOre.name);
                }
            }
            yield return null; // wait a frame to avoid flooding the client
        }

        Debug.Log("Finished spawning ores in batches.");
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

    public void ClearCave()
    {
        Transform meshys = GameObject.Find("Meshys")?.transform;
        if (meshys != null)
        {
            DestroyImmediate(meshys.gameObject);
            DestroyImmediate(GameObject.Find("Sphere(Clone)"));
        }
        combine.Clear();
        blockPositions.Clear();
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

}
