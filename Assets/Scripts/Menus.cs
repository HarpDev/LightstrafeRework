using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class Menus : MonoBehaviour
{
    public GameObject pauseMenu;

    private void Update()
    {
        if (Input.GetKeyDown(PlayerInput.Pause))
        {
            if (IsPaused())
                Unpause();
            else
                Pause();
        }
    }

    public static bool IsPaused()
    {
        return Cursor.visible;
    }

    public void Pause()
    {
        if (Cursor.visible) return;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        pauseMenu.SetActive(true);

        Blur blur;
        Game.I.PostProcessVolume.profile.TryGetSettings(out blur);
        blur.BlurIterations.value = 8;
        blur.enabled.value = true;
    }

    public void Unpause()
    {
        if (!Cursor.visible) return;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        pauseMenu.SetActive(false);

        Game.I.PostProcessVolume.profile.TryGetSettings(out Blur blur);
        if (blur != null)
        {
            blur.BlurIterations.value = 0;
            blur.enabled.value = false;
        }
    }
}