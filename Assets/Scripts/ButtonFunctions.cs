using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonFunctions : MonoBehaviour
{

    public void RestartLevel()
    {
        Game.RestartLevel();
    }

    public void ReturnToLastCheckpoint()
    {
        Game.ReturnToLastCheckpoint();
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
    }

    public void OpenChapter1Select()
    {
        Game.OpenChapter1Select();
    }

   

}