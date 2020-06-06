using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
public class MirrorFlipCamera : MonoBehaviour
{
    new Camera camera;
    void Awake()
    {
        camera = GetComponent<Camera>();
    }
    void OnPreCull()
    {
        camera.ResetWorldToCameraMatrix();
        camera.ResetProjectionMatrix();
        camera.projectionMatrix *= Matrix4x4.Scale(new Vector3(-1, 1, 1));
    }
    void OnPreRender()
    {
        GL.invertCulling = true;
    }

    void OnPostRender()
    {
        GL.invertCulling = false;
    }
}
