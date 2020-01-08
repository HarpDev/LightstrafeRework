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
        Physics.Raycast(_prevPosition - lookDir, lookDir.normalized, out hit, lookDir.magnitude * 2);
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
        HasExploded = true;

        //Game.Level.player.Accelerate(Vector3.up, power, power);
        Time.timeScale = 0.1f;

        model.SetActive(false);

        if (explodeSound != null) explodeSound.Play();
        if (explodeParticle != null) explodeParticle.Play();
    }

    public void Collide(RaycastHit hit)
    {
        if (hit.collider.CompareTag("Player")) return;
        if (!Fired) return;
        if (hit.collider.isTrigger) return;
        //radiusIndicator.gameObject.SetActive(true);
        transform.position = hit.point;
        Hit = true;
        _hitTransform = hit.transform;
        _hitTransform.hasChanged = false;
        GetComponent<Rigidbody>().isKinematic = true;
        var kill = hit.collider.gameObject.GetComponent<KillBlock>();
        if (kill != null) kill.Hit();

        /*model.SetActive(false);
        var vector = (Game.I.Player.transform.position - hit.point).normalized;
        vector.x = Mathf.Round(vector.x);
        vector.y = Mathf.Round(vector.y);
        vector.z = Mathf.Round(vector.z);
        Game.I.Player.Accelerate(vector.normalized, power, power);
        if (explodeSound != null) explodeSound.Play();
        if (explodeParticle != null) explodeParticle.Play();*/
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}