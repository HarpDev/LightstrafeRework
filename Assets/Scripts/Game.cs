using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour
{
    public static Game I;

    public static float Sensitivity
    {
        get
        {
            if (!PlayerPrefs.HasKey("Sensitivity")) PlayerPrefs.SetFloat("Sensitivity", 1);
            return PlayerPrefs.GetFloat("Sensitivity");
        }
        set { PlayerPrefs.SetFloat("Sensitivity", value); }
    }

    public static float CurrentLevelTime { get; set; }
    public static void SetBestLevelTime(string level, float time)
    {
        PlayerPrefs.SetFloat("v1.5BestTime" + level, time);
    }

    public static float GetBestLevelTime(string level)
    {
        return PlayerPrefs.HasKey("v1.5BestTime" + level) ? PlayerPrefs.GetFloat("v1.5BestTime" + level) : -1f;
    }

    public static bool TimerRunning { get; private set; }
    public PlayerMovement Player { get; private set; }
    public Canvas Canvas { get; private set; }
    
    public PostProcessVolume PostProcessVolume { get; private set; }
    
    public Hitmarker Hitmarker { get; private set; }

    public Notification notification;

    private void Awake()
    {
        Time.timeScale = 1;
        if (I == null)
        {
            DontDestroyOnLoad(gameObject);
            I = this;
            Application.targetFrameRate = 300;
        }
        else if (I != this)
        {
            I.Find();
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        I.Find();
    }

    private void Update()
    {
        if (TimerRunning) CurrentLevelTime += Time.unscaledDeltaTime;
    }

    private void Find()
    {
        var playerObj = GameObject.Find("Player");
        if (playerObj != null) Player = playerObj.GetComponent<PlayerMovement>();
        var canvasObj = GameObject.Find("Canvas");
        if (canvasObj != null) Canvas = canvasObj.GetComponent<Canvas>();
        var postprocessObj = GameObject.Find("Level");
        if (postprocessObj != null) PostProcessVolume = postprocessObj.GetComponent<PostProcessVolume>();
        var hitmarkerObj = GameObject.Find("hitmarker");
        if (hitmarkerObj != null) Hitmarker = hitmarkerObj.GetComponent<Hitmarker>();
    }

    public static void ResetTimer()
    {
        CurrentLevelTime = 0;
    }

    public static void StopTimer()
    {
        TimerRunning = false;
    }

    public static void StartTimer()
    {
        TimerRunning = true;
    }

    public static void EndTimer()
    {
        TimerRunning = false;
        var level = SceneManager.GetActiveScene().name;
        if (CurrentLevelTime < GetBestLevelTime(level) || GetBestLevelTime(level) < 0f)
        {
            SetBestLevelTime(level, CurrentLevelTime);
        }
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