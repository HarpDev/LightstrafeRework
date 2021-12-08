using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Menu : MonoBehaviour
{

    private void Update()
    {
        if (PlayerInput.SincePressed(PlayerInput.Pause) == 0)
        {
            Game.CloseMenu();
            PlayerInput.ConsumeBuffer(PlayerInput.Pause);
        }
    }
}
