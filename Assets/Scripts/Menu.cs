using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Menu : MonoBehaviour
{
    private CanvasManager canvasManager;
    private PlayerInput input;

    private void Start()
    {
        input = Game.OnStartResolve<PlayerInput>();
        canvasManager = Game.OnStartResolve<CanvasManager>();
    }

    private void LateUpdate()
    {
        if (Input.GetKeyDown((KeyCode)PlayerInput.Pause))
        {
            canvasManager.CloseMenu();
        }
    }
}
