using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class YAlignment : MonoBehaviour
{

    private void Update()
    {
        if (Application.isPlaying) return;
        var position = transform.position;
        position.y -= position.y % 5;
        transform.position = position;
    }
}
