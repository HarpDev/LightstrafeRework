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
    public Canvas Finish;

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
        PlayerPrefs.SetFloat("v1.5BestTime" + level, time);
    }

    public static float GetBestLevelTime(string level)
    {
        return PlayerPrefs.HasKey("v1.5BestTime" + level) ? PlayerPrefs.GetFloat("v1.5BestTime" + level) : -1f;
    }

    private Level level;
    public Level Level
    {
        get
        {
            if (level == null)
            {
                var levelObj = GameObject.Find("Level");
                if (levelObj != null) level = levelObj.GetComponent<Level>();
            }
            return level;
        }
        private set { level = value; }
    }

    private Canvas canvas;
    public Canvas Canvas
    {
        get
        {
            if (canvas == null)
            {
                var canvasObj = GameObject.Find("Canvas");
                if (canvasObj != null) canvas = canvasObj.GetComponent<Canvas>();
            }
            return canvas;
        }
        private set { canvas = value; }
    }

    public List<Canvas> UiTree { get; private set; }

    private PostProcessVolume postProcessVolume;
    public PostProcessVolume PostProcessVolume
    {
        get
        {
            if (postProcessVolume == null)
            {
                var levelObj = GameObject.Find("Level");
                if (levelObj != null) postProcessVolume = levelObj.GetComponent<PostProcessVolume>();
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

    private void Update()
    {
        if (Input.GetKeyDown(PlayerInput.Pause))
        {
            if (UiTree.Count > 0)
            {
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
    }

    public void OpenPauseMenu()
    {
        foreach (var canvas in UiTree)
        {
            canvas.gameObject.SetActive(false);
        }
        Canvas.gameObject.SetActive(false);
        UiTree.Add(Instantiate(Pause));
    }

    public void OpenFinishMenu()
    {
        foreach (var canvas in UiTree)
        {
            Destroy(canvas.gameObject);
        }
        UiTree.Clear();
        Destroy(Canvas.gameObject);
        Canvas = Instantiate(Finish);
        Canvas.gameObject.SetActive(true);
    }

    public void OpenChapter1Select()
    {
        foreach (var canvas in UiTree)
        {
            canvas.gameObject.SetActive(false);
        }
        Canvas.gameObject.SetActive(false);
        UiTree.Add(Instantiate(Chapter1Select));
    }

    public void OpenOptionsMenu()
    {
        foreach (var canvas in UiTree)
        {
            canvas.gameObject.SetActive(false);
        }
        Canvas.gameObject.SetActive(false);
        UiTree.Add(Instantiate(Options));
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