using UnityEngine;
using UnityEngine.UI;

public class SpeedDisplay : MonoBehaviour
{
    public Text speedText;
    public string prefix;
    public bool potential;

    private void Update()
    {
        var display = Mathf.RoundToInt(potential ? Mathf.Abs(Game.Player.velocity.y) : (Flatten(Game.Player.velocity).magnitude));
        speedText.text = prefix + display;
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}
