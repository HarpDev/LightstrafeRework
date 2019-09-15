using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaunchBlock : MonoBehaviour
{
    public bool Shoving { get; set; }
    public bool IsAtApex { get; set; }

    public Transform to;

    private float _shoveTime;
    private Rigidbody _rigidbody;
    public AudioSource sound;
    private Vector3 _beforePosition;
    private float _speed;

    public Vector3 Direction
    {
        get { return (to.position - gameObject.transform.position).normalized; }
    }

    public float maxSpeed = 30;

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void ActivateLaunch()
    {
        if (Shoving) return;
        Shoving = true;

        if (sound != null) sound.Play();
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
            _rigidbody.MovePosition(position + Direction.normalized * _speed);
        }
        else if (_shoveTime > time * 2)
        {
            _rigidbody.MovePosition(Vector3.Lerp(position, _beforePosition, (_shoveTime - time * 2) / time));
        }
        else
        {
            IsAtApex = true;
        }

        if (!(_shoveTime > time * 3)) return;
        Shoving = false;
        _shoveTime = 0;
    }
}