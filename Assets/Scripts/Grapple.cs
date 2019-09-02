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

    public AudioClip attach;
    public AudioSource during;
    public AudioClip release;

    public float yOffset;

    private Vector3 hookPosition;
    public bool Hooked { get; set; }

    public float swingForce = 25f;
    public float detachFrictionScale = 0.12f;
    public float attachFrictionScale = 0.08f;

    public int maxSwingTimeMillis = 5000;

    public int maxDistance = 120;

    public float airAcceleration = 8;

    private int attachTimestamp;

    private void Start()
    {
        rope.useWorldSpace = false;
        rope.enabled = false;
        during.volume = 0;
    }

    private float prevAirAcceleration;

    public void Attach(Vector3 point)
    {
        if (!enabled) return;
        player.source.PlayOneShot(attach);
        Game.I.Hitmarker.Display();
        if (Hooked) return;
        if (Vector3.Distance(point, player.transform.position) > maxDistance) return;
        hookPosition = point;
        Hooked = true;
        prevAirAcceleration = player.airAcceleration;
        player.airAcceleration = airAcceleration;
        player.gravityEnabled = false;
        DoubleJump.doubleJumpSpent = false;
        rope.enabled = true;
        player.ApplyVerticalFriction(attachFrictionScale);
        attachTimestamp = Environment.TickCount;
        during.volume = 1;
        
        var towardPoint = (hookPosition - player.transform.position).normalized;
        var yankProjection = Vector3.Dot(Game.I.Player.velocity, towardPoint);
        Game.I.Player.velocity -= towardPoint * yankProjection;
    }

    public void Detach()
    {
        if (!enabled) return;
        player.airAcceleration = prevAirAcceleration;
        player.gravityEnabled = true;
        if (Hooked) Hooked = false;
        if (rope.enabled) rope.enabled = false;
        player.ApplyVerticalFriction(detachFrictionScale);
        during.volume = 0;
        player.source.PlayOneShot(release);
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

        during.pitch = player.velocity.magnitude / 30f;

        player.CameraRotation = Mathf.Lerp(player.CameraRotation, velocityProjection * value, Time.deltaTime * 10);

        var yankProjection = Vector3.Dot(Game.I.Player.velocity, towardPoint);
        if (yankProjection < 0) Game.I.Player.velocity -= towardPoint * yankProjection;
        
        var returnScale = Mathf.Pow(player.velocity.magnitude / 15f + 0.5f, -2) + 0.9f;

        player.velocity += returnScale * swingForce * Time.deltaTime * towardPoint;
        player.velocity += returnScale * (swingForce / 3) * Time.deltaTime * player.velocity.normalized;
    }
}