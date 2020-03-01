using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

public class Level : MonoBehaviour
{
    public bool TimerRunning { get; private set; }

    public bool LevelCompleted { get; private set; }

    public Hitmarker hitmarker;
    public PlayerMovement player;

    public float CurrentTime { get; set; }

    private void Update()
    {
        if (TimerRunning)
        {
            CurrentTime += Time.unscaledDeltaTime;
        }
        if (Input.GetKeyDown(PlayerInput.Pause))
        {
            if (!IsPaused()) Pause();
        }
        if (!IsPaused() && Cursor.visible) Unpause();

        if (Input.GetKeyDown(PlayerInput.PrimaryInteract))
        {
            _wishTimerStart = true;
        }
    }

    private bool _wishTimerStart;

    private void FixedUpdate()
    {
        if ((Flatten(player.velocity).magnitude > 0.01f || _wishTimerStart) && !TimerRunning && CurrentTime == 0 && !LevelCompleted)
        {
            TimerRunning = true;
        }
        _wishTimerStart = false;
    }

    public bool IsPaused()
    {
        return Game.UiTree.Count != 0;
    }

    public void Pause()
    {
        Time.timeScale = 0;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Game.OpenPauseMenu();

        Game.PostProcessVolume.profile.TryGetSettings(out Blur blur);
        blur.BlurIterations.value = 8;
        blur.enabled.value = true;
    }

    public void Unpause()
    {
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Game.PostProcessVolume.profile.TryGetSettings(out Blur blur);
        if (blur != null)
        {
            blur.BlurIterations.value = 0;
            blur.enabled.value = false;
        }
    }

    public void EndTimer()
    {
        if (TimerRunning)
        {
            LevelCompleted = true;
            TimerRunning = false;
            var level = SceneManager.GetActiveScene().name;
            if (CurrentTime < Game.GetBestLevelTime(level) || Game.GetBestLevelTime(level) < 0f)
            {
                Game.SetBestLevelTime(level, CurrentTime);
                TimerDisplay.color = Color.yellow;
            }
            else
            {
                TimerDisplay.color = Color.green;
            }
        }
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}
