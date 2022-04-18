using FullSerializer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ReplayGhost : MonoBehaviour
{
    private Timers timers;
    public static Game.Replay replay;

    private void Awake()
    {
        var replaysFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Lightstrafe\replays";
        Directory.CreateDirectory(replaysFolder);
        var files = Directory.GetFiles(replaysFolder);
        Game.Replay fastest = null;
        for (var i = 0; i < files.Length; i++)
        {
            var file = files[i];
            var json = File.ReadAllText(file);
            fsData data = fsJsonParser.Parse(json);
            var fs = new fsSerializer();
            Game.Replay replay = null;
            fs.TryDeserialize<Game.Replay>(data, ref replay);
            if (i == 0) fastest = replay;

            if (replay.scene == SceneManager.GetActiveScene().buildIndex)
            {
                if (replay.finalTimeTickCount < fastest.finalTimeTickCount)
                {
                    fastest = replay;
                }
            }
        }

        replay = fastest;
        if (replay != null) transform.position = replay.ticks[0].pP;
    }

    private void Start()
    {
        timers = Game.OnStartResolve<Timers>();
    }

    private void Update()
    {
        if (replay == null) return;
        if (!startGhost) return;
        interpolationDelta += Time.deltaTime;
        transform.position = Vector3.Lerp(currentPos, nextPos, interpolationDelta / (Time.fixedDeltaTime * replay.everyNTicks));
    }

    private bool startGhost = false;

    private Vector3 currentPos;
    private Vector3 nextPos;

    private float interpolationDelta;

    private void FixedUpdate()
    {
        if (replay == null) return;
        if (replay.scene != SceneManager.GetActiveScene().buildIndex) return;
        if (replay.ticks.ContainsKey(timers.CurrentLevelTickCount + replay.startTick))
        {
            startGhost = true;
            var tick = replay.ticks[timers.CurrentLevelTickCount + replay.startTick];
            currentPos = tick.pP;
            nextPos = tick.pP;
            if (replay.ticks.ContainsKey(timers.CurrentLevelTickCount + replay.startTick + replay.everyNTicks))
            {
                var nextTick = replay.ticks[timers.CurrentLevelTickCount + replay.startTick + replay.everyNTicks];
                nextPos = nextTick.pP;
            }
            interpolationDelta = 0;
        }
    }
}
