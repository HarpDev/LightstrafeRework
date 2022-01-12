using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonFunctions : MonoBehaviour
{

    public void RestartLevel()
    {
        Game.RestartLevel();
    }

    public void NextLevel()
    {
        Game.NextLevel();
    }

    public void Startlevel(string name)
    {
        SceneManager.LoadScene(name);
    }

    public void MainMenu()
    {
        SceneManager.LoadScene(0);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void OpenOptions()
    {
        Game.OpenOptionsMenu();
        var menu = Game.MenuAnimation;
        if (menu != null) menu.SendToOptionsPosition();
    }

    public void OpenChapter1Select()
    {
        Game.OpenChapter1Select();
        var menu = Game.MenuAnimation;
        if (menu != null) menu.SendToLevelSelectPosition();
    }

    public void ResetTimes()
    {
        for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            var name = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));
            if (PlayerPrefs.HasKey("BestTime" + name))
            {
                PlayerPrefs.DeleteKey("BestTime" + name);
            }
        }
    }

    public void ResetBinds()
    {
        PlayerInput.ResetBindsToDefault();
    }

}