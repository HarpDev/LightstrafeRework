using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour
{
    public static Game I;

    public static int LevelStartTime { get; set; }
    public static int FinalTime { get; set; }
    public static int BestTime { get; set; }
    public PlayerControls Player { get; private set; }
    public Canvas Canvas { get; private set; }

    public Hitmarker hitmarker;

    private void Awake()
    {
        if (I == null)
        {
            LevelStartTime = Environment.TickCount;
            DontDestroyOnLoad(gameObject);
            I = this;
            Application.targetFrameRate = 300;
            I.find();
            BestTime = Int32.MaxValue;
        }
        else if (I != this)
        {
            I.find();
            Destroy(gameObject);
        }
    }

    private void find()
    {
        var playerObj = GameObject.Find("Player");
        if (playerObj != null) Player = playerObj.GetComponent<PlayerControls>();
        var canvasObj = GameObject.Find("Canvas");
        if (canvasObj != null) Canvas = canvasObj.GetComponent<Canvas>();
    }

    public static void StartLevel()
    {
        LevelStartTime = Environment.TickCount;
        SceneManager.LoadScene("Level2");
    }

    public static void RestartLevel()
    {
        LevelStartTime = Environment.TickCount;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public static void StartMenu()
    {
        SceneManager.LoadScene("Menu");
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}