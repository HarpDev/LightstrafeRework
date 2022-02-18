using UnityEngine;
using UnityEngine.UI;

public class SpeedDisplay : MonoBehaviour
{
    private Text speedText;
    public string prefix;
    public string suffix;
    public bool potential;
    private Player player;

    private void Start()
    {
        player = Game.OnStartResolve<Player>();
    }

    private void Awake()
    {
        speedText = GetComponent<Text>();
    }

    private float speedLerp = 0;

    private void Update()
    {
        if (player == null)
        {
            speedText.color = new Color(1, 1, 1, 0);
            return;
        }
        var display = potential ? Mathf.Abs(player.velocity.y) : Flatten(player.velocity).magnitude;
        speedLerp = Mathf.Lerp(speedLerp, display, Time.deltaTime * 8);
        speedText.text = prefix + Mathf.RoundToInt(speedLerp) + suffix;
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}
