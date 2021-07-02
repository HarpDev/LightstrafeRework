using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TimerDisplay : MonoBehaviour
{
    public Text timerText;
    public string prefix;
    public string level;
    public bool currentLevel;

    public static Color color;

    public bool bestTime;

    private void Start()
    {
        color = timerText.color;
    }

    private void Update()
    {
        var secondformat = "0.00";
        var secondformat2 = "00.00";
        var minuteformat = "0";
        if (!bestTime) timerText.color = color;
        var ticks = bestTime
            ? Game.GetBestLevelTime(currentLevel ? SceneManager.GetActiveScene().name : level)
            : Game.CurrentLevelTickCount;
        var seconds = (ticks % 6000) * Time.fixedDeltaTime;
        var minutes = Mathf.Floor(ticks / 6000);

        // this is probably bad and could be done in like 3 lines but it's the best i could figure out atm lmao
        if (minutes > 0)
        {
            timerText.text = prefix + minutes.ToString(minuteformat) + ":" + seconds.ToString(secondformat2);
        } else
        {
            timerText.text = prefix + seconds.ToString(secondformat);
        }

        // stop rendering best time timer if player has not set a pb yet
        if(ticks == -1f)
        {
            timerText.text = "";
        }
    }
}