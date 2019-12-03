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
        if (!bestTime) timerText.color = color;
        var seconds = bestTime
            ? Game.GetBestLevelTime(currentLevel ? SceneManager.GetActiveScene().name : level)
            : Game.Level.CurrentTime;
        if (seconds < 0) timerText.text = "";
        else timerText.text = prefix + seconds.ToString("0.0");
    }
}