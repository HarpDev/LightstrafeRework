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

    public float radius = 2.5f;
    public float power = 20f;

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
        Physics.Raycast(_prevPosition, lookDir.normalized, out hit, lookDir.magnitude);
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
        HasExploded = true;

        Game.I.Player.Accelerate(Vector3.up, power, power);
        Time.timeScale = 0.1f;

        model.SetActive(false);
        radiusIndicator.gameObject.SetActive(false);

        if (explodeSound != null) explodeSound.Play();
        if (explodeParticle != null) explodeParticle.Play();
    }

    public void Collide(RaycastHit hit)
    {
        if (hit.collider.CompareTag("Player")) return;
        if (!Fired) return;
        if (hit.collider.isTrigger)
        {
            if (hit.collider.CompareTag("Rail"))
            {
                Game.I.Player.AttachGrapple(hit.transform);
                Destroy(gameObject);
            }
            return;
        }
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