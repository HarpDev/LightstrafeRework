using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{
    public bool crit;

    public GameObject fullTarget;

    public Canvas canvas;

    public void Hit()
    {
        Instantiate(!crit ? Game.I.hitmarker : Game.I.critmarker, canvas.transform);
        Game.I.Player.ding.Play();
        Destroy(fullTarget);
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}