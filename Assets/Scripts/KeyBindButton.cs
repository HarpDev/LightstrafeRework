using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KeyBindButton : MonoBehaviour
{

    public string bindName;
    public Text text;

    private bool rebinding = false;

    private PlayerInput input;

    private void Start()
    {
        input = Game.OnStartResolve<PlayerInput>();
        text.text = GetValue();
    }

    private void Update()
    {
        if (rebinding)
        {
            foreach (KeyCode kcode in Enum.GetValues(typeof(KeyCode)))
            {
                if (input.GetKeyDown(kcode))
                {
                    PlayerInput.SetBind(bindName, kcode);
                    text.text = kcode.ToString();
                    rebinding = false;
                    break;
                }
            }
            foreach (PlayerInput.AlternateCode kcode in Enum.GetValues(typeof(PlayerInput.AlternateCode)))
            {
                if (input.GetKeyDown(kcode))
                {
                    PlayerInput.SetBind(bindName, kcode);
                    text.text = kcode.ToString();
                    rebinding = false;
                    break;
                }
            }
        } else
        {
            text.text = PlayerInput.GetBindName(PlayerInput.GetBindByName(bindName));
        }
    }

    private string GetValue()
    {
        return typeof(PlayerInput).GetProperty(bindName).GetValue(null, null).ToString();
    }

    public void ReBind()
    {
        text.text = "Press a Key";
        rebinding = true;
    }
}
