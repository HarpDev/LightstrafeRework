using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Goo : MonoBehaviour
{

    private ParticleSystem particle;
    private Renderer modelRenderer;

    public float globPerVolume = 1;
    public int health = 2;

    private float controlTime;

    private void Start()
    {
        particle = GetComponent<ParticleSystem>();
        modelRenderer = GetComponent<Renderer>();
    }
    private void Update()
    {
        controlTime += Time.deltaTime;
        modelRenderer.material.SetFloat("_ControlTime", controlTime);
    }

    public void Hit(Vector3 force, Vector3 origin)
    {
        health--;
        if (health == 0)
        {
            Explode(force);
            return;
        }
        modelRenderer = GetComponent<Renderer>();
        controlTime = 0;
        modelRenderer.material.SetVector("_ModelOrigin", transform.position);
        modelRenderer.material.SetVector("_ImpactOrigin", origin);
    }

    public void Explode(Vector3 force)
    {
        GetComponent<MeshCollider>().enabled = false;
        GetComponent<MeshRenderer>().enabled = false;
        var scale = transform.localScale;
        transform.localScale = new Vector3(1, 1, 1);
        var shape = particle.shape;
        shape.scale = scale;
        var velocityoverlife = particle.velocityOverLifetime;
        velocityoverlife.x = force.x / 3;
        velocityoverlife.y = force.y / 3;
        velocityoverlife.z = force.z / 3;
        particle.Play();
    }

}
