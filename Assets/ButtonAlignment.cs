using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonAlignment : MonoBehaviour
{

    private void Update()
    {
        var position = transform.position;
        position.x -= (position.x % 20);
        position.y -= (position.y % 20);
        transform.position = position;
    }
}
