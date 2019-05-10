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
    private bool hooked;

    private float radius;

    private void Start()
    {
        rope.useWorldSpace = false;
    }

    private void Update()
    {
        var trans = player.camera.transform.position + new Vector3(0, yOffset, 0);
        if (Input.GetAxis("Grapple") > 0)
        {
            if (!rope.enabled)
            {
                hooked = false;
                RaycastHit hit;
                Physics.Raycast(trans, player.camera.transform.forward, out hit, 100);
                if (hit.collider != null && !hit.collider.CompareTag("Ground"))
                {
                    hookPosition = hit.point;
                    radius = Vector3.Distance(hookPosition, trans);
                    hooked = true;
                }

                if (hooked) rope.enabled = true;
            }

            var list = new List<Vector3> {new Vector3(0, yOffset, 0), player.transform.InverseTransformPoint(hookPosition)};

            rope.positionCount = list.Count;
            rope.SetPositions(list.ToArray());
        }
        else
        {
            if (hooked) hooked = false;
            if (rope.enabled) rope.enabled = false;
        }
    }

    public void HandleGrapple()
    {
        if (!hooked) return;
        var trans = player.transform.position;
        
        var lookHook = Vector3.RotateTowards(new Vector3(1, 0, 0), hookPosition - trans, 200 * Mathf.Deg2Rad, 0.0f);
        player.velocity += lookHook * Time.deltaTime * 250 / player.velocity.magnitude;

        var difference2 = trans + player.velocity - hookPosition;

        var r2 = difference2.magnitude;
        var t2 = Mathf.Acos(difference2.y / r2);
        var p2 = Mathf.Atan2(difference2.z, difference2.x);

        if (r2 < radius) radius = r2;

        var x = radius * Mathf.Sin(t2) * Mathf.Cos(p2);
        var y = radius * Mathf.Cos(t2);
        var z = radius * Mathf.Sin(t2) * Mathf.Sin(p2);

        var lookDir = hookPosition + new Vector3(x, y, z) - trans;
        var look = Vector3.RotateTowards(player.velocity, lookDir, 90 * Mathf.Deg2Rad * Time.deltaTime, 0.0f);

        player.velocity = look;
    }
}