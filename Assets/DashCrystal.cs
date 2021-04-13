using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashCrystal : MonoBehaviour
{
    private void Start()
    {
        Game.Player.TriggerEvent += PlayerTriggerEvent;
    }

    private void Update()
    {
        transform.Rotate(new Vector3(0, 0, Time.deltaTime * 70));
    }

    public void PlayerTriggerEvent(Vector3 normal, Collider collider)
    {
        if (collider.gameObject != this.gameObject) return;
        if (Game.Player.DashAvailable) return;
        Game.Player.DashAvailable = true;
        gameObject.SetActive(false);
    }
}
