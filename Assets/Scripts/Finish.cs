using UnityEngine;

public class Finish : MonoBehaviour
{
    public bool realFinish;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!realFinish)
            {
                Game.Score = 0;
            }
            Game.StartMenu();
            if (Game.Score > Game.HighScore)
            {
                Game.HighScore = Game.Score;
            }
        }
    }
}
