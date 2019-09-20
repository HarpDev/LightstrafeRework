using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tutorial : MonoBehaviour
{

    private void Update()
    {
        // velocity 0, 20, 30
        Game.I.Player.LookScale = 0;
        if (Input.GetAxis("Right") > 0)
        {
            Time.timeScale = 1;
        }
    }
}
