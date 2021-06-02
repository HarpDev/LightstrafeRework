using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Menu : MonoBehaviour
{

    private void Update()
    {
        if (PlayerInput.GetKeyDown(PlayerInput.Pause))
        {
            Game.CloseMenu();
        }
    }
}
