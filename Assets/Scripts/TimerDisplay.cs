using System;
using UnityEngine;
using UnityEngine.UI;

public class TimerDisplay : MonoBehaviour
{
    public Text timerText;
    public string prefix;

    public bool finalTime;

    public bool bestTime;

    // Update is called once per frame
    private void Update()
    {
        if (!finalTime && !bestTime)
        {
            var seconds = (Environment.TickCount - Game.LevelStartTime) / 1000f;
            timerText.text = prefix + seconds.ToString("0.0");
        }
        else if (finalTime && !bestTime)
        {
            var seconds = Game.FinalTime / 1000f;
            timerText.text = prefix + seconds.ToString("0.0");
        }
        else
        {
            var seconds = Game.BestTime / 1000f;
            timerText.text = prefix + seconds.ToString("0.0");
        }
    }
}
