using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrow : MonoBehaviour
{
    public bool Fired { get; set; }
    public bool Hit { get; set; }

    public GameObject model;

    public Vector3 beforeFiredRotation = new Vector3(90, 0, 0);
    public Vector3 afterFiredRotation;

    public ParticleSystem explodeParticle;
    public AudioSource explodeSound;

    private const float radius = 15f;
    private const float power = 15f;

    public Transform nockPosition;
    public new Rigidbody rigidbody;
    public TrailRenderer trail;

    public float FiredVelocity { get; set; }
    public bool HasExploded { get; set; }

    private Transform _hitTransform;

    private void Update()
    {
        if (Hit)
        {
            return;
        }

        if (Fired && !Hit)
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(rigidbody.velocity),
                Time.deltaTime * 10);
        else if (!Fired && !Hit)
            model.transform.localRotation = Quaternion.Euler(beforeFiredRotation);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Collide(collision);
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

    public void Collide(Collision collision)
    {
        if (collision.collider.CompareTag("Player")) return;
        if (!Fired || Hit) return;
        if (collision.collider.isTrigger) return;

        transform.position = collision.GetContact(0).point;
        Hit = true;
        _hitTransform = collision.transform;
        _hitTransform.hasChanged = false;
        GetComponent<Rigidbody>().isKinematic = true;

        model.SetActive(false);
        var vector = Game.Player.transform.position - transform.position;
        var amount = Mathf.Pow(Mathf.Max(radius - vector.magnitude, 0) / radius, 2);
        //Game.Player.Accelerate(vector.normalized, new PlayerMovement.SpeedAccel(0, amount * power));
        Game.Player.velocity += vector.normalized * amount * power;

        if (explodeSound != null) explodeSound.Play();
        if (explodeParticle != null) explodeParticle.Play();
    }
}