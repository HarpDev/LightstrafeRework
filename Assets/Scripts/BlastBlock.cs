using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlastBlock : MonoBehaviour
{

    public float force = 20;

    public void Hit()
    {
        Hitmarker.Display(false);
        var o = gameObject;

        var lookat = (Game.I.Player.transform.position - o.transform.position).normalized;
        lookat *= 2;
        lookat.x = Mathf.RoundToInt(lookat.x);
        lookat.y = Mathf.RoundToInt(lookat.y);
        lookat.z = Mathf.RoundToInt(lookat.z);
        lookat /= 2;
        
        Game.I.Player.velocity += lookat * force;

        DoubleJump.doubleJumpSpent = false;
        
        Destroy(o);
    }
}
