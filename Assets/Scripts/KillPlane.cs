using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillPlane : MonoBehaviour
{
    public float killLevel = 0f;
    private Player player;
    private Level level;

    private void Start()
    {
        player = Game.OnStartResolve<Player>();
        level = Game.OnStartResolve<Level>();
    }

    private void FixedUpdate()
    {
        if (player.transform.position.y <= killLevel)
        {
            level.RestartLevel();
        }
    }
}
