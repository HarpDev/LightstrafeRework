using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TracerDecay : MonoBehaviour
{
    private LineRenderer line;
    private float a = 1;

    private void Start()
    {
        line = GetComponent<LineRenderer>();
    }
    private void Update()
    {
        if (a < 0) Destroy(gameObject);
        var color = Color.white;
        color.a = Mathf.Clamp01(a);
        line.material.color = color;
        a -= Time.deltaTime;
    }
}
