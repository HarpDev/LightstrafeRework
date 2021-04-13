using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{

    private void Awake()
    {
        Game.Player.weaponManager.ShotEvent += Hit;
    }

    public void Hit(RaycastHit hit)
    {
        if (hit.collider.gameObject != gameObject) return;
        Game.Canvas.hitmarker.Display();
        Game.Player.source.PlayOneShot(Game.Player.ding);
        Game.Player.weaponManager.ShotEvent -= Hit;
        Destroy(gameObject);
    }
}
