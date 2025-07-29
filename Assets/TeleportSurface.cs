using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportSurface : MonoBehaviour, MapInteractable
{
    public void Proc(RaycastHit hit)
    {
        player.Teleport(hit.point);
    }

    private Player player;

    private void Start()
    {
        player = Game.OnStartResolve<Player>();
    }
}
