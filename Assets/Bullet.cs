using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private void Start()
    {
        Game.Player.TriggerEvent += PlayerTriggerEvent;
    }

    private void Update()
    {
        
    }

    public void PlayerTriggerEvent(Vector3 normal, Collider collider)
    {
        if (collider.gameObject != this.gameObject) return;
        Game.Player.weaponManager.PistolShots++;
        gameObject.SetActive(false);
    }
}
