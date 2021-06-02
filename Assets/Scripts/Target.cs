using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{
    public AudioClip hitSound;

    private void Awake()
    {
        Game.Player.weaponManager.ShotEvent += Hit;
    }

    public void Hit(RaycastHit hit, ref bool doReload)
    {
        if (hit.collider.gameObject != gameObject) return;
        Game.Canvas.hitmarker.Display();
        doReload = false;
        Game.Player.audioManager.PlayOneShot(hitSound);
        Game.Player.weaponManager.ShotEvent -= Hit;
        Destroy(gameObject);
    }
}
