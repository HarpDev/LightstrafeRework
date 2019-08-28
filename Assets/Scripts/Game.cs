using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour
{
    public static Game I;
    
    public static Dictionary<string, LevelTime> LevelTimes { get; set; }
    public class LevelTime
    {
        public LevelTime()
        {
            BestTime = Int32.MaxValue;
        }
        
        public float Time { get; set; }
        public float BestTime { get; set; }
    }

    private static bool timerRunning;

    public static int LevelStartTime { get; set; }
    public PlayerControls Player { get; private set; }
    public Canvas Canvas { get; private set; }
    
    public PostProcessVolume PostProcessVolume { get; private set; }

    public Hitmarker hitmarker;

    private void Awake()
    {
        Time.timeScale = 1;
        if (I == null)
        {
            LevelTimes = new Dictionary<string, LevelTime>();
            DontDestroyOnLoad(gameObject);
            I = this;
            Application.targetFrameRate = 300;
            I.find();
        }
        else if (I != this)
        {
            I.find();
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (timerRunning)
        {
            var level = SceneManager.GetActiveScene().name;
            LevelTime time;
            if (!LevelTimes.TryGetValue(level, out time))
                time = LevelTimes[level] = new LevelTime();
            time.Time += Time.unscaledDeltaTime;
        }
    }

    private void find()
    {
        var playerObj = GameObject.Find("Player");
        if (playerObj != null) Player = playerObj.GetComponent<PlayerControls>();
        var canvasObj = GameObject.Find("Canvas");
        if (canvasObj != null) Canvas = canvasObj.GetComponent<Canvas>();
        var postprocessObj = GameObject.Find("Level");
        if (postprocessObj != null) PostProcessVolume = postprocessObj.GetComponent<PostProcessVolume>();
    }

    public static void ResetTimer()
    {
        LevelStartTime = Environment.TickCount;
    }

    public static void StartTimer()
    {
        timerRunning = true;
    }

    public static void EndTimer()
    {
        timerRunning = false;
        var time = LevelTimes[SceneManager.GetActiveScene().name];
        if (time.Time < time.BestTime)
        {
            time.BestTime = time.Time;
        }
    }

    public static void StartLevel()
    {
        SceneManager.LoadScene(0);
    }

    public static void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public static void NextLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public static void StartMenu()
    {
        SceneManager.LoadScene("LevelSelect");
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}