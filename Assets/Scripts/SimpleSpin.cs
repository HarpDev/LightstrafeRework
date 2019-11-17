using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleSpin : MonoBehaviour
{

    public bool xAxis;
    public bool yAxis;
    public bool zAxis;

    public float xSpeed;
    public float ySpeed;
    public float zSpeed;

    private void Update()
    {
        var x = xAxis ? xSpeed : 0;
        var y = yAxis ? ySpeed : 0;
        var z = zAxis ? zSpeed : 0;
        transform.Rotate(new Vector3(x, y, z) * Time.deltaTime);
    }
}
