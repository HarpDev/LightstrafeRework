using UnityEngine;
using UnityEngine.UI;

public class SpeedDisplay : MonoBehaviour
{
    public Text speedText;
    public string prefix;
    public bool potential;

    private void Update()
    {
        var display = Mathf.RoundToInt(potential ? Mathf.Abs(Game.Level.player.velocity.y) * 2 : (Flatten(Game.Level.player.velocity).magnitude * 2));
        speedText.text = prefix + display;
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}
