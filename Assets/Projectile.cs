using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{

    public Vector3 velocity;

    private const float basespeed = 40;

    public ParticleSystem explodeParticle;
    public ParticleSystem trailParticle;
    public AudioSource explodeSound;

    public GameObject visual;

    private const float radius = 15f;
    private const float power = 15f;

    private const float delay = 0.3f;

    private Rigidbody _rigidbody;

    private bool _hit;

    private Target _hitTarget;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();

        trailParticle.Play();
    }

    public void Fire(Vector3 vel, Vector3 realPosition, Vector3 visualPosition)
    {
        transform.position = realPosition;
        visual.transform.position = visualPosition;
        velocity = vel;
        velocity += velocity.normalized * basespeed;
    }

    private void Update()
    {
        if (_hit) return;

        _rigidbody.velocity = velocity;

        visual.transform.position = Vector3.Lerp(visual.transform.position, transform.position, Time.deltaTime * 8);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.collider.CompareTag("Player")) return;
        if (collision.collider.isTrigger) return;

        _hit = true;
        visual.transform.position = transform.position;

        _rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
        _rigidbody.isKinematic = true;
        _rigidbody.velocity = new Vector3();

        _hitTarget = collision.collider.gameObject.GetComponent<Target>();

        Invoke("Explode", delay);
    }

    private void Explode()
    {
        if (_hitTarget != null)
        {
            _hitTarget.Explode(_rigidbody.velocity.normalized);
        }

        var vector = Game.Level.player.transform.position - transform.position;
        var amount = Mathf.Pow(Mathf.Max(radius - vector.magnitude, 0) / radius, 2);

        var direction = vector.normalized;

        if (Game.Level.player.IsGrounded)
        {
            var ground = Game.Level.player.GroundNormal;
            var projection = Vector3.Dot(direction, ground);
            if (projection > 0) direction -= ground * projection;
        }

        Game.Level.player.Accelerate(direction, 0, amount * power);
        Game.Level.player.velocity += direction * amount * power;

        if (explodeSound != null) explodeSound.Play();
        if (explodeParticle != null) explodeParticle.Play();

        trailParticle.Stop();

        visual.GetComponent<MeshRenderer>().enabled = false;
        GetComponent<SphereCollider>().enabled = false;
    }
}
