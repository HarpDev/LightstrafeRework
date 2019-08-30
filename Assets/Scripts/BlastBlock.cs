using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlastBlock : MonoBehaviour
{

    public float force = 20;

    public ParticleSystem particle;

    public AudioSource blast;

    public void Hit()
    {
        Game.I.Hitmarker.Display();
        var o = gameObject;

        var lookat = (Game.I.Player.transform.position - o.transform.position).normalized;
        lookat *= 2;
        lookat.x = Mathf.RoundToInt(lookat.x);
        lookat.y = Mathf.RoundToInt(lookat.y);
        lookat.z = Mathf.RoundToInt(lookat.z);
        lookat /= 2;
        
        Game.I.Player.velocity += lookat * force;

        DoubleJump.doubleJumpSpent = false;
        blast.Play();
        particle.Play();

        o.GetComponent<MeshRenderer>().enabled = false;
        o.GetComponent<BoxCollider>().enabled = false;
        
        Invoke("Respawn", 3f);
    }

    public void Respawn()
    {
        var o = gameObject;
        o.GetComponent<MeshRenderer>().enabled = true;
        o.GetComponent<BoxCollider>().enabled = true;
    }
}
