using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetHitbox : MonoBehaviour
{
    public bool crit;

    public GameObject fullTarget;

    public void Hit()
    {
        Game.I.Hitmarker.Display();
        Game.I.Player.source.PlayOneShot(Game.I.Player.ding);
        Destroy(fullTarget);
    }
}