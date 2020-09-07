using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour
{
    public static Game I;

    public Canvas Chapter1Select;
    public Canvas Options;
    public Canvas Pause;

    public string checkpointScene;
    public Vector3 lastCheckpoint;
    public float checkpointYaw;

    public static readonly Color green = new Color(19f / 255f, 176f / 255f, 65f / 255f);
    public static readonly Color gold = new Color(255f / 255f, 226f / 255f, 0);

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
        PlayerPrefs.SetFloat("va0.7BestTime" + level, time);
    }

    public static float GetBestLevelTime(string level)
    {
        return PlayerPrefs.HasKey("va0.7BestTime" + level) ? PlayerPrefs.GetFloat("va0.7BestTime" + level) : -1f;
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

    public static void CloseMenu()
    {
        if (UiTree.Count > 0)
        {
            if (UiTree.Count == 1 && player != null && player.IsPaused() && player.LevelCompleted) return;
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

    public static void OpenPauseMenu()
    {
        foreach (var canvas in UiTree)
        {
            canvas.gameObject.SetActive(false);
        }
        Canvas.gameObject.SetActive(false);
        UiTree.Add(Instantiate(I.Pause));
    }

    public static void OpenChapter1Select()
    {
        foreach (var canvas in UiTree)
        {
            canvas.gameObject.SetActive(false);
        }
        Canvas.gameObject.SetActive(false);
        UiTree.Add(Instantiate(I.Chapter1Select));
    }

    public static void OpenOptionsMenu()
    {
        foreach (var canvas in UiTree)
        {
            canvas.gameObject.SetActive(false);
        }
        Canvas.gameObject.SetActive(false);
        UiTree.Add(Instantiate(I.Options));
    }

    public static void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public static void NextLevel()
    {
        if (SceneManager.GetActiveScene().buildIndex + 1 >= SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(0);
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