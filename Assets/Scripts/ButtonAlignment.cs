using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class ButtonAlignment : MonoBehaviour
{

    private void Update()
    {
        if (Application.isPlaying) return;
        var position = transform.localPosition;
        position.x -= position.x % 165;
        position.y -= position.y % 35;
        transform.localPosition = position;
    }
}
