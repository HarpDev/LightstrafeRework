using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class Menus : MonoBehaviour
{

    public GameObject pauseMenu;

    private bool pauseLock;

    private const float Tolerance = 0.05f;

    private void Update()
    {
        if (Input.GetAxis("Pause") > 0 && !pauseLock)
        {
            pauseLock = true;
            if (IsPaused())
                Unpause();
            else
                Pause();
        } else if (Input.GetAxis("Pause") < Tolerance) pauseLock = false;
    }

    public bool IsPaused()
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
    }

    public void Unpause()
    {
        if (!Cursor.visible) return;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        pauseMenu.SetActive(false);
        
        Blur blur;
        Game.I.PostProcessVolume.profile.TryGetSettings(out blur);
        blur.BlurIterations.value = 0;
        blur.enabled.value = false;
    }
}
