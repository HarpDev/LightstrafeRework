using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TimerDisplay : MonoBehaviour
{
    public enum TimerType
    {
        CURRENT_LEVEL,
        LEVEL_BEST,
        SUM_OF_BEST,
        FULL_GAME_PB,
        FULL_GAME
    }

    public Text timerText;
    public string prefix;
    public string levelName;

    public TimerType timerType;

    private Timers timers;
    private Level level;

    private void Start()
    {
        level = Game.OnStartResolve<Level>();
        timers = Game.OnStartResolve<Timers>();
    }

    private void Update()
    {
        var secondformat = "0.00";
        var secondformat2 = "00.00";
        var minuteformat = "0";

        var currentLevel = levelName.Length <= 0;

        var color = Color.white;
        if (level != null && level.IsLevelFinished)
        {
            color = Color.green;
            if (timers.PB) color = Color.yellow;
        }

        var ticks = 0;
        switch (timerType)
        {
            case TimerType.LEVEL_BEST:
                ticks = timers.GetBestLevelTime(currentLevel ? SceneManager.GetActiveScene().name : levelName);
                break;
            case TimerType.CURRENT_LEVEL:
                ticks = timers.CurrentLevelTickCount;
                timerText.color = color;
                break;
            case TimerType.FULL_GAME:
                ticks = GameSettings.FullGameTimer && timers.CurrentFullRunTickCount >= 0 &&
                        timers.CurrentFullRunTickCount != timers.CurrentLevelTickCount
                    ? timers.CurrentFullRunTickCount
                    : -1;
                break;
            case TimerType.SUM_OF_BEST:
                ticks = 0;
                for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                {
                    var name = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));
                    if (name.ToLower().Contains("level"))
                    {
                        if (PlayerPrefs.HasKey("BestTime" + name))
                        {
                            //ticks += timers.GetBestLevelTime(name);
                        }
                        else
                        {
                            ticks = -1;
                            break;
                        }
                    }
                }

                break;
            case TimerType.FULL_GAME_PB:
                ticks = timers.FullGamePB;
                break;
        }

        // stop rendering best time timer if player has not set a pb yet
        if (ticks == -1)
        {
            timerText.text = "";
            return;
        }

        var seconds = (ticks % 6000) * Time.fixedDeltaTime;
        var minutes = Mathf.Floor(ticks / 6000f);

        if (minutes > 0)
        {
            timerText.text = prefix + minutes.ToString(minuteformat) + ":" + seconds.ToString(secondformat2);
        }
        else
        {
            timerText.text = prefix + seconds.ToString(secondformat);
        }
    }
}