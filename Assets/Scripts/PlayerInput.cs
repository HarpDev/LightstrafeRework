using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public static int tickCount;
    
    public enum Key
    {
        StrafeRight = KeyCode.D,
        StrafeLeft = KeyCode.A,
        StrafeForward = KeyCode.W,
        StrafeBack = KeyCode.S,
        Slide = KeyCode.LeftControl,
        RestartLevel = KeyCode.R,
        FireBow = KeyCode.Mouse0,
        Jump = KeyCode.Space,
        Pause = KeyCode.Escape
    }

    private class KeyData
    {
        public int pressedTimestamp;
    }

    private static Dictionary<Key, KeyData> keys = new Dictionary<Key, KeyData>();

    public static int SincePressed(Key key)
    {
        return tickCount - keys[key].pressedTimestamp;
    }

    public static float GetAxisStrafeRight()
    {
        if (Input.GetKey((KeyCode) Key.StrafeRight))
            return Input.GetKey((KeyCode) Key.StrafeLeft) ? 0 : 1;
        else
            return Input.GetKey((KeyCode) Key.StrafeLeft) ? -1 : 0;
    }

    public static float GetAxisStrafeForward()
    {
        if (Input.GetKey((KeyCode) Key.StrafeForward))
            return Input.GetKey((KeyCode) Key.StrafeBack) ? 0 : 1;
        else
            return Input.GetKey((KeyCode) Key.StrafeBack) ? -1 : 0;
    }

    private void Start()
    {
        foreach (var k in Enum.GetValues(typeof(Key)))
        {
            keys[(Key) k] = new KeyData();
        }
    }

    private void FixedUpdate()
    {
        tickCount++;
    }

    private void Update()
    {
        var list = new List<Key>(keys.Keys);
        foreach (var key in list)
        {
            if (Input.GetKeyDown((KeyCode) key))
            {
                keys[key].pressedTimestamp = tickCount;
            }
        }
    }
}