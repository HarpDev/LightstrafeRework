using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Level : MonoBehaviour
{
    
    public bool IsLevelFinished { get; private set; }

    public string LevelName => SceneManager.GetActiveScene().name;

    public Canvas LevelCompletedUIPrefab;

    private CanvasManager canvasManager;

    private void Awake()
    {
        Game.OnAwakeBind(this);
    }

    private void Start()
    {
        canvasManager = Game.OnStartResolve<CanvasManager>();
        Time.timeScale = 1;
    }

    private bool togglePauseOnNextTick;

    private void Update()
    {
        if (Input.GetKeyDown((KeyCode) PlayerInput.Pause))
        {
            if (canvasManager.MenuLayerCount == 1)
            {
                Time.timeScale = 1;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            if (canvasManager.MenuLayerCount == 0)
            {
                togglePauseOnNextTick = true;
            }
        }
    }

    private void FixedUpdate()
    {
        if (togglePauseOnNextTick)
        {
            togglePauseOnNextTick = false;
            if (canvasManager.MenuLayerCount == 0)
            {
                Time.timeScale = 0;
                canvasManager.OpenMenu(canvasManager.Pause);
            }
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