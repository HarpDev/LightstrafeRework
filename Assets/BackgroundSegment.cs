using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundSegment : MonoBehaviour
{

    public float yOffset;

    private Vector3 interpPosition;

    public float targetDegrees = -1;

    private void Update()
    {
        var t = Mathf.Deg2Rad * targetDegrees;
        var r = 400;
        var x = Mathf.Cos(t) * r;
        var y = 0;
        var z = Mathf.Sin(t) * r;

        var target = new Vector3(x, y, z);
        interpPosition = interpPosition.magnitude == 0 ? target : Vector3.Lerp(interpPosition, target, Time.deltaTime * 8);
        interpPosition = interpPosition.normalized * r;
        transform.position = Game.Player.camera.transform.position + interpPosition + (Vector3.up * yOffset);

        transform.rotation = Quaternion.LookRotation(Game.Player.camera.transform.position - transform.position);
    }
}
