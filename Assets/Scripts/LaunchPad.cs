using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaunchPad : MonoBehaviour
{

    private MeshRenderer mesh;

    public float force;

    private void Awake()
    {
        mesh = GetComponent<MeshRenderer>();
    }

    private void FixedUpdate()
    {
        var material = mesh.material;
        material.mainTextureOffset = new Vector2(0, material.mainTextureOffset.y % 1 -0.03f);
    }

    public void Launch()
    {
        Game.I.Player.velocity += transform.forward * force;
    }
}
