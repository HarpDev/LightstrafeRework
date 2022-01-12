using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillPlane : MonoBehaviour
{
    public float killLevel = 0f;

    private void FixedUpdate()
    {
        if (Game.Player.transform.position.y <= killLevel)
        {
            Game.RestartLevel();
        }
    }
}
