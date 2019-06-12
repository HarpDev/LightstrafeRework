using UnityEngine;
using UnityEngine.UI;

public class SpeedDisplay : MonoBehaviour
{
    public Text speedText;
    public string prefix;

    // Update is called once per frame
    private void Update()
    {
        speedText.text = prefix + Mathf.RoundToInt(Flatten(Game.I.Player.velocity).magnitude);
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}
