using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Menu : MonoBehaviour
{

    private void LateUpdate()
    {
        if (Input.GetKeyDown((KeyCode)PlayerInput.Pause))
        {
            Game.CloseMenu();
            PlayerInput.ConsumeBuffer(PlayerInput.Pause);
        }
    }
}
