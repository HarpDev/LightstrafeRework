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

    private void Update()
    {
        var display = Mathf.RoundToInt(potential ? Mathf.Abs(player.velocity.y) : Flatten(player.velocity).magnitude);
        speedText.text = prefix + display + suffix;
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}
