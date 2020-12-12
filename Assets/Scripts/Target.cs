using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayerMovement;

public class Target : MonoBehaviour
{

    public GameObject core;

    private const int abilityCooldown = 1000;

    private bool _activated;

    private int _activatedTicks;

    private void Awake()
    {
        GetComponent<Rigidbody>().AddTorque(new Vector3(Random.Range(-500, 500), Random.Range(-500, 500), Random.Range(-500, 500)));

        //Gun.ShotEvent += new Gun.GunShot(Explode);
        //Projectile.ProjectileHitEvent += new Projectile.ProjectileHit(Explode);
    }

    private void FixedUpdate()
    {
        if (_activated)
        {
            //if (core.activeSelf && ability == Ability.GRAPPLE && !Game.Player.GrappleHooked) core.SetActive(false);
            if (!core.activeSelf) _activatedTicks++;
            if (_activatedTicks > abilityCooldown)
            {
                core.SetActive(true);
                _activated = false;
            }
        }
    }

    public void Explode(RaycastHit hit)
    {
        if (_activated || hit.collider.gameObject != gameObject) return;
        Game.Canvas.hitmarker.Display();
        _activatedTicks = 0;
        _activated = true;
        Game.Player.AttachGrapple(transform.position);
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}
