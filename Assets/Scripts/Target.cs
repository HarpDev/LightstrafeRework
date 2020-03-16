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

    private GameObject _shockwave;
    private bool _shockwaveActive;

    private void Awake()
    {
        GetComponent<Rigidbody>().AddTorque(new Vector3(Random.Range(-500, 500), Random.Range(-500, 500), Random.Range(-500, 500)));
    }

    private void Update()
    {
        //if (!exploded) transform.Rotate(new Vector3(1, 2, 3) * Time.deltaTime * 20);
        if (_shockwaveActive)
        {
            _shockwave.transform.localScale += Vector3.one * Time.deltaTime * 60;
            if (_shockwave.transform.localScale.x > 40)
            {
                _shockwaveActive = false;
                Destroy(_shockwave);
            }
        }
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
            _shockwave = Instantiate(core);
            _shockwave.transform.position = transform.position;
            var collider = (SphereCollider)_shockwave.AddComponent(typeof(SphereCollider));
            collider.isTrigger = true;
            _shockwave.tag = "Shockwave";
            _shockwaveActive = true;
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
