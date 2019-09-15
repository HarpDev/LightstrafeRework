using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockAction : MonoBehaviour
{
    public ParticleSystem particle;
    public bool doesRotate;
    public bool breakOnHit;
    public AudioSource sound;

    public Vector3 direction;
    public float maxSpeed = 30;

    public enum Action
    {
        Blast,
        Grapple,
        Refresh
    }

    public Action action;

    public bool IsAtApex { get; set; }
    private Vector3 _beforePosition;
    private float _speed;
    private bool _apexFrameDelay;

    private void Update()
    {
        if (doesRotate) gameObject.transform.Rotate(15 * Time.deltaTime, 18 * Time.deltaTime, 14 * Time.deltaTime);
    }

    public void Hit(RaycastHit hit)
    {
        switch (action)
        {
            case Action.Blast:
                Blast();
                break;
            case Action.Refresh:
                PlayerMovement.DoubleJumpAvailable = true;
                break;
            case Action.Grapple:
                Game.I.Player.AttachGrapple(hit.transform);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (sound != null) sound.Play();
        if (particle != null) particle.Play();

        if (breakOnHit)
        {
            var o = gameObject;

            o.GetComponent<MeshRenderer>().enabled = false;
            o.GetComponent<BoxCollider>().enabled = false;

            Invoke("Respawn", 3f);
        }
    }

    public void Blast()
    {
        Game.I.Hitmarker.Display();
        var o = gameObject;

        var lookat = (Game.I.Player.transform.position - o.transform.position).normalized;
        lookat *= 2;
        lookat.x = Mathf.RoundToInt(lookat.x);
        lookat.y = Mathf.RoundToInt(lookat.y);
        lookat.z = Mathf.RoundToInt(lookat.z);
        lookat /= 2;

        Game.I.Player.Accelerate(lookat.normalized, 20, 20);

        PlayerMovement.DoubleJumpAvailable = true;
    }

    public void Respawn()
    {
        var o = gameObject;
        o.GetComponent<MeshRenderer>().enabled = true;
        o.GetComponent<BoxCollider>().enabled = true;
    }
}