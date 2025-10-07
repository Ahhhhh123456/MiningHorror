using UnityEngine;
using System.Collections.Generic;

public class CaveGeneration : MonoBehaviour
{
    public GameObject blockPrefab;
    public Material material;

    private float noiseScale = .05f;
    private int caveWidth = 200;
    private int caveHeight = 20;
    private int caveDepth = 200;
    
    private List<CombineInstance> combine = new List<CombineInstance>();

    void Start()
    {
        CreateCave();
    }

    public void CreateCave()
    {


        MeshFilter blockMesh = Instantiate(blockPrefab, Vector3.zero, Quaternion.identity).GetComponent<MeshFilter>();

        //float offset = Random.Range(0f, 1000f);
        int offset = 996;
        Debug.Log("Offset: " + offset);

        for (int x = 0; x < caveWidth; x++)
        {
            for (int y = 0; y < caveHeight; y++)
            {
                for (int z = 0; z < caveDepth; z++)
                {
                    float noiseValue = Perlin3D((x + offset) * noiseScale / 2, (y + offset) * noiseScale, (z + offset) * noiseScale / 2);
                    if ((noiseValue < 0.45 || noiseValue > 0.55) || (y == 0 || y == caveHeight - 1) || (x == 0 || x == caveWidth - 1) || (z == 0 || z == caveDepth - 1))
                    {
                        blockMesh.transform.position = new Vector3(x, y, z);
                        combine.Add(new CombineInstance
                        {
                            mesh = blockMesh.sharedMesh,
                            transform = blockMesh.transform.localToWorldMatrix
                        });
                    }
                }
            }
        }

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

        Transform meshys = new GameObject("Meshys").transform;
        foreach (List<CombineInstance> list in combineLists)
        {
            GameObject g = new GameObject("Meshy");
            g.transform.parent = meshys;
            MeshFilter mf = g.AddComponent<MeshFilter>();
            MeshRenderer mr = g.AddComponent<MeshRenderer>();
            mr.material = material;
            mf.mesh.CombineMeshes(list.ToArray());
            g.AddComponent<MeshCollider>();
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
    }

    public static float Perlin3D(float x, float y, float z)
    {
        float ab = Mathf.PerlinNoise(x, y);
        float bc = Mathf.PerlinNoise(y, z);
        float ac = Mathf.PerlinNoise(x, z);

        float ba = Mathf.PerlinNoise(y, x);
        float cb = Mathf.PerlinNoise(z, y);
        float ca = Mathf.PerlinNoise(z, x);

        float abc = ab + bc + ac + ba + cb + ca;
        return abc / 6f; // Average the values to get a smoother result

    }
}
