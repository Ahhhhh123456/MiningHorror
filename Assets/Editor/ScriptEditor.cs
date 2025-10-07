using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CaveGeneration))]
public class ScriptEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CaveGeneration myScript = (CaveGeneration)target;
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
