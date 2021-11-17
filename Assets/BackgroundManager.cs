using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class BackgroundManager : MonoBehaviour
{
    public GameObject line;
    public int lineAmount = 5;
    public float occilation = 10;
    public float speed = 10;

    private Random random;

    private GameObject[] lines;
    
    private void Start()
    {
        random = new Random();
        lines = new GameObject[lineAmount];
        for (var i = 0; i < lineAmount; i++)
        {
            lines[i] = Instantiate(line);
            var comp = lines[i].GetComponent<BackgroundLine>();
            comp.degreeOffset = (360f / lineAmount) * i;
            comp.manager = this;
        }
    }

    private float nextChange = 4;

    private void FixedUpdate()
    {
        nextChange -= Time.fixedDeltaTime;
        if (nextChange <= 0)
        {
            nextChange = random.Next(4) + 3;
            occilation = random.Next(4) * lineAmount + 15;
            speed = random.Next(2) * 10 + 25;
        }
    }
}
