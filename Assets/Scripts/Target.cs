using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{

    public GameObject debris;
    public MeshRenderer mesh;
    public new Collider collider;
    public AudioSource sound;

    public void Hit(Vector3 vel)
    {
        mesh.enabled = false;
        sound.Play();
        collider.enabled = false;
        debris.SetActive(true);
        var r = new System.Random();
        foreach (Transform child in debris.transform)
        {
            var random = vel;
            random += new Vector3(r.Next(-5, 6) * 10, r.Next(-5, 6) * 10, r.Next(-5, 6) * 10);
            var body = child.gameObject.GetComponent<Rigidbody>();
            body.AddForce(random * 15);
            body.AddTorque(new Vector3(r.Next(-5, 6), r.Next(-5, 6), r.Next(-5, 6)) * 60);
            
        }
    }
    
}
