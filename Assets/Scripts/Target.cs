using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{
    public AudioClip hitSound;

    public void Hit(RaycastHit hit, ref bool doReload)
    {
        if (hit.collider.gameObject != gameObject) return;
        Game.Canvas.hitmarker.Display();
        doReload = false;
        Game.Player.audioManager.PlayOneShot(hitSound);
        Destroy(gameObject);
    }
}
