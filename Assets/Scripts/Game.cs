using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour
{

    private static Game I;

    public Canvas Chapter1Select;
    public Canvas Options;
    public Canvas Pause;
    public Canvas LevelCompleted;

    public static string checkpointScene;
    public static Vector3 lastCheckpoint;
    public static float checkpointYaw;

    public static readonly Color green = new Color(19f / 255f, 176f / 255f, 65f / 255f);
    public static readonly Color gold = new Color(255f / 255f, 226f / 255f, 0);

    public static string CurrentLevel { get; private set; }
    public static int CurrentLevelTickCount { get; private set; }
    public static bool TimerRunning { get; set; }
    public static bool LevelFinished { get; private set; }

    public static float Sensitivity
    {
        get
        {
            if (!PlayerPrefs.HasKey("Sensitivity")) PlayerPrefs.SetFloat("Sensitivity", 1);
            return PlayerPrefs.GetFloat("Sensitivity");
        }
        set { PlayerPrefs.SetFloat("Sensitivity", value); }
    }
    public static void SetBestLevelTime(string level, float time)
    {
        PlayerPrefs.SetFloat("va0.8BestTime" + level, time);
    }

    public static float GetBestLevelTime(string level)
    {
        return PlayerPrefs.HasKey("va0.8BestTime" + level) ? PlayerPrefs.GetFloat("va0.8BestTime" + level) : -1f;
    }

    private static PlayerMovement player;
    public static PlayerMovement Player
    {
        get
        {
            if (player == null)
            {
                var levelObj = GameObject.Find("Player");
                if (levelObj != null) player = levelObj.GetComponent<PlayerMovement>();
            }
            return player;
        }
        private set { player = value; }
    }

    private static CanvasContainer canvas;
    public static CanvasContainer Canvas
    {
        get
        {
            if (canvas == null)
            {
                var canvasObj = GameObject.Find("Canvas");
                if (canvasObj != null) canvas = canvasObj.GetComponent<CanvasContainer>();
            }
            return canvas;
        }
        private set { canvas = value; }
    }

    public static List<Canvas> UiTree { get; private set; }

    private static PostProcessVolume postProcessVolume;
    public static PostProcessVolume PostProcessVolume
    {
        get
        {
            if (postProcessVolume == null)
            {
                if (player != null) postProcessVolume = player.gameObject.GetComponent<PostProcessVolume>();
            }
            return postProcessVolume;
        }
        private set { postProcessVolume = value; }
    }

    private static bool _inputAlreadyTaken;

    private void LateUpdate()
    {
        _inputAlreadyTaken = false;
    }

    private void FixedUpdate()
    {
        if (!LevelFinished)
        {
            if (TimerRunning) CurrentLevelTickCount++;
            else
            {
                if (Player != null)
                {
                    if (Player.velocity.magnitude > 0.01f)
                    {
                        CurrentLevelTickCount++;
                        TimerRunning = true;
                    }
                }
            }
        }

        if (LevelFinished)
        {
            if (Time.timeScale > 0.1f)
                Time.timeScale -= Mathf.Min(Time.fixedUnscaledDeltaTime * Time.timeScale, Time.timeScale);
            else
            {
                Time.timeScale = 0;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                OpenMenu(LevelCompleted);
            }

            if (Game.PostProcessVolume.profile.TryGetSettings(out Blur blur))
            {
                blur.BlurIterations.value = Mathf.RoundToInt((1 - Time.timeScale) * 8);
                blur.enabled.value = true;
            }
        }

        var level = SceneManager.GetActiveScene().name;
        if (level != CurrentLevel)
        {
            TimerRunning = false;
            LevelFinished = false;
            CurrentLevelTickCount = 0;
            CurrentLevel = level;
        }
    }

    private void Awake()
    {
        UiTree = new List<Canvas>();
        Time.timeScale = 1;
        if (I == null)
        {
            DontDestroyOnLoad(gameObject);
            I = this;
            Application.targetFrameRate = 300;
        }
        else if (I != this)
        {
            Destroy(gameObject);
        }
    }

    public static void EndTimer()
    {
        if (TimerRunning)
        {
            TimerRunning = false;
            LevelFinished = true;
            var level = SceneManager.GetActiveScene().name;
            if (CurrentLevelTickCount < Game.GetBestLevelTime(level) || Game.GetBestLevelTime(level) < 0f)
            {
                Game.SetBestLevelTime(level, CurrentLevelTickCount);
                TimerDisplay.color = Color.yellow;
            }
            else
            {
                TimerDisplay.color = Color.green;
            }
        }
    }

    public static void CloseMenu()
    {
        if (_inputAlreadyTaken) return;
        _inputAlreadyTaken = true;
        if (UiTree.Count > 0)
        {
            if (UiTree.Count == 1 && player != null && player.IsPaused() && LevelFinished) return;
            var obj = UiTree[UiTree.Count - 1];
            UiTree.RemoveAt(UiTree.Count - 1);
            Destroy(obj.gameObject);
            if (UiTree.Count > 0)
            {
                UiTree[UiTree.Count - 1].gameObject.SetActive(true);
            }
            else
            {
                Canvas.gameObject.SetActive(true);
            }
        }
    }

    private static void OpenMenu(Canvas canvas)
    {
        if (_inputAlreadyTaken) return;
        _inputAlreadyTaken = true;
        foreach (var c in UiTree)
        {
            c.gameObject.SetActive(false);
        }
        Canvas.gameObject.SetActive(false);
        UiTree.Add(Instantiate(canvas));
    }

    public static void OpenPauseMenu()
    {
        OpenMenu(I.Pause);
    }

    public static void OpenChapter1Select()
    {
        OpenMenu(I.Chapter1Select);
    }

    public static void OpenOptionsMenu()
    {
        OpenMenu(I.Options);
    }

    public static void RestartLevel()
    {
        TimerRunning = false;
        LevelFinished = false;
        CurrentLevelTickCount = 0;
        Time.timeScale = 1;
        lastCheckpoint = new Vector3();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public static void ReturnToLastCheckpoint()
    {
        if (lastCheckpoint.sqrMagnitude <= 0.05f)
        {
            CurrentLevelTickCount = 0;
            TimerRunning = false;
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public static void NextLevel()
    {
        if (SceneManager.GetActiveScene().buildIndex + 1 >= SceneManager.sceneCountInBuildSettings)
        {
            if (SceneManager.GetActiveScene().buildIndex == 0)
            {
                RestartLevel();
            } else
            {
                SceneManager.LoadScene(0);
            }
        } else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
    }

    public static void StartMenu()
    {
        SceneManager.LoadScene("LevelSelect");
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}