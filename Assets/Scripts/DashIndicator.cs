using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DashIndicator : MonoBehaviour
{

    private Image _image;

    private void Start()
    {
        _image = GetComponent<Image>();
    }

    private void Update()
    {
        var camera = Game.Player.camera;

        var x = Mathf.Cos(PlayerMovement.DASH_THRESHOLD * Mathf.Deg2Rad);
        var y = Mathf.Sin(PlayerMovement.DASH_THRESHOLD * Mathf.Deg2Rad);

        var direction = Flatten(camera.transform.forward).normalized * x;
        var directionVector = new Vector3(direction.x, y, direction.z);

        var threshold = camera.WorldToScreenPoint(camera.transform.position + directionVector);

        var position = _image.transform.position;
        position.y = threshold.y;
        _image.transform.position = position;
    }
    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }

}
