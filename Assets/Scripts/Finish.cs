using System;
using System.Collections.Generic;
using UnityEngine;

public class Finish : MonoBehaviour
{

    public int TargetsHit { get; set; }
    public int totalTargets;

    private void Awake()
    {
        TargetsHit = 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (TargetsHit < totalTargets)
        {
            Game.RestartLevel();
        }
        else if (other.CompareTag("Player"))
        {
            Game.FinalTime = Environment.TickCount - Game.LevelStartTime;
            Game.StartMenu();
            if (Game.FinalTime < Game.BestTime)
            {
                Game.BestTime = Game.FinalTime;
            }
        }
    }
}
