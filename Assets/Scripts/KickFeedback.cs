using UnityEngine;
using UnityEngine.UI;

public class KickFeedback : MonoBehaviour
{
    public Text text;

    private void Awake()
    {
        Game.OnAwakeBind(this);
    }

    private void Update()
    {
        if (text.color.a > 0)
        {
            var color = text.color;
            if (GameSettings.UseTimingDisplay)
                color.a -= Time.deltaTime * (1.03f - color.a) * 4f;
            else
                color.a = 0;
            text.color = color;
        }
    }

    public void Display(int frictionTicks, Color color)
    {
        text.text = (frictionTicks >= 0 ? "+" : "-") + Mathf.Abs(frictionTicks);
        text.color = color;
    }
}
