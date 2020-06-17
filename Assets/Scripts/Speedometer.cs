using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Speedometer : MonoBehaviour
{

    public Image leftLayer1;
    public Image rightLayer1;
    public Image leftLayer2;
    public Image rightLayer2;

    private float _layer1Lerp;
    private float _layer2Lerp;

    // Update is called once per frame
    private void Update()
    {
        var speed = Flatten(Game.Player.velocity).magnitude / 50;

        var layer1 = Mathf.Min(1, Mathf.Max(0, speed));
        _layer1Lerp = Mathf.Lerp(_layer1Lerp, layer1, Time.deltaTime * 5);
        leftLayer1.fillAmount = _layer1Lerp / 2;
        rightLayer1.fillAmount = _layer1Lerp / 2;

        var layer2 = Mathf.Min(1, Mathf.Max(0, speed - 1));
        _layer2Lerp = Mathf.Lerp(_layer2Lerp, layer2, Time.deltaTime * 5);
        leftLayer2.fillAmount = _layer2Lerp / 2;
        rightLayer2.fillAmount = _layer2Lerp / 2;
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}
