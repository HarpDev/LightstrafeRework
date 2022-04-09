using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public int tickCount;
    public bool usingAnalog;

    private static PlayerInput I;

    private Player player;

    private void Awake()
    {
        if (I == null) Game.OnAwakeBind(this);
    }

    private void Start()
    {
        if (I == null)
        {
            DontDestroyOnLoad(gameObject);
            I = this;
        }
        else if (I != this)
        {
            I.Start();
            Destroy(gameObject);
            return;
        }

        player = Game.OnStartResolve<Player>();
        tickCount = 0;

        keyPressTimestamps = new Dictionary<int, int>();
        keyReleaseTimestamps = new Dictionary<int, int>();
    }

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

    public Dictionary<int, int> keyPressTimestamps = new Dictionary<int, int>();
    public Dictionary<int, int> keyReleaseTimestamps = new Dictionary<int, int>();

    public enum AlternateCode
    {
        ScrollUp = -1,
        ScrollDown = -2
    }

    private int GetPressTimestamp(int key)
    {
        var exists = keyPressTimestamps.TryGetValue(key, out int time);
        if (exists) return time;
        else return -1;
    }

    public int SincePressed(int key)
    {
        var data = GetPressTimestamp(key);
        if (data == -1) return int.MaxValue;
        return tickCount - data;
    }

    private int GetReleaseTimestamp(int key)
    {
        var exists = keyReleaseTimestamps.TryGetValue(key, out int time);
        if (exists) return time;
        else return -1;
    }


    public int SinceReleased(int key)
    {
        var data = GetReleaseTimestamp(key);
        if (data == -1) return int.MaxValue;
        return tickCount - data;
    }

    public void ConsumeBuffer(int key)
    {
        keyPressTimestamps[key] = -1;
        keyReleaseTimestamps[key] = -1;
    }

    public bool IsKeyPressed(int key)
    {
        return SincePressed(key) < SinceReleased(key);
    }

    public float GetAxisStrafeRight()
    {
        float v;
        if (Input.GetKey((KeyCode) MoveRight))
            v = IsKeyPressed(MoveLeft) ? 0 : 1;
        else
            v = IsKeyPressed(MoveLeft) ? -1 : 0;

        if (v == 0)
        {
            v = Input.GetAxis("Joy 1 X");
        }

        return v;
    }

    public float GetAxisStrafeForward()
    {
        float v;
        if (Input.GetKey((KeyCode) MoveForward))
            v = IsKeyPressed(MoveBackward) ? 0 : 1;
        else
            v = IsKeyPressed(MoveBackward) ? -1 : 0;

        if (v == 0)
        {
            v = -Input.GetAxis("Joy 1 Y");
        }

        return v;
    }

    public void SimulateKeyPress(int key)
    {
        keyPressTimestamps[key] = tickCount;
    }

    public void SimulateKeyRelease(int key)
    {
        keyReleaseTimestamps[key] = tickCount;
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

    private void FixedUpdate()
    {
        usingAnalog = Mathf.Abs(Input.GetAxis("Joy 1 Y")) > 0.1f || Mathf.Abs(Input.GetAxis("Joy 1 X")) > 0.1f;
        tickCount++;
    }

    private void Update()
    {
        var properties = typeof(PlayerInput).GetProperties(System.Reflection.BindingFlags.Public |
                                                           System.Reflection.BindingFlags.Static |
                                                           System.Reflection.BindingFlags.DeclaredOnly);
        foreach (var prop in properties)
        {
            var key = (int) prop.GetValue(null, null);
            if (Game.playingReplay && key != Pause) return;
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