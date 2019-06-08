
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Game : MonoBehaviour
{
    
    public static Game I;

    public int Score { get; set; }
    public PlayerControls Player { get; set; }

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

    public void StartLevel()
    {
        SceneManager.LoadScene("Level1");
    }
}
