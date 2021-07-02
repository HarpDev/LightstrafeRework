using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PistolOrb : MonoBehaviour
{
    public AudioClip hitSound;

    public void Hit(RaycastHit hit, ref bool doReload)
    {
        if (hit.collider.gameObject != gameObject) return;

        Game.Canvas.hitmarker.Display();
        Game.Player.audioManager.PlayOneShot(hitSound);
        Game.Player.weaponManager.EquipGun(WeaponManager.GunType.Pistol);
        doReload = false;
        Destroy(gameObject);
    }
}
