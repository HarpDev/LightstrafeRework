using System;
using UnityEngine;
using UnityEngine.UI;

public class TimerDisplay : MonoBehaviour
{
    public Text timerText;
    public string prefix;

    // Update is called once per frame
    private void Update()
    {
        var seconds = (Environment.TickCount - Game.LevelStartTime) / 1000f;
        timerText.text = prefix + seconds.ToString("0.0");
    }
}
