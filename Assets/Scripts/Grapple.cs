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
        var trans = player.cameraHudMovement.camera.transform.position + new Vector3(0, yOffset, 0);
        if (Input.GetAxis("Grapple") > 0)
        {
            if (!rope.enabled)
            {
                hooked = false;
                RaycastHit hit;
                Physics.Raycast(trans, player.cameraHudMovement.camera.transform.forward, out hit, 100);
                if (hit.collider != null && !hit.collider.CompareTag("Ground"))
                {
                    hookPosition = hit.point;
                    radius = Vector3.Distance(hookPosition, trans);
                    hooked = true;
                    player.RefreshDoubleJump();
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
        player.cameraHudMovement.camera.transform.localPosition = new Vector3();

        var difference = trans + player.velocity - hookPosition;

        var r = difference.magnitude;
        var t = Mathf.Acos(difference.y / r);
        var p = Mathf.Atan2(difference.z, difference.x);

        if (r < radius) radius = r;

        var x = radius * Mathf.Sin(t) * Mathf.Cos(p);
        var y = radius * Mathf.Cos(t);
        var z = radius * Mathf.Sin(t) * Mathf.Sin(p);

        var lookDir = hookPosition + new Vector3(x, y, z) - trans;
        var look = Vector3.RotateTowards(player.velocity, lookDir, 90 * Mathf.Deg2Rad * Time.deltaTime, 0.0f);

        player.velocity = look;
    }
}