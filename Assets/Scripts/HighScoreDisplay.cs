using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HighScoreDisplay : MonoBehaviour
{
    public Text highScoreText;
    public string prefix;

    // Update is called once per frame
    private void Update()
    {
        highScoreText.text = prefix + Game.HighScore;
    }
}
