using UnityEngine;
using UnityEngine.UI;

public class SpeedDisplay : MonoBehaviour
{
    public Text speedText;
    public string prefix;
    public bool flatten;

    private void Update()
    {
        var display = Mathf.RoundToInt(flatten ? (Flatten(Game.Level.player.velocity) * 2).magnitude : (Game.Level.player.velocity * 2).magnitude);
        speedText.text = prefix + display;
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}
