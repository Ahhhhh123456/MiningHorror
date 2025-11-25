using System.Collections.Generic;
using UnityEngine;

public static class MeshUtils
{
    // STEP 1: Find open edges / boundary loops
    public static List<List<int>> FindBoundaryLoops(Mesh mesh)
    {
        var tris = mesh.triangles;

        Dictionary<(int,int), List<int>> edgeDict = new Dictionary<(int,int), List<int>>();

        for (int i = 0; i < tris.Length; i += 3)
        {
            int a = tris[i];
            int b = tris[i + 1];
            int c = tris[i + 2];

            void AddEdge(int v1, int v2)
            {
                var key = v1 < v2 ? (v1,v2) : (v2,v1);
                if (!edgeDict.ContainsKey(key)) edgeDict[key] = new List<int>();
                edgeDict[key].Add(i/3);
            }

            AddEdge(a,b);
            AddEdge(b,c);
            AddEdge(c,a);
        }

        List<(int,int)> boundaryEdges = new List<(int,int)>();
        foreach (var kvp in edgeDict)
            if (kvp.Value.Count == 1)
                boundaryEdges.Add(kvp.Key);

        List<List<int>> loops = new List<List<int>>();
        foreach (var edge in boundaryEdges)
            loops.Add(new List<int>{ edge.Item1, edge.Item2 });

        return loops;
    }

    // STEP 2: Fill holes using the boundary loops
    public static void FillSmallHoles(Mesh mesh)
    {
        var verts = new List<Vector3>(mesh.vertices);
        var tris = new List<int>(mesh.triangles);

        List<List<int>> loops = FindBoundaryLoops(mesh);

        foreach (var loop in loops)
        {
            Vector3 centroid = Vector3.zero;
            foreach (int idx in loop)
                centroid += verts[idx];
            centroid /= loop.Count;

            int centroidIndex = verts.Count;
            verts.Add(centroid);

            for (int i = 0; i < loop.Count; i++)
            {
                int a = loop[i];
                int b = loop[(i + 1) % loop.Count];

                tris.Add(a);
                tris.Add(b);
                tris.Add(centroidIndex);
            }
        }

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}
