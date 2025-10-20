using UnityEngine;
using UnityEditor;

//[CustomEditor(typeof(CaveGeneration))]

[CustomEditor(typeof(MarchingCubes))]
public class ScriptEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        //CaveGeneration myScript = (CaveGeneration)target;

        MarchingCubes myScript = (MarchingCubes)target;
        if (GUILayout.Button("Create Cave"))
        {
            myScript.CreateCave();
        }

        if (GUILayout.Button("Clear Cave"))
        {
            myScript.ClearCave();
        }
    }
}
