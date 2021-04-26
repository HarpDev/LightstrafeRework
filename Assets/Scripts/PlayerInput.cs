using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public static int tickCount;

    // Binds must have the same variable name as their playerprefs entry
    public static KeyCode MoveForward { get { return (KeyCode)PlayerPrefs.GetInt("MoveForward", (int)KeyCode.W); } private set { PlayerPrefs.SetInt("MoveForward", (int)value); } }
    public static KeyCode MoveBackward { get { return (KeyCode)PlayerPrefs.GetInt("MoveBackward", (int)KeyCode.S); } private set { PlayerPrefs.SetInt("MoveBackward", (int)value); } }
    public static KeyCode MoveRight { get { return (KeyCode)PlayerPrefs.GetInt("MoveRight", (int)KeyCode.D); } private set { PlayerPrefs.SetInt("MoveRight", (int)value); } }
    public static KeyCode MoveLeft { get { return (KeyCode)PlayerPrefs.GetInt("MoveLeft", (int)KeyCode.A); } private set { PlayerPrefs.SetInt("MoveLeft", (int)value); } }
    public static KeyCode LastCheckpoint { get { return (KeyCode)PlayerPrefs.GetInt("LastCheckpoint", (int)KeyCode.R); } private set { PlayerPrefs.SetInt("LastCheckpoint", (int)value); } }
    public static KeyCode RestartLevel { get { return (KeyCode)PlayerPrefs.GetInt("RestartLevel", (int)KeyCode.None); } private set { PlayerPrefs.SetInt("RestartLevel", (int)value); } }
    public static KeyCode PrimaryInteract { get { return (KeyCode)PlayerPrefs.GetInt("PrimaryInteract", (int)KeyCode.Mouse0); } private set { PlayerPrefs.SetInt("PrimaryInteract", (int)value); } }
    public static KeyCode SecondaryInteract { get { return (KeyCode)PlayerPrefs.GetInt("SecondaryInteract", (int)KeyCode.Mouse1); } private set { PlayerPrefs.SetInt("SecondaryInteract", (int)value); } }
    public static KeyCode Jump { get { return (KeyCode)PlayerPrefs.GetInt("Jump", (int)KeyCode.Space); } private set { PlayerPrefs.SetInt("Jump", (int)value); } }
    public static KeyCode Pause { get { return (KeyCode)PlayerPrefs.GetInt("Pause", (int)KeyCode.Escape); } private set { PlayerPrefs.SetInt("Pause", (int)value); } }
    public static KeyCode Slide { get { return (KeyCode)PlayerPrefs.GetInt("Slide", (int)KeyCode.LeftControl); } private set { PlayerPrefs.SetInt("Slide", (int)value); } }

    private static Dictionary<KeyCode, int> keys = new Dictionary<KeyCode, int>();

    private static int GetData(KeyCode key)
    {
        var exists = keys.TryGetValue(key, out int time);
        if (exists) return time;
        else return -1;
    }

    public static int SincePressed(KeyCode key)
    {
        var data = GetData(key);
        if (data == -1) return int.MaxValue;
        return tickCount - data;
    }

    public static void ConsumeBuffer(KeyCode key)
    {
        keys[key] = -1;
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

    public static void SimulateKeyPress(KeyCode key)
    {
        keys[key] = tickCount;
    }

    private void FixedUpdate()
    {
        tickCount++;
    }

    public static KeyCode GetBindByName(string name)
    {
        var properties = typeof(PlayerInput).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly);
        foreach (var prop in properties)
        {
            var existing = (KeyCode)prop.GetValue(null, null);
            if (prop.Name == name) return existing;
        }
        return KeyCode.None;
    }

    public static void SetBind(string key, KeyCode bind)
    {
        var properties = typeof(PlayerInput).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly);
        foreach (var prop in properties)
        {
            var existing = (KeyCode)prop.GetValue(null, null);
            if (existing == bind)
            {
                prop.SetValue(null, KeyCode.None);
            }
        }

        typeof(PlayerInput).GetProperty(key).SetValue(null, bind);
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