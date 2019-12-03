using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillPlane : MonoBehaviour
{

    public int yLimit;

    // Update is called once per frame
    void Update()
    {
        if (Game.Level.player.transform.position.y < yLimit)
        {
            Game.RestartLevel();
        }
    }
}
