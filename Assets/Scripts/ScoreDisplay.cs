using System;
using UnityEngine;
using UnityEngine.UI;

public class ScoreDisplay : MonoBehaviour
{
    public Text scoreText;
    public string prefix;

    // Update is called once per frame
    private void Update()
    {
        scoreText.text = prefix + Game.I.Score;
    }
}
