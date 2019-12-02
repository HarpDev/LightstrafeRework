using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public static int tickCount;

    public static KeyCode MoveForward { get { return (KeyCode)PlayerPrefs.GetInt("MoveForward", (int)KeyCode.W); } set { PlayerPrefs.SetInt("MoveForward", (int)value); } }
    public static KeyCode MoveBackward { get { return (KeyCode)PlayerPrefs.GetInt("MoveBackward", (int)KeyCode.S); } set { PlayerPrefs.SetInt("MoveBackward", (int)value); } }
    public static KeyCode MoveRight { get { return (KeyCode)PlayerPrefs.GetInt("MoveRight", (int)KeyCode.D); } set { PlayerPrefs.SetInt("MoveRight", (int)value); } }
    public static KeyCode MoveLeft { get { return (KeyCode)PlayerPrefs.GetInt("MoveLeft", (int)KeyCode.A); } set { PlayerPrefs.SetInt("MoveLeft", (int)value); } }
    public static KeyCode RestartLevel { get { return (KeyCode)PlayerPrefs.GetInt("RestartLevel", (int)KeyCode.R); } set { PlayerPrefs.SetInt("RestartLevel", (int)value); } }
    public static KeyCode PrimaryInteract { get { return (KeyCode)PlayerPrefs.GetInt("PrimaryInteract", (int)KeyCode.Mouse0); } set { PlayerPrefs.SetInt("PrimaryInteract", (int)value); } }
    public static KeyCode SecondaryInteract { get { return (KeyCode)PlayerPrefs.GetInt("SecondaryInteract", (int)KeyCode.Mouse1); } set { PlayerPrefs.SetInt("SecondaryInteract", (int)value); } }
    public static KeyCode Jump { get { return (KeyCode)PlayerPrefs.GetInt("Jump", (int)KeyCode.Space); } set { PlayerPrefs.SetInt("Jump", (int)value); } }
    public static KeyCode Pause { get { return (KeyCode)PlayerPrefs.GetInt("Pause", (int)KeyCode.Escape); } set { PlayerPrefs.SetInt("Pause", (int)value); } }

    private static Dictionary<KeyCode, int> keys = new Dictionary<KeyCode, int>();

    private static int GetData(KeyCode key)
    {
        var exists = keys.TryGetValue(key, out int time);
        if (exists) return time;
        else return 0;
    }

    public static int SincePressed(KeyCode key)
    {
        return tickCount - GetData(key);
    }

    public static void ClearSincePressed(KeyCode key)
    {
        keys[key] = 0;
    }

    public static float GetAxisStrafeRight()
    {
        if (Input.GetKey(MoveRight))
            return Input.GetKey(MoveLeft) ? 0 : 1;
        else
            return Input.GetKey(MoveLeft) ? -1 : 0;
    }

    public static float GetAxisStrafeForward()
    {
        if (Input.GetKey(MoveForward))
            return Input.GetKey(MoveBackward) ? 0 : 1;
        else
            return Input.GetKey(MoveBackward) ? -1 : 0;
    }

    private void FixedUpdate()
    {
        tickCount++;
    }

    private void Update()
    {
        var properties = typeof(PlayerInput).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly);
        foreach (var prop in properties)
        {
            var key = (KeyCode)prop.GetValue(null, null);
            if (Input.GetKeyDown(key))
            {
                keys[key] = tickCount;
            }
        }
    }
}