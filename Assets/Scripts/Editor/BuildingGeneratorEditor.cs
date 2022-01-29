using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BuildingGenerator))]
public class BuildingGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        BuildingGenerator generator = (BuildingGenerator) target;
        if (GUILayout.Button("Build"))
        {
            generator.Build();
        }
    }
    
}
