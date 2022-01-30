using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityUiTutorial : MonoBehaviour
{

    private Player player;
    private CanvasManager canvasManager;

    private void Start()
    {
        player = Game.OnStartResolve<Player>();
        canvasManager = Game.OnStartResolve<CanvasManager>();
        if (player == null || canvasManager == null) return;
        
        var notification = "";
        var keys = new List<int>();
        if (player.DashEnabled)
        {
            notification += "Press " + ((KeyCode) PlayerInput.SecondaryInteract) + " to dash";
            keys.Add(PlayerInput.SecondaryInteract);
        }

        if (player.GrappleEnabled)
        {
            if (player.DashEnabled) notification += "\n";
            notification += "Press " + ((KeyCode) PlayerInput.PrimaryInteract) + " to grapple";
            keys.Add(PlayerInput.PrimaryInteract);
        }

        if (notification.Length > 0) canvasManager.SendNotification(notification, keys);
    }
}
