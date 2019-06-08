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

    public Transform nockPosition;
    public new Rigidbody rigidbody;
    public TrailRenderer trail;
    
    public Hitmarker hitmarker { get; set; }

    private void Start()
    {
        trail.enabled = false;
    }

    private void Update()
    {
        if (Fired && !Hit)
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(rigidbody.velocity),
                Time.deltaTime * 10);
        else if (!Fired && !Hit)
            model.transform.localRotation = Quaternion.Euler(beforeFiredRotation);
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
    
    public delegate void ArrowCollide(RaycastHit hit);

    public event ArrowCollide OnArrowCollide;

    public void Collide(RaycastHit hit)
    {
        if (!Fired) return;
        if (OnArrowCollide != null) OnArrowCollide(hit);
        transform.position = hit.point;
        Hit = true;
        GetComponent<Rigidbody>().isKinematic = true;
        if (hit.collider.CompareTag("Target"))
        {
            if (hitmarker != null)
                hitmarker.Hit();
            var target = hit.collider.gameObject.GetComponent<Target>();
            Destroy(target.targetObject);
            Game.I.Score += Mathf.RoundToInt(target.value * Game.I.Player.velocity.magnitude);
        }
    }
}
