using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonFunctions : MonoBehaviour
{
    private Level level;
    private Timers timers;
    private CanvasManager canvasManager;
    private new PlayerAudioManager audio;
    private void Start()
    {
        audio = Game.OnStartResolve<PlayerAudioManager>();
        canvasManager = Game.OnStartResolve<CanvasManager>();
        level = Game.OnStartResolve<Level>();
        timers = Game.OnStartResolve<Timers>();
    }

    public void RestartLevel()
    {
        level.RestartLevel();
    }

    public void NextLevel()
    {
        level.NextLevel();
    }

    public void Startlevel(string name)
    {
        Game.StartLevel(name);
    }

    public void MainMenu()
    {
        SceneManager.LoadScene(0);
        timers.ResetFullGameRun();
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void OpenOptions()
    {
        canvasManager.OpenMenu(canvasManager.Options);
    }

    public void OpenChapter1Select()
    {
        canvasManager.OpenMenu(canvasManager.Chapter1Select);
    }

    public void OpenReplays()
    {
        canvasManager.OpenMenu(canvasManager.Replays);
    }

    public void OpenReplaysFolder()
    {
        var replaysFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                            @"\Lightstrafe\replays";
        Directory.CreateDirectory(replaysFolder);
        Process.Start("explorer.exe", replaysFolder);
    }

    public void ResetTimes()
    {
        timers.ResetTimes();
    }

    public void ResetBinds()
    {
        PlayerInput.ResetBindsToDefault();
    }

    public AudioClip buttonHover;
    public void PlayButtonHover()
    {
        audio.PlayAudio(buttonHover, false, 0.4f, true);
    }

    public AudioClip buttonClick;
    public void PlayButtonClick()
    {
        audio.PlayOneShot(buttonClick, false, 1, true);
    }

}