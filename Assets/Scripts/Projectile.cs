using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{

    public Vector3 velocity;

    private const float basespeed = 200;

    public ParticleSystem explodeParticle;
    public AudioSource explodeSound;

    public GameObject visual;

    public Radial radial;

    private const float radius = 10f;
    private const float power = 15f;
    private const float drop = 20f;

    private const float delay = 0.3f;

    private Rigidbody _rigidbody;

    private bool _hit;
    private bool _exploded;

    private Target _hitTarget;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void Fire(Vector3 vel, Vector3 realPosition, Vector3 visualPosition)
    {
        transform.position = realPosition;
        visual.transform.position = visualPosition;
        velocity = vel;
        velocity += velocity.normalized * basespeed;
        visual.transform.rotation = Quaternion.LookRotation(-velocity);
    }

    private void Update()
    {
        if (_exploded) return;
        if (_hit) return;

        velocity += Vector3.down * Time.deltaTime * drop;
        _rigidbody.velocity = velocity;

        visual.transform.rotation = Quaternion.Lerp(visual.transform.rotation, Quaternion.LookRotation(-velocity), Time.deltaTime * 10);
        visual.transform.position = Vector3.Lerp(visual.transform.position, transform.position, Time.deltaTime * 8);
    }

    private void FixedUpdate()
    {
        if (_exploded || !_hit) return;
        var toPlayer = transform.position - Game.Level.player.transform.position;
        if (toPlayer.magnitude < radius && toPlayer.magnitude > 3 && Vector3.Dot(Flatten(toPlayer).normalized, Flatten(Game.Level.player.velocity).normalized) < -0.7f)
        {
            //Explode();
        }
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
        if (_hitTarget != null) Game.Level.hitmarker.Display();

        if (collision.collider.CompareTag("Target"))
        {
            _hitTarget.Explode();
            visual.GetComponent<MeshRenderer>().enabled = false;
            GetComponent<SphereCollider>().enabled = false;
        }
    }

    private void Explode()
    {
        if (_exploded) return;
        _exploded = true;

        if (_hitTarget != null)
        {
            _hitTarget.Explode();
            return;
        }

        var vector = Game.Level.player.transform.position - transform.position;
        if (vector.magnitude < radius)
        {
            vector = Flatten(vector);
            var amount = Mathf.Pow(Mathf.Max(radius - vector.magnitude, 0) / radius, 2);
            if (amount > 0) amount = Mathf.Min(1, amount + 0.5f);

            var direction = vector.normalized;

            Game.Level.player.Accelerate(direction, 0, amount * power);
            Game.Level.player.velocity += direction * amount * (power / 2);
            if (amount > 0)
            {
                var r = Instantiate(radial, Game.Canvas.transform).GetComponent<Radial>();
                if (amount < 1)
                {
                    r.Image.color = Game.green;
                }
                else
                {
                    r.Image.color = Game.gold;
                }
                r.size = amount * 2;
                var flat = Flatten(direction).normalized;
                var angle = Mathf.Atan2(flat.z, flat.x);
                r.position = angle * Mathf.Rad2Deg + (Game.Level.player.Yaw + 90);
            }
        }

        if (explodeSound != null) explodeSound.Play();
        if (explodeParticle != null) explodeParticle.Play();
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}
