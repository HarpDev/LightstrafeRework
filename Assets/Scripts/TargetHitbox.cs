using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetHitbox : MonoBehaviour
{
    public bool crit;

    public GameObject fullTarget;

    public Finish finish;

    public void Hit()
    {
        finish.TargetsHit++;
        Hitmarker.Display(crit);
        Game.I.Player.ding.Play();
        Destroy(fullTarget);
    }
}
