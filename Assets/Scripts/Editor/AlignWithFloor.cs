using UnityEngine;
using UnityEditor;

public class AlignWithFloor : EditorWindow
{

    [MenuItem("Tools/Align With Floor")]
    static void CreateAlignWithFloor()
    {
        GetWindow<AlignWithFloor>();
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Move"))
        {
            var selection = Selection.gameObjects;

            var lowestY = float.MaxValue;

            for (var i = selection.Length - 1; i >= 0; --i)
            {
                var selected = selection[i];
                if (selected.transform.position.y < lowestY) lowestY = selected.transform.position.y;
            }

            var margin = 15f;

            for (var i = selection.Length - 1; i >= 0; --i)
            {
                var selected = selection[i];

                selected.transform.position += Vector3.down * lowestY;
                selected.transform.position += Vector3.up * margin;
            }
        }

        GUI.enabled = false;
    }
}