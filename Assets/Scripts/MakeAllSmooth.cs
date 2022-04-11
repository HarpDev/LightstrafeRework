using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MakeAllSmooth : MonoBehaviour
{
    private void Start()
    {
        foreach(var mesh in FindObjectsOfType<MeshRenderer>())
        {
            foreach (var mat in mesh.materials)
            {
                mat.SetFloat("_Smoothness", 0.9f);
            }
        }
    }
}
