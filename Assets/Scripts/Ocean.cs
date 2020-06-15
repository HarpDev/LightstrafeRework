using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class Ocean : MonoBehaviour
{

    private MeshRenderer _meshRenderer;

    private void Start()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
    }

    private void Update()
    {
        var position = transform.position;
        position.x = Game.Player.camera.transform.position.x;
        position.z = Game.Player.camera.transform.position.z;
        transform.position = position;

        if (Application.isPlaying)
        {
            _meshRenderer.material.SetTextureOffset("_MainTex", new Vector2(position.x / transform.localScale.x, position.z / transform.localScale.z));
        }
    }
}
