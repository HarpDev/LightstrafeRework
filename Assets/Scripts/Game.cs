using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour
{
    private static Game I;

    private static List<object> bindings = new();

    public static void OnAwakeBind<T>(T obj)
    {
        for (var i = bindings.Count - 1; i >= 0; i--)
        {
            var b = bindings[i];
            if (b.GetType() == typeof(T))
            {
                bindings.RemoveAt(i);
                break;
            }
        }

        bindings.Add(obj);
    }

    public static T OnStartResolve<T>()
    {
        T bound = default;
        for (var i = bindings.Count - 1; i >= 0; i--)
        {
            var b = bindings[i];
            if (b == null)
            {
                bindings.RemoveAt(i);
                continue;
            }
            if (b.GetType() == typeof(T))
            {
                bound = (T) b;
            }
        }

        return bound;
    }

    private void Start()
    {
        if (I == null)
        {
            DontDestroyOnLoad(gameObject);
            I = this;
            Application.targetFrameRate = 144;
        }
        else if (I != this)
        {
            I.Start();
            Destroy(gameObject);
        }
    }

    public static void StartLevel(string level)
    {
        SceneManager.LoadScene(level);
    }
}