using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContactTeleport : MonoBehaviour
{

    public Transform firstTeleport;

    public bool killVelocity;

    private void OnCollisionEnter(Collision collision)
    {
        Game.I.Player.Teleport(firstTeleport.position);
        if (killVelocity)
            Game.I.Player.velocity = new Vector3();
    }
}
