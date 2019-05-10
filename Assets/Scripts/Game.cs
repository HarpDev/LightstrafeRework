
using UnityEngine;
using UnityEngine.UI;

public class Game : MonoBehaviour
{
    
    public static Game I;

    public Hitmarker hitmarker;

    public Text speedText;
    public Text scoreText;

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

    private void Update()
    {
        speedText.text = "Speed: " + (int)Player.velocity.magnitude;
        scoreText.text = "Score: " + Score;
    }
}
