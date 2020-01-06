using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PipeSystem))]
public class PipeSystemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        PipeSystem pipeSystem = (PipeSystem)target;
        if (GUILayout.Button("Regenerate Pipe"))
        {
            pipeSystem.Generate();
        }
    }
}
