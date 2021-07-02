using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightProjectile : MonoBehaviour
{

    //private TrailRenderer _trail;
    private LocalTrailRenderer _trail;

    private float _progress;
    private Vector3 _startingLocation;

    public Transform Target { get; set; }

    private float _frequency;
    private float _rotations;

    private float _startingDistance;
    private Vector3 _originalStartingLocation;

    private bool _finished;

    private float _radius = 1;
    public float AnimTime = 0.9f;


    private void Start()
    {
        //_trail = GetComponent<TrailRenderer>();
        //_max = _trail.startWidth;
        _trail = GetComponent<LocalTrailRenderer>();

        _frequency = Random.Range(2, 6);
        _rotations = Random.Range(0.5f, 4) * (Random.Range(1, 3) > 1 ? 1 : -1);
        _startingLocation = transform.position;
        _originalStartingLocation = transform.position;
        _startingDistance = Mathf.Max((_startingLocation - Target.position).magnitude, 20);
    }

    private void LateUpdate()
    {
        if (_finished)
        {
            return;
        }
        _progress += Time.deltaTime;
        //_trail.startWidth = Mathf.Min((transform.position - Target.position).magnitude / 100f + 0.05f, _max);

        float factor = Mathf.Min(_progress / AnimTime, 1);

        _startingLocation = Vector3.Lerp(_originalStartingLocation, Target.position + Target.forward * _startingDistance, factor);

        var position = Vector3.Lerp(_startingLocation, Target.position, factor);

        float yaw = 0;
        float pitch;

        float dx = _startingLocation.x - Target.position.x;
        float dy = _startingLocation.y - Target.position.y;
        float dz = _startingLocation.z - Target.position.z;

        if (dx != 0)
        {
            if (dx < 0)
            {
                yaw = 1.5f * Mathf.PI;
            } else
            {
                yaw = 0.5f * Mathf.PI;
            }
            yaw -= Mathf.Atan(dz / dx);
        } else if (dz < 0)
        {
            yaw = Mathf.PI;
        }

        float dxz = Mathf.Sqrt(Mathf.Pow(dx, 2) + Mathf.Pow(dz, 2));

        pitch = -Mathf.Atan(dy / dxz);

        var r = ((Target.position - _startingLocation).magnitude / 10) * _radius;
        Vector3 start = new Vector3(r, 0, 0);

        start = rotateAroundY(start, Mathf.Deg2Rad * factor * 360 * _rotations);
        start = rotateAroundZ(start, pitch + (Mathf.Deg2Rad * 90));
        start = rotateAroundY(start, -yaw + (Mathf.Deg2Rad * 90));

        var easeIn = Mathf.Sin((factor * 2 * Mathf.PI) / 2);
        var easeOut = 1 - Mathf.Pow(factor * 2 - 1, 5);

        var ease = factor > 0.5f ? easeOut : easeIn;

        position += start * Mathf.Sin(factor * _frequency) * ease;

        transform.position = Vector3.Lerp(transform.position, position, Time.deltaTime * 50);

        if (factor == 1)
        {
            transform.position = position;
            transform.parent = Target;
            _trail.Lock();
            Destroy(gameObject, _trail.life);
            _finished = true;
        }
    }

    private Vector3 rotateAroundZ(Vector3 start, float t)
    {
        float x = start.x * Mathf.Cos(t) - start.y * Mathf.Sin(t);
        float y = start.x * Mathf.Sin(t) + start.y * Mathf.Cos(t);
        return new Vector3(x, y, start.z);
    }

    private Vector3 rotateAroundY(Vector3 start, float t)
    {
        float x = start.x * Mathf.Cos(t) + start.z * Mathf.Sin(t);
        float z = -start.x * Mathf.Sin(t) + start.z * Mathf.Cos(t);
        return new Vector3(x, start.y, z);
    }

    private Vector3 rotateAroundX(Vector3 start, float t)
    {
        float y = start.y * Mathf.Cos(t) - start.z * Mathf.Sin(t);
        float z = start.y * Mathf.Sin(t) + start.z * Mathf.Cos(t);
        return new Vector3(start.x, y, z);
    }
}
