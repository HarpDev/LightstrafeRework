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

    private float _blurCount;
    public bool Finished { get; set; }

    private void Update()
    {
        if (Finished && Time.timeScale > 0)
        {
            Time.timeScale -= Time.unscaledDeltaTime * 3;
            if (Time.timeScale < 0.05f) Time.timeScale = 0;
            Game.I.Level.player.LookScale = Time.timeScale;
            Game.I.PostProcessVolume.profile.TryGetSettings(out Blur blur);
            if (blur != null && blur.BlurIterations.value < 8)
            {
                if (!blur.enabled.value) blur.enabled.value = true;
                blur.BlurIterations.value = Mathf.RoundToInt(_blurCount);
                _blurCount += Time.unscaledDeltaTime * 15;
            }
        }
        else if (Finished && Mathf.Abs(Time.timeScale) < 0.05f)
        {
            if (!Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Game.I.OpenFinishMenu();
            }
        }

        if (TimerRunning) CurrentTime += Time.unscaledDeltaTime;
        if (Input.GetKeyDown(PlayerInput.Pause))
        {
            if (!IsPaused()) Pause();
        }
        if (!IsPaused() && Cursor.visible) Unpause();
    }

    public bool IsPaused()
    {
        return Game.I.UiTree.Count != 0;
    }

    public void Pause()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Game.I.OpenPauseMenu();

        Game.I.PostProcessVolume.profile.TryGetSettings(out Blur blur);
        blur.BlurIterations.value = 8;
        blur.enabled.value = true;
    }

    public void Unpause()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Game.I.PostProcessVolume.profile.TryGetSettings(out Blur blur);
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
        TimerRunning = true;
    }

    public void EndTimer()
    {
        Finished = true;
        TimerRunning = false;
        var level = SceneManager.GetActiveScene().name;
        if (CurrentTime < Game.GetBestLevelTime(level) || Game.GetBestLevelTime(level) < 0f)
        {
            Game.SetBestLevelTime(level, CurrentTime);
        }
    }
}
