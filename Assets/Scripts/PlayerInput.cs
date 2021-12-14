using System;
using System.Collections.Generic;
using System.IO;
using FullSerializer;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public static int tickCount;

    // Binds must have the same variable name as their playerprefs entry
    public static int MoveForward { get { return PlayerPrefs.GetInt("MoveForward", (int)KeyCode.W); } private set { PlayerPrefs.SetInt("MoveForward", (int)value); } }
    public static int MoveBackward { get { return PlayerPrefs.GetInt("MoveBackward", (int)KeyCode.S); } private set { PlayerPrefs.SetInt("MoveBackward", (int)value); } }
    public static int MoveRight { get { return PlayerPrefs.GetInt("MoveRight", (int)KeyCode.D); } private set { PlayerPrefs.SetInt("MoveRight", (int)value); } }
    public static int MoveLeft { get { return PlayerPrefs.GetInt("MoveLeft", (int)KeyCode.A); } private set { PlayerPrefs.SetInt("MoveLeft", (int)value); } }
    public static int RestartLevel { get { return PlayerPrefs.GetInt("RestartLevel", (int)KeyCode.R); } private set { PlayerPrefs.SetInt("RestartLevel", (int)value); } }
    public static int PrimaryInteract { get { return PlayerPrefs.GetInt("PrimaryInteract", (int)KeyCode.Mouse0); } private set { PlayerPrefs.SetInt("PrimaryInteract", (int)value); } }
    public static int SecondaryInteract { get { return PlayerPrefs.GetInt("SecondaryInteract", (int)KeyCode.Mouse1); } private set { PlayerPrefs.SetInt("SecondaryInteract", (int)value); } }
    public static int TertiaryInteract { get { return PlayerPrefs.GetInt("TertiaryInteract", (int)KeyCode.Q); } private set { PlayerPrefs.SetInt("TertiaryInteract", (int)value); } }
    public static int Jump { get { return PlayerPrefs.GetInt("Jump", (int)KeyCode.Space); } private set { PlayerPrefs.SetInt("Jump", value); } }
    public static int Pause { get { return PlayerPrefs.GetInt("Pause", (int)KeyCode.Escape); } private set { PlayerPrefs.SetInt("Pause", (int)value); } }

    public static Dictionary<int, int> keyPressTimestamps = new Dictionary<int, int>();
    public static Dictionary<int, int> keyReleaseTimestamps = new Dictionary<int, int>();

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

    public static bool GetKeyDown(KeyCode key) { return GetKeyDown((int)key); }
    public static bool GetKeyDown(AlternateCode key) { return GetKeyDown((int)key); }

    private static bool GetKeyDown(int key)
    {
        if (key > 0) return Input.GetKeyDown((KeyCode)key);

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
        if (key > 0) return Input.GetKeyUp((KeyCode)key);

        return false;
    }

    public static bool GetKey(int key)
    {
        if (key > 0) return SincePressed(key) < SinceReleased(key);

        return false;
    }

    public static void ConsumeBuffer(int key)
    {
        if (playing) return;
        keyPressTimestamps[key] = -1;
        keyReleaseTimestamps[key] = -1;
    }

    public static float GetAxisStrafeRight()
    {
        if (playing)
        {
            return replay[tickCount].axisRight;
        }
        if (Input.GetKey((KeyCode)MoveRight))
            return Input.GetKey((KeyCode)MoveLeft) ? 0 : 1;
        else
            return Input.GetKey((KeyCode)MoveLeft) ? -1 : 0;
    }

    public static float GetAxisStrafeForward()
    {
        if (playing)
        {
            return replay[tickCount].axisForward;
        }
        if (Input.GetKey((KeyCode)MoveForward))
            return Input.GetKey((KeyCode)MoveBackward) ? 0 : 1;
        else
            return Input.GetKey((KeyCode)MoveBackward) ? -1 : 0;
    }

    public static void SimulateKeyPress(int key)
    {
        if (playing) return;
        keyPressTimestamps[key] = tickCount;
        keyReleaseTimestamps[key] = tickCount;
    }

    private struct ReplayTick
    {
        public Dictionary<int, int> keyPressTicks;
        public Dictionary<int, int> keyReleaseTicks;
        public float axisRight;
        public float axisForward;
        public float yaw;
        public float pitch;
        public Vector3 position;
        public Vector3 velocity;
    }

    private static bool recording = false;
    private static bool playing = false;
    private static Dictionary<int, ReplayTick> replay = new Dictionary<int, ReplayTick>();
    private float replayLastYaw;
    private float replayLastPitch;
    private float replayMouseInterpolation;
    private void FixedUpdate()
    {
        tickCount++;
        if (recording)
        {
            var presses = new Dictionary<int, int>(keyPressTimestamps);
            var releases = new Dictionary<int, int>(keyReleaseTimestamps);
            var tick = new ReplayTick
            {
                keyPressTicks = presses,
                keyReleaseTicks = releases,
                axisForward = GetAxisStrafeForward(),
                axisRight = GetAxisStrafeRight(),
                yaw = Game.Player.Yaw,
                pitch = Game.Player.Pitch,
                position = Game.Player.transform.position,
                velocity = Game.Player.velocity
            };
            replay[tickCount] = tick;
        }

        if (playing)
        {
            if (replay.Count < tickCount)
            {
                Debug.Log("replay stopped");
                Time.timeScale = 0;
                playing = false;
            }
            if (replay.ContainsKey(tickCount))
            {
                keyPressTimestamps = replay[tickCount].keyPressTicks;
                keyReleaseTimestamps = replay[tickCount].keyReleaseTicks;
                replayLastYaw = replay[tickCount].yaw;
                replayLastPitch = replay[tickCount].pitch;
                Game.Player.transform.position = replay[tickCount].position;
                Game.Player.velocity = replay[tickCount].velocity;
                replayMouseInterpolation = 0;
            }
        }
    }

    public static void WriteReplayToFile()
    {
        if (!recording) return;
        Debug.Log("replay saved to file");
        var s = new fsSerializer();
        recording = false;
        s.TrySerialize(replay, out var serial).AssertSuccessWithoutWarnings();
        File.WriteAllText("C:\\Users\\Fzzy\\Desktop\\replay.txt", fsJsonPrinter.CompressedJson(serial));
    }

    public static void ReadReplayFile(string path)
    {
        var s = new fsSerializer();
        var serial = File.ReadAllText(path);
        var json = fsJsonParser.Parse(serial);
        object deserialized = null;
        s.TryDeserialize(json, replay.GetType(), ref deserialized).AssertSuccessWithoutWarnings();
        recording = false;
        playing = true;
        replay = (Dictionary<int, ReplayTick>)deserialized;
        Debug.Log("reading replay from file\ntickCount: " + replay.Count);
    }
    
    public static int GetBindByName(string name)
    {
        var properties = typeof(PlayerInput).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly);
        foreach (var prop in properties)
        {
            var existing = (KeyCode)prop.GetValue(null, null);
            if (prop.Name == name) return (int)existing;
        }
        return (int)KeyCode.None;
    }

    public static string GetBindName(int key)
    {
        if (key >= 0)
        {
            return ((KeyCode)key).ToString();
        } else
        {
            return ((AlternateCode)key).ToString();
        }
    }

    public static void SetBind(string key, KeyCode bind)
    {
        SetBind(key, (int)bind);
    }
    public static void SetBind(string key, AlternateCode bind)
    {
        SetBind(key, (int)bind);
    }
    private static void SetBind(string key, int bind)
    {
        var properties = typeof(PlayerInput).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly);
        foreach (var prop in properties)
        {
            var existing = (int)prop.GetValue(null, null);
            if (existing == bind)
            {
                prop.SetValue(null, KeyCode.None);
            }
        }

        typeof(PlayerInput).GetProperty(key).SetValue(null, bind);
    }

    private void Update()
    {
        if (playing)
        {
            Game.Player.Yaw = Mathf.Lerp(replayLastYaw, replay[tickCount].yaw, replayMouseInterpolation);
            Game.Player.Pitch = Mathf.Lerp(replayLastPitch, replay[tickCount].pitch, replayMouseInterpolation);
            replayMouseInterpolation += Time.deltaTime;
            return;
        }
        var properties = typeof(PlayerInput).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly);
        foreach (var prop in properties)
        {
            var key = (int)prop.GetValue(null, null);
            if (key > 0)
            {
                if (Input.GetKeyDown((KeyCode)key))
                {
                    keyPressTimestamps[key] = tickCount;
                }
                if (Input.GetKeyUp((KeyCode)key))
                {
                    keyReleaseTimestamps[key] = tickCount;
                }
            } else if (key == -1)
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