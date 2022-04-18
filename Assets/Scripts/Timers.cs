using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Timers : MonoBehaviour
{
    public int CurrentLevelTickCount { get; private set; }
    public int CurrentFullRunTickCount { get; private set; }
    public bool PB { get; private set; }

    private const string FIRST_LEVEL = "Level1";
    private const string LAST_LEVEL = "Level9";

    private static Timers I;

    private void Awake()
    {
        if (I == null) Game.OnAwakeBind(this);
    }

    private Player player;
    private Level level;
    private PlayerInput input;

    private void Start()
    {
        if (I == null)
        {
            DontDestroyOnLoad(gameObject);
            I = this;
            ResetFullGameRun();
        }
        else if (I != this)
        {
            I.Start();
            Destroy(gameObject);
            return;
        }
        
        PB = false;
        level = Game.OnStartResolve<Level>();
        player = Game.OnStartResolve<Player>();
        input = Game.OnStartResolve<PlayerInput>();
        CurrentLevelTickCount = 0;
        TimerRunning = false;
    }

    private void FixedUpdate()
    {
        if (level == null) ResetFullGameRun();
        if (level != null && !level.IsLevelFinished && !TimerRunning)
        {
            if (player != null && Flatten(player.velocity).magnitude > 0.01f)
            {
                if (level.LevelName == FIRST_LEVEL)
                {
                    StartFullGameRun();
                }

                CurrentLevelTickCount++;
                if (CurrentFullRunTickCount >= 0) CurrentFullRunTickCount++;
                TimerRunning = true;
                Game.currentReplay.startTick = input.tickCount;
            }
        }
        if (TimerRunning)
        {
            CurrentLevelTickCount++;
            if (CurrentFullRunTickCount >= 0)
            {
                CurrentFullRunTickCount++;
            }
            
            if (level.IsLevelFinished)
            {
                EndTimer();
            }
        }
    }

    public bool TimerRunning { get; set; }

    public int FullGamePB
    {
        get => PlayerPrefs.GetInt("FullGamePB", -1);
        set => PlayerPrefs.SetInt("FullGamePB", value);
    }

    public void SetBestLevelTime(string level, int ticks)
    {
        PlayerPrefs.SetInt("BestTime" + level, ticks);
    }

    public int GetBestLevelTime(string level)
    {
        return PlayerPrefs.HasKey("BestTime" + level) ? PlayerPrefs.GetInt("BestTime" + level) : -1;
    }

    public void ResetFullGameRun()
    {
        CurrentFullRunTickCount = -1;
    }

    public void StartFullGameRun()
    {
        CurrentFullRunTickCount = 0;
    }

    public void ResetTimes()
    {
        for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            var n = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));
            if (PlayerPrefs.HasKey("BestTime" + n))
            {
                PlayerPrefs.DeleteKey("BestTime" + n);
            }
        }
    }

    private void EndTimer()
    {
        if (TimerRunning)
        {
            TimerRunning = false;
            if (level.LevelName == LAST_LEVEL && CurrentFullRunTickCount >= 0)
            {
                if (CurrentFullRunTickCount < FullGamePB)
                {
                    FullGamePB = CurrentFullRunTickCount;
                    if (GameSettings.FullGameTimer)
                    {
                        PB = true;
                    }
                }
            }

            if (CurrentLevelTickCount < GetBestLevelTime(level.LevelName) || GetBestLevelTime(level.LevelName) < 0f)
            {
                if (!Game.playingReplay)
                {
                    SetBestLevelTime(level.LevelName, CurrentLevelTickCount);
                    if (!GameSettings.FullGameTimer || CurrentFullRunTickCount < 0)
                    {
                        PB = true;
                    }
                }
            }
        }
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}