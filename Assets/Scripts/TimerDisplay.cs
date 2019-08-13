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

    public bool bestTime;

    private void Update()
    {
        Game.LevelTime time;
        if (!currentLevel)
        {
            if (!Game.LevelTimes.TryGetValue(level, out time))
                time = new Game.LevelTime();
        }
        else
        {
            if (!Game.LevelTimes.TryGetValue(SceneManager.GetActiveScene().name, out time))
                time = new Game.LevelTime();
        }
        var seconds = bestTime ? time.BestTime : time.Time;
        timerText.text = prefix + seconds.ToString("0.0");
    }
}
