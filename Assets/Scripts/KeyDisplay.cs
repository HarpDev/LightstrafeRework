using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KeyDisplay : MonoBehaviour
{

    public Text key;

    public string keycode;

    public Image image;

    public Sprite keyup;
    public Sprite keydown;

    private int _fontSize;
    private Vector3 _position;

    private void Start()
    {
        _fontSize = key.fontSize;
        _position = key.transform.position;
    }

    private void Update()
    {
        var pressed = Input.GetKey(keycode);
        if (pressed)
        {
            image.sprite = keydown;
            key.fontSize = _fontSize - 5;
            key.transform.position = _position - new Vector3(0, 1, 0);
        }
        else
        {
            image.sprite = keyup;
            key.fontSize = _fontSize;
            key.transform.position = _position;
        }
    }
}
