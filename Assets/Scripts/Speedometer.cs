using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Speedometer : MonoBehaviour
{

    public Image leftLayer1;
    public Image rightLayer1;

    private float _layer1Lerp;

    private void Update()
    {
        var speed = Flatten(Game.Player.velocity).magnitude / PlayerMovement.FLOWSPEED;

        var layer1 = Mathf.Min(1, Mathf.Max(0, speed));
        _layer1Lerp = Mathf.Lerp(_layer1Lerp, layer1, Time.deltaTime * 5);
        leftLayer1.fillAmount = _layer1Lerp / 2;
        rightLayer1.fillAmount = _layer1Lerp / 2;
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}
