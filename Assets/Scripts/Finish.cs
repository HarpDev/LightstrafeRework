using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class Finish : MonoBehaviour
{

    public GameObject finishMenu;

    private bool _finished;

    private void Awake()
    {
        finishMenu.SetActive(false);
    }

    private float TOLERANCE = 0.01f;

    private float _blurCount;

    private void Update()
    {
        if (_finished && Time.timeScale > 0)
        {
            Time.timeScale -= Time.deltaTime * 3;
            if (Time.timeScale < TOLERANCE) Time.timeScale = 0;
            Game.I.Player.LookScale = Time.timeScale;
            Blur blur;
            Game.I.PostProcessVolume.profile.TryGetSettings(out blur);
            if (blur.BlurIterations.value < 8)
            {
                if (!blur.enabled.value) blur.enabled.value = true;
                blur.BlurIterations.value = Mathf.RoundToInt(_blurCount);
                _blurCount += Time.unscaledDeltaTime * 15;
            }
        }
        else if (_finished && Math.Abs(Time.timeScale) < TOLERANCE)
        {
            if (!Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                finishMenu.SetActive(true);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _finished = true;
            Game.EndTimer();
        }
    }
}