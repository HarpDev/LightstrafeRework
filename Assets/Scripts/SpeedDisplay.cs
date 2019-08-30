using UnityEngine;
using UnityEngine.UI;

public class SpeedDisplay : MonoBehaviour
{
    public Text speedText;
    public string prefix;

    // Update is called once per frame
    private void Update()
    {
        speedText.text = prefix + Mathf.RoundToInt((Game.I.Player.velocity * 2).magnitude);
    }
}
