using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

public class Level : MonoBehaviour
{
    public bool TimerRunning { get; private set; }

    public Hitmarker hitmarker;
    public PlayerMovement player;
    public float CurrentTime { get; set; }
    private bool Finished { get; set; }

    private void Update()
    {
        if (TimerRunning && !Finished) CurrentTime += Time.unscaledDeltaTime;
        if (Input.GetKeyDown(PlayerInput.Pause))
        {
            if (!IsPaused()) Pause();
        }
        if (!IsPaused() && Cursor.visible) Unpause();
    }

    public bool IsPaused()
    {
        return Game.UiTree.Count != 0;
    }

    public void Pause()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Game.OpenPauseMenu();

        Game.PostProcessVolume.profile.TryGetSettings(out Blur blur);
        blur.BlurIterations.value = 8;
        blur.enabled.value = true;
    }

    public void Unpause()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Game.PostProcessVolume.profile.TryGetSettings(out Blur blur);
        if (blur != null)
        {
            blur.BlurIterations.value = 0;
            blur.enabled.value = false;
        }
    }

    public void ResetTimer()
    {
        CurrentTime = 0;
    }

    public void StopTimer()
    {
        TimerRunning = false;
    }

    public void StartTimer()
    {
        if (!Finished) TimerRunning = true;
    }

    public void EndTimer()
    {
        if (TimerRunning || !Finished)
        {
            Finished = true;
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
}
