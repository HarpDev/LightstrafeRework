using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Breakable : MonoBehaviour
{

    public GameObject unbroken;

    public GameObject[] fragments;

    public void Break(Vector3 impulse, Vector3 playerPosition)
    {
        GetComponent<Collider>().enabled = false;
        unbroken.SetActive(false);
        foreach (var frag in fragments)
        {
            frag.SetActive(true);
            var rigidbody = frag.GetComponent<Rigidbody>();

            var center = frag.GetComponent<Renderer>().bounds.center;
            var randomPhysics = new Vector3(Random.Range(-60, 60), Random.Range(-60, 60), Random.Range(-60, 60));
            var distance = (center - playerPosition).magnitude;
            var scale = (10 - distance) / 3;
            if (scale < 1) scale = 1;
            rigidbody.AddForce(impulse * scale * 25 + randomPhysics);

            var torque = 200 * scale;
            rigidbody.AddTorque(Random.Range(-torque, torque), Random.Range(-torque, torque), Random.Range(-torque, torque));
        }
    }

}
