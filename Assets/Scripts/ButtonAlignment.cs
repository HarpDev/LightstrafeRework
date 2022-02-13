using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class ButtonAlignment : MonoBehaviour
{

    private void Update()
    {
        if (Application.isPlaying) return;
        var rect = GetComponent<RectTransform>();
        var position = transform.localPosition;
        position.x -= position.x % (rect.rect.width + 10);
        position.y -= position.y % (rect.rect.height + 10);
        //transform.localPosition = position;
    }
}
