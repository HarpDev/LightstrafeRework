using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrow : MonoBehaviour
{
    public bool Fired { get; set; }
    public bool Hit { get; set; }

    public GameObject model;

    public new SphereCollider collider;

    public MeshRenderer radiusIndicator;

    public Vector3 beforeFiredRotation = new Vector3(90, 0, 0);
    public Vector3 afterFiredRotation;

    public ParticleSystem explodeParticle;
    public AudioSource explodeSound;

    public float radius = 8f;

    public Transform nockPosition;
    public new Rigidbody rigidbody;
    public TrailRenderer trail;

    public float FiredVelocity { get; set; }
    public bool HasExploded { get; set; }

    private Vector3 _prevPosition;
    private Transform _hitTransform;

    private void Start()
    {
        _prevPosition = transform.position;
        radiusIndicator.transform.localScale = new Vector3(radius * 2, radius * 2, radius * 2);
        collider.radius = radius;
        radiusIndicator.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Hit)
        {
            if (_hitTransform.hasChanged) model.GetComponent<MeshRenderer>().enabled = false;
            return;
        }

        if (Fired && !Hit)
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(rigidbody.velocity),
                Time.deltaTime * 10);
        else if (!Fired && !Hit)
            model.transform.localRotation = Quaternion.Euler(beforeFiredRotation);

        var trans = transform;
        var lookDir = trans.position - _prevPosition;
        RaycastHit hit;
        var layermask = ~(1 << 9);
        Physics.Raycast(_prevPosition, lookDir.normalized, out hit, lookDir.magnitude, layermask);
        if (hit.collider != null) Collide(hit);

        _prevPosition = transform.position;
    }

    public void Fire(Quaternion direction, Vector3 velocity)
    {
        Fired = true;
        trail.enabled = true;
        model.transform.localRotation = Quaternion.Euler(afterFiredRotation);
        transform.rotation = direction;
        rigidbody.velocity = velocity;
        rigidbody.isKinematic = false;
    }

    public void Explode()
    {
        if (!Hit || HasExploded) return;
        collider.enabled = false;
        var o = gameObject;
        HasExploded = true;

        var lookat = (Game.I.Player.transform.position - o.transform.position).normalized;

        var y = lookat.y > 0 ? 1 : -1;
        Game.I.Player.Accelerate(new Vector3(0, y, 0), 20, 20);

        model.SetActive(false);
        radiusIndicator.gameObject.SetActive(false);

        if (explodeSound != null) explodeSound.Play();
        if (explodeParticle != null) explodeParticle.Play();
    }

    public void Collide(RaycastHit hit)
    {
        if (!Fired) return;
        radiusIndicator.gameObject.SetActive(true);
        transform.position = hit.point;
        Hit = true;
        _hitTransform = hit.transform;
        _hitTransform.hasChanged = false;
        GetComponent<Rigidbody>().isKinematic = true;
        var action = hit.collider.gameObject.GetComponent<BlockAction>();
        if (action != null) action.Hit(hit);
        var kill = hit.collider.gameObject.GetComponent<KillBlock>();
        if (kill != null) kill.Hit();
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}