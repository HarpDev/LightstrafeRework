using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class GridPositioning : MonoBehaviour
{
    
    private void Update()
    {
        if (Application.isPlaying) return;
        var t = transform;
        var position = t.position;
        position.x = Mathf.RoundToInt(position.x);
        position.y = Mathf.RoundToInt(position.y);
        position.z = Mathf.RoundToInt(position.z);
        transform.position = position;
    }
}
