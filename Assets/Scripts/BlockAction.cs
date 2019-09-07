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

    public Vector3 move;
    public Vector3 rotate;
    public float time;

    public enum Action
    {
        Blast,
        Grapple,
        Refresh,
        Shove
    }

    public Action action;

    private bool _shoving;
    private float _shoveTime;
    private Rigidbody _rigidbody;
    private Vector3 _beforePosition;
    private Vector3 _beforeRotation;

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (doesRotate) gameObject.transform.Rotate(15 * Time.deltaTime, 18 * Time.deltaTime, 14 * Time.deltaTime);
    }

    private void FixedUpdate()
    {
        if (_shoving)
        {
            if (_shoveTime <= 0)
            {
                var trans = transform;
                _beforePosition = trans.position;
                _beforeRotation = trans.rotation.eulerAngles;
            }

            _shoveTime += Time.fixedDeltaTime;
            if (_shoveTime > time * 2)
            {
                _shoving = false;
                _shoveTime = 0;
            }

            var correctedTime = _shoveTime / time;

            var factor = correctedTime * correctedTime * correctedTime * correctedTime * correctedTime;
            if (_shoveTime > time) factor = 1 - (_shoveTime - time) / time;

            var toPosition = Vector3.Lerp(_beforePosition, _beforePosition + move, factor);
            var toRotation = Vector3.Lerp(_beforeRotation, _beforeRotation + rotate, factor);

            _rigidbody.MoveRotation(Quaternion.Euler(toRotation));
            _rigidbody.MovePosition(toPosition);
        }
    }

    public void ActivateLaunch()
    {
        _shoving = true;
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
                Game.I.Player.AttachGrapple(hit.point);
                break;
            case Action.Shove:
                ActivateLaunch();
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