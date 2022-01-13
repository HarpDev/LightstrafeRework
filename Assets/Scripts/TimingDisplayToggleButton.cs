using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimingDisplayToggleButton : MonoBehaviour
{

    public Text text;

    private void Update()
    {
        text.text = Game.UseTimingDisplay ? "Yes" : "No";
    }

    public void Toggle()
    {
        Game.UseTimingDisplay = !Game.UseTimingDisplay;
    }
}
