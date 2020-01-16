using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Torec;

public class MeshSmoother : MonoBehaviour
{

    public int iterations = 2;

    private void Start()
    {
        var meshfilter = gameObject.GetComponentInChildren<MeshFilter>();
        var sub = CatmullClark.Subdivide(meshfilter.mesh, iterations);
        meshfilter.mesh = sub;
        var meshCollider = gameObject.GetComponentInChildren<MeshCollider>();
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = sub;
        }
    }
}
