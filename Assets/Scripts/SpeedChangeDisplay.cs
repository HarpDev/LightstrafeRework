using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpeedChangeDisplay : MonoBehaviour
{
    public Image gain;
    public Image loss;

    private float prevSpeed;
    public float interpolation { get; set; }
    private Player player;

    private void Start()
    {
        player = Game.OnStartResolve<Player>();
    }

    private void Update()
    {
        var speed = Flatten(player.velocity).magnitude;
        var change = speed - prevSpeed;
        interpolation += change;

        gain.fillAmount = Mathf.Clamp01(interpolation / 10);
        loss.fillAmount = Mathf.Clamp01(-interpolation / 10);

        interpolation = Mathf.Lerp(interpolation, 0, Time.deltaTime * 3);

        prevSpeed = speed;
    }
    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}
