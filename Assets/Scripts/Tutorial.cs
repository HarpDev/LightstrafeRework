using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Tutorial : MonoBehaviour
{

    private int _stage = -1;
    private bool _pauseOnNextTick;

    public KeyDisplay strafeRight;
    public KeyDisplay strafeLeft;

    private void Start()
    {
        strafeLeft.gameObject.SetActive(false);
    }

    private void Update()
    {
        // velocity 0, 20, 30
        if (_stage == -1)
        {
            _stage++;
            Game.I.Player.LookScale = 0;
            Time.timeScale = 0;
        }

        if (_stage == 0)
        {
            if (Input.GetAxisRaw("Right") > 0)
            {
                Game.I.Player.velocity = new Vector3(0, 20, 30);
                Game.I.Player.LookScale = 1;
                Time.timeScale = 1;
                _stage++;
            }
        }
        if (Game.I.Player.transform.position.z > 250 && _stage < 2)
        {
            _stage++;
            Game.I.Player.Yaw = 0;
            Game.I.Player.Pitch = 0;
            _pauseOnNextTick = true;
            strafeLeft.gameObject.SetActive(true);
        }
        
        if (_stage == 2)
        {
            if (Input.GetAxisRaw("Right") < 0)
            {
                Game.I.Player.velocity = new Vector3(0, 20, 30);
                Game.I.Player.LookScale = 1;
                Time.timeScale = 1;
                _stage++;
            }
        }
        if (Game.I.Player.transform.position.z > 400 && _stage < 4)
        {
            _stage++;
            Game.I.Player.Yaw = 0;
            Game.I.Player.Pitch = 0;
            _pauseOnNextTick = true;
        }
        if (_stage == 4)
        {
            if (Input.GetAxisRaw("Right") < 0)
            {
                Game.I.Player.velocity = new Vector3(0, 20, 30);
                Game.I.Player.LookScale = 1;
                Time.timeScale = 1;
                _stage++;
            }
        }

      
    }

    private void FixedUpdate()
    {
        if (_pauseOnNextTick)
        {
            Game.I.Player.LookScale = 0;
            Time.timeScale = 0;
            _pauseOnNextTick = false;
        }
    }

}
