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
        Refresh,
        Shove
    }

    public Action action;

    public bool Shoving { get; set; }
    public bool IsAtApex { get; set; }
    private float _shoveTime;
    private Rigidbody _rigidbody;
    private Vector3 _beforePosition;
    private float _speed;
    private bool _apexFrameDelay;

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
        if (!Shoving) return;
        const float time = 0.5f;
        var max = maxSpeed * Time.fixedDeltaTime;
        if (_shoveTime <= 0)
        {
            var trans = transform;
            _beforePosition = trans.position;
            _speed = 0;
        }

        _shoveTime += Time.fixedDeltaTime;

        if (_speed < max) _speed += Time.fixedDeltaTime * Mathf.Pow(_shoveTime / time, 2) * 14;
        if (_speed > max) _speed = max;

        var position = _rigidbody.position;
        IsAtApex = false;
        if (_shoveTime < time)
        {
            _apexFrameDelay = false;
            _rigidbody.MovePosition(position + direction.normalized * _speed);
        }
        else if (_shoveTime > time * 2)
        {
            _rigidbody.MovePosition(Vector3.Lerp(position, _beforePosition, (_shoveTime - time * 2) / time));
        }
        else
        {
            if (!_apexFrameDelay)
                _apexFrameDelay = true;
            else
                IsAtApex = true;
        }

        if (!(_shoveTime > time * 3)) return;
        Shoving = false;
        _shoveTime = 0;
    }

    public void ActivateLaunch()
    {
        if (Shoving) return;
        Shoving = true;

        if (sound != null) sound.Play();
        if (particle != null) particle.Play();
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
            case Action.Shove:
                Shoving = true;
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