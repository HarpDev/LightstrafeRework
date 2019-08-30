using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using UnityEngine;

public class Grapple : MonoBehaviour
{
    public PlayerControls player;
    public LineRenderer rope;

    public float yOffset;

    private Vector3 hookPosition;
    public bool Hooked { get; set; }

    private float radius;

    public float swingForce = 5f;
    public float frictionScale = 0.12f;

    private void Start()
    {
        rope.useWorldSpace = false;
        rope.enabled = false;
    }

    public void Attach(Vector3 point)
    {
        if (!enabled) return;
        var trans = player.camera.transform.position + new Vector3(0, yOffset, 0);
        hookPosition = point;
        radius = Vector3.Distance(hookPosition, trans);
        Hooked = true;
        DoubleJump.doubleJumpSpent = false;
        player.gravityEnabled = false;
        player.movementEnabled = false;
        HandleGrapple(false);
        rope.enabled = true;
    }

    public void Detach()
    {
        if (!enabled) return;
        player.gravityEnabled = true;
        player.movementEnabled = true;
        if (Hooked) Hooked = false;
        if (rope.enabled) rope.enabled = false;
        player.ApplyFriction(frictionScale);
    }

    private void Update()
    {
        if (Hooked)
        {
            player.velocity += swingForce * Time.deltaTime * player.velocity.normalized;
            var list = new List<Vector3>
                {new Vector3(0, yOffset, 0), player.transform.InverseTransformPoint(hookPosition)};

            rope.positionCount = list.Count;
            rope.SetPositions(list.ToArray());
        }
    }

    public void HandleGrapple(bool smooth)
    {
        if (!Hooked) return;
        var trans = player.transform.position;
        var transform1 = player.camera.transform;
        transform1.localPosition = new Vector3();

        var difference = trans + player.velocity - hookPosition;

        var r = difference.magnitude;
        var t = Mathf.Acos(difference.y / r);
        var p = Mathf.Atan2(difference.z, difference.x);

        if (r < radius) radius = r;

        var x = radius * Mathf.Sin(t) * Mathf.Cos(p);
        var y = radius * Mathf.Cos(t);
        var z = radius * Mathf.Sin(t) * Mathf.Sin(p);

        var lookDir = hookPosition + new Vector3(x, y, z) - trans;
        var look = Vector3.RotateTowards(player.velocity, lookDir, 90 * Mathf.Deg2Rad, 0.0f);

        player.velocity = smooth ? look : lookDir;
    }
}