using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FullSerializer;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public static int tickCount;
    public static bool usingAnalog;

    // Binds must have the same variable name as their playerprefs entry
    public static int MoveForward
    {
        get { return PlayerPrefs.GetInt("MoveForward", (int) KeyCode.W); }
        private set { PlayerPrefs.SetInt("MoveForward", (int) value); }
    }

    public static int MoveBackward
    {
        get { return PlayerPrefs.GetInt("MoveBackward", (int) KeyCode.S); }
        private set { PlayerPrefs.SetInt("MoveBackward", (int) value); }
    }

    public static int MoveRight
    {
        get { return PlayerPrefs.GetInt("MoveRight", (int) KeyCode.D); }
        private set { PlayerPrefs.SetInt("MoveRight", (int) value); }
    }

    public static int MoveLeft
    {
        get { return PlayerPrefs.GetInt("MoveLeft", (int) KeyCode.A); }
        private set { PlayerPrefs.SetInt("MoveLeft", (int) value); }
    }

    public static int RestartLevel
    {
        get { return PlayerPrefs.GetInt("RestartLevel", (int) KeyCode.R); }
        private set { PlayerPrefs.SetInt("RestartLevel", (int) value); }
    }

    public static int PrimaryInteract
    {
        get { return PlayerPrefs.GetInt("PrimaryInteract", (int) KeyCode.Mouse0); }
        private set { PlayerPrefs.SetInt("PrimaryInteract", (int) value); }
    }

    public static int SecondaryInteract
    {
        get { return PlayerPrefs.GetInt("SecondaryInteract", (int) KeyCode.Mouse1); }
        private set { PlayerPrefs.SetInt("SecondaryInteract", (int) value); }
    }

    public static int TertiaryInteract
    {
        get { return PlayerPrefs.GetInt("TertiaryInteract", (int) KeyCode.Q); }
        private set { PlayerPrefs.SetInt("TertiaryInteract", (int) value); }
    }

    public static int Jump
    {
        get { return PlayerPrefs.GetInt("Jump", (int) KeyCode.Space); }
        private set { PlayerPrefs.SetInt("Jump", value); }
    }

    public static int Pause
    {
        get { return PlayerPrefs.GetInt("Pause", (int) KeyCode.Escape); }
        private set { PlayerPrefs.SetInt("p", (int) value); }
    }

    public static Dictionary<int, int> keyPressTimestamps = new();
    public static Dictionary<int, int> keyReleaseTimestamps = new();

    public enum AlternateCode
    {
        ScrollUp = -1,
        ScrollDown = -2
    }

    private static int GetPressTimestamp(int key)
    {
        var exists = keyPressTimestamps.TryGetValue(key, out int time);
        if (exists) return time;
        else return -1;
    }

    public static int SincePressed(int key)
    {
        var data = GetPressTimestamp(key);
        if (data == -1) return int.MaxValue;
        return tickCount - data;
    }

    private static int GetReleaseTimestamp(int key)
    {
        var exists = keyReleaseTimestamps.TryGetValue(key, out int time);
        if (exists) return time;
        else return -1;
    }


    public static int SinceReleased(int key)
    {
        var data = GetReleaseTimestamp(key);
        if (data == -1) return int.MaxValue;
        return tickCount - data;
    }

    public static bool GetKeyDown(KeyCode key)
    {
        return GetKeyDown((int) key);
    }

    public static bool GetKeyDown(AlternateCode key)
    {
        return GetKeyDown((int) key);
    }

    private static bool GetKeyDown(int key)
    {
        if (key > 0) return Input.GetKeyDown((KeyCode) key);

        if (key == -1)
        {
            return Input.mouseScrollDelta.y == 1;
        }
        else if (key == -2)
        {
            return Input.mouseScrollDelta.y == -1;
        }

        return false;
    }

    public static bool GetKeyUp(int key)
    {
        if (key > 0) return Input.GetKeyUp((KeyCode) key);

        return false;
    }

    public static bool GetKey(int key)
    {
        if (key > 0) return SincePressed(key) < SinceReleased(key);

        return false;
    }

    public static void ConsumeBuffer(int key)
    {
        keyPressTimestamps[key] = -1;
        keyReleaseTimestamps[key] = -1;
    }

    public static float GetAxisStrafeRight()
    {
        float v;
        if (Input.GetKey((KeyCode) MoveRight))
            v = Input.GetKey((KeyCode) MoveLeft) ? 0 : 1;
        else
            v = Input.GetKey((KeyCode) MoveLeft) ? -1 : 0;

        if (v == 0)
        {
            v = Input.GetAxis("Joy 1 X");
        }

        return v;
    }

    public static float GetAxisStrafeForward()
    {
        float v;
        if (Input.GetKey((KeyCode) MoveForward))
            v = Input.GetKey((KeyCode) MoveBackward) ? 0 : 1;
        else
            v = Input.GetKey((KeyCode) MoveBackward) ? -1 : 0;

        if (v == 0)
        {
            v = -Input.GetAxis("Joy 1 Y");
        }

        return v;
    }

    public static void SimulateKeyPress(int key)
    {
        keyPressTimestamps[key] = tickCount;
        keyReleaseTimestamps[key] = tickCount;
    }

    private void FixedUpdate()
    {
        usingAnalog = Mathf.Abs(Input.GetAxis("Joy 1 Y")) > 0.1f || Mathf.Abs(Input.GetAxis("Joy 1 X")) > 0.1f;
        tickCount++;
    }

    public static int GetBindByName(string name)
    {
        var properties = typeof(PlayerInput).GetProperties(System.Reflection.BindingFlags.Public |
                                                           System.Reflection.BindingFlags.Static |
                                                           System.Reflection.BindingFlags.DeclaredOnly);
        foreach (var prop in properties)
        {
            var existing = (KeyCode) prop.GetValue(null, null);
            if (prop.Name == name) return (int) existing;
        }

        return (int) KeyCode.None;
    }

    public static string GetBindName(int key)
    {
        if (key >= 0)
        {
            return ((KeyCode) key).ToString();
        }
        else
        {
            return ((AlternateCode) key).ToString();
        }
    }

    public static void SetBind(string key, KeyCode bind)
    {
        SetBind(key, (int) bind);
    }

    public static void SetBind(string key, AlternateCode bind)
    {
        SetBind(key, (int) bind);
    }

    private static void SetBind(string key, int bind)
    {
        var properties = typeof(PlayerInput).GetProperties(System.Reflection.BindingFlags.Public |
                                                           System.Reflection.BindingFlags.Static |
                                                           System.Reflection.BindingFlags.DeclaredOnly);
        foreach (var prop in properties)
        {
            var existing = (int) prop.GetValue(null, null);
            if (existing == bind)
            {
                prop.SetValue(null, KeyCode.None);
            }
        }

        typeof(PlayerInput).GetProperty(key).SetValue(null, bind);
    }

    public static void ResetBindsToDefault()
    {
        var properties = typeof(PlayerInput).GetProperties(System.Reflection.BindingFlags.Public |
                                                           System.Reflection.BindingFlags.Static |
                                                           System.Reflection.BindingFlags.DeclaredOnly);
        foreach (var prop in properties)
        {
            PlayerPrefs.DeleteKey(prop.Name);
        }
    }

    private void Update()
    {
        var properties = typeof(PlayerInput).GetProperties(System.Reflection.BindingFlags.Public |
                                                           System.Reflection.BindingFlags.Static |
                                                           System.Reflection.BindingFlags.DeclaredOnly);
        foreach (var prop in properties)
        {
            var key = (int) prop.GetValue(null, null);
            if (key != Pause && Time.timeScale == 0) continue;
            if (key > 0)
            {
                if (Input.GetKeyDown((KeyCode) key))
                {
                    keyPressTimestamps[key] = tickCount;
                }

                if (Input.GetKeyUp((KeyCode) key))
                {
                    keyReleaseTimestamps[key] = tickCount;
                }
            }
            else if (key == -1)
            {
                if (Input.mouseScrollDelta.y > 0) keyPressTimestamps[key] = tickCount;
            }
            else if (key == -2)
            {
                if (Input.mouseScrollDelta.y < 0) keyPressTimestamps[key] = tickCount;
            }
        }
    }
}