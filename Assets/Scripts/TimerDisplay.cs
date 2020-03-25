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
        var format = "0.0";
        if (Game.Level.LevelCompleted || bestTime) format = "0.00";
        if (!bestTime) timerText.color = color;
        var seconds = bestTime
            ? Game.GetBestLevelTime(currentLevel ? SceneManager.GetActiveScene().name : level)
            : Game.Level.CurrentTime;
        if (seconds < 0) timerText.text = "";
        else timerText.text = prefix + seconds.ToString(format);
    }
}