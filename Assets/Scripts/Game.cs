
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Game : MonoBehaviour
{
    
    public static Game I;

    public static int Score { get; set; }
    public static int HighScore { get; set; }
    public PlayerControls Player { get; set; }

    public Hitmarker hitmarker;
    public Hitmarker critmarker;

    private void Awake()
    {
        if (I == null)
        {
            DontDestroyOnLoad(gameObject);
            I = this;
            Application.targetFrameRate = 300;
        }
        else if (I != this)
        {
            Destroy(gameObject);
        }
    }

    public static void StartLevel()
    {
        Score = 0;
        SceneManager.LoadScene("Level1");
    }

    public static void StartMenu()
    {
        SceneManager.LoadScene("Menu");
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
