using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetHitbox : MonoBehaviour
{
    public bool crit;

    public GameObject fullTarget;

    private Finish finish;
    
    private void Awake()
    {
        var f = GameObject.Find("Finish");
        if (f != null) finish = f.GetComponent<Finish>();
    }

    public void Hit()
    {
        if (finish != null)
            finish.TargetsHit++;
        Hitmarker.Display(crit);
        Game.I.Player.source.PlayOneShot(Game.I.Player.ding);
        Destroy(fullTarget);
    }
}