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

    public void LevelSelect()
    {
        SceneManager.LoadScene("LevelSelect");
    }

}