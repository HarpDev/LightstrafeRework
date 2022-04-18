using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuickspawnVelocity : MonoBehaviour
{
    public Vector3 Velocity
    {
        get => transform.forward * speed;
    }
    public float speed = 0;
}
