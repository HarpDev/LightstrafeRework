using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PistolOrb : MonoBehaviour
{
    public AudioClip hitSound;

    private void Start()
    {
        Game.Player.weaponManager.ShotEvent += Hit;
    }

    public void Hit(RaycastHit hit, ref bool doReload)
    {
        if (hit.collider.gameObject != gameObject) return;

        Game.Canvas.hitmarker.Display();
        Game.Player.audioManager.PlayOneShot(hitSound);
        Game.Player.weaponManager.EquipGun(WeaponManager.GunType.Pistol);
        doReload = false;
        Game.Player.weaponManager.ShotEvent -= Hit;
        Destroy(gameObject);
    }
}
