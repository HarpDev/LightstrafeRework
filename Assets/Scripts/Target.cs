using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayerMovement;

public class Target : MonoBehaviour
{

    public GameObject core;
    public Ability ability;

    private const float radius = 20f;
    private const float power = 55f;

    private void Awake()
    {
        GetComponent<Rigidbody>().AddTorque(new Vector3(Random.Range(-500, 500), Random.Range(-500, 500), Random.Range(-500, 500)));
    }

    public void Explode()
    {
        if (ability == Ability.GRAPPLE)
        {
            Game.Level.player.AttachGrapple(transform.position);
            return;
        }
        if (ability == Ability.DASH)
        {
            var shockwave = Instantiate(core);
            shockwave.transform.position = transform.position;
            shockwave.transform.localScale = Vector3.one * 40;
            var collider = (SphereCollider)shockwave.AddComponent(typeof(SphereCollider));
            collider.isTrigger = true;
            shockwave.tag = "Shockwave";
        }
        /*
        debris.transform.parent = null;
        exploded = true;
        debris.SetActive(true);
        foreach (var body in debris.GetComponentsInChildren<Rigidbody>())
        {
            body.AddTorque(new Vector3(Random.Range(-50, 50), Random.Range(-50, 50), Random.Range(-50, 50)));
            var force = direction.normalized * 150 + new Vector3(Random.Range(-100, 100), Random.Range(-100, 100), Random.Range(-100, 100));
            body.AddForce(force * 10);
        }
        Game.Level.player.AddAbility(ability);*/
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}
