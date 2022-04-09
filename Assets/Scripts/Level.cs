using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Level : MonoBehaviour
{
    
    public bool IsLevelFinished { get; private set; }

    public string LevelName => SceneManager.GetActiveScene().name;

    public Canvas LevelCompletedUIPrefab;

    private CanvasManager canvasManager;
    private const float KILL_LEVEL = -10f;

    private void Awake()
    {
        Game.OnAwakeBind(this);
    }

    private Player player;

    private void Start()
    {
        player = Game.OnStartResolve<Player>();
        canvasManager = Game.OnStartResolve<CanvasManager>();
        Time.timeScale = 1;
    }

    private void Update()
    {
        if (Input.GetKeyDown((KeyCode) PlayerInput.Pause))
        {
            if (canvasManager.MenuLayerCount == 1)
            {
                if (!Game.playingReplay || !Game.replayFinishedPlaying) Time.timeScale = 1;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            if (canvasManager.MenuLayerCount == 0)
            {
                if (IsLevelFinished)
                {
                    Time.timeScale = 0;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    canvasManager.OpenMenuAndSetAsBaseCanvas(LevelCompletedUIPrefab);
                }
                else
                {
                    if (canvasManager.MenuLayerCount == 0)
                    {
                        Time.timeScale = 0;
                        canvasManager.OpenMenu(canvasManager.Pause);
                    }
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (player != null && player.transform.position.y <= KILL_LEVEL)
        {
            player.DoQuickDeath();
        }
        
        if (IsLevelFinished)
        {
            if (Time.timeScale > 0.1f)
                Time.timeScale -= Mathf.Min(Time.fixedUnscaledDeltaTime * Time.timeScale, Time.timeScale);
            else
            {
                Time.timeScale = 0;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                canvasManager.OpenMenuAndSetAsBaseCanvas(LevelCompletedUIPrefab);
            }
        }
    }

    public void LevelFinished()
    {
        IsLevelFinished = true;
        Game.SaveReplay = true;
    }

    public void RestartLevel()
    {
        IsLevelFinished = false;
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void NextLevel()
    {
        if (SceneManager.GetActiveScene().buildIndex + 1 >= SceneManager.sceneCountInBuildSettings)
        {
            if (SceneManager.GetActiveScene().buildIndex == 0)
            {
                RestartLevel();
            }
            else
            {
                IsLevelFinished = false;
                Time.timeScale = 1;
                SceneManager.LoadScene(0);
            }
        }
        else
        {
            IsLevelFinished = false;
            Time.timeScale = 1;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
    }
}