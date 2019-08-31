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

    public float swingForce = 35f;
    public float detachFrictionScale = 0.12f;
    public float attachFrictionScale = 0.25f;

    public int maxSwingTimeMillis = 4000;

    public int maxDistance = 200;

    private int attachTimestamp;

    private void Start()
    {
        rope.useWorldSpace = false;
        rope.enabled = false;
    }

    public void Attach(Vector3 point)
    {
        if (!enabled) return;
        if (Vector3.Distance(point, player.transform.position) > maxDistance) return;
        hookPosition = point;
        Hooked = true;
        player.movementEnabled = false;
        player.gravityEnabled = false;
        DoubleJump.doubleJumpSpent = false;
        rope.enabled = true;
        player.ApplyVerticalFriction(attachFrictionScale);
        attachTimestamp = Environment.TickCount;
    }

    public void Detach()
    {
        if (!enabled) return;
        player.movementEnabled = true;
        player.gravityEnabled = true;
        if (Hooked) Hooked = false;
        if (rope.enabled) rope.enabled = false;
        player.ApplyVerticalFriction(detachFrictionScale);
    }

    private void Update()
    {
        if (!Hooked) return;

        var list = new List<Vector3>
            {new Vector3(0, yOffset, 0), player.transform.InverseTransformPoint(hookPosition)};

        rope.positionCount = list.Count;
        rope.SetPositions(list.ToArray());

        if (Environment.TickCount - attachTimestamp > maxSwingTimeMillis) Detach();

        var trans = player.transform.position;
        var camTrans = player.camera.transform;
        camTrans.localPosition = new Vector3();

        var towardPoint = (hookPosition - trans).normalized;

        var forward = camTrans.forward;
        var projection = Vector3.Dot(forward, towardPoint);
        if (projection < 0) Detach();

        var velocityProjection = Mathf.Abs(Vector3.Dot(player.velocity, camTrans.right));

        var relativePoint = player.transform.InverseTransformPoint(hookPosition);

        var value = Mathf.Atan2(relativePoint.z, relativePoint.x) * Mathf.Rad2Deg;
        value = Mathf.Abs(value);
        value -= 90;
        value /= 90;

        player.CameraRotation = Mathf.Lerp(player.CameraRotation, velocityProjection * value, Time.deltaTime * 10);

        player.velocity += swingForce * Time.deltaTime * towardPoint;
        player.velocity += swingForce / 3 * Time.deltaTime * forward;
    }
}