using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour
{
    private static Game I;

    private static List<object> bindings = new List<object>();

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

    private PlayerInput input;
    private Player player;
    private Level level;

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
            return;
        }

        input = OnStartResolve<PlayerInput>();
        player = OnStartResolve<Player>();
        level = OnStartResolve<Level>();
    }

    private void OnEnable()
    {
        if (I != null) return;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public static void StartLevel(string level)
    {
        SceneManager.LoadScene(level);
    }

    public class ReplayTick
    {
        //public List<int> kp;
        //public List<int> kr;
        public Vector3 pV;
        public Vector3 pP;
        public float py;
        public float pp;
    }

    public class Replay
    {
        public Dictionary<int, List<int>> keyPresses = new Dictionary<int, List<int>>();
        public Dictionary<int, List<int>> keyReleases = new Dictionary<int, List<int>>();
        public int scene;
        public Dictionary<int, ReplayTick> ticks = new Dictionary<int, ReplayTick>();
        public int everyNTicks = 2;
    }

    private static Replay currentReplay;
    public static bool playingReplay;
    public static bool replayFinishedPlaying;

    public static void PlayReplay(Replay replay)
    {
        currentReplay = replay;
        playingReplay = true;
        replayIgnoreUnload = true;
        replayFinishedPlaying = false;
        SceneManager.LoadScene(replay.scene);
    }

    public static bool SaveReplay;

    private static bool replayIgnoreUnload;

    private void OnSceneUnloaded(Scene scene)
    {
        if (SaveReplay)
        {
            SaveReplay = false;
            if (currentReplay.ticks.Count > 0 && !playingReplay)
            {
                var sets = new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };
                var replaysFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                                    @"\Lightstrafe\replays";
                Directory.CreateDirectory(replaysFolder);

                var dateTime = DateTime.Now.ToString("MM-dd-y hh:mmtt");
                dateTime = dateTime.Replace("/", "-");
                dateTime = dateTime.Replace(":", "-");

                using var file = File.CreateText(replaysFolder + @"\" + scene.name + " " + dateTime + ".json");
                var serializer = JsonSerializer.Create(sets);
                serializer.Serialize(file, currentReplay);
            }
        }

        if (replayIgnoreUnload)
        {
            replayIgnoreUnload = false;
        }
        else
        {
            playingReplay = false;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!playingReplay)
        {
            currentReplay = new Replay
            {
                scene = SceneManager.GetActiveScene().buildIndex,
                everyNTicks = 4
            };
        }
    }

    private void Update()
    {
        interpolationDelta += Time.deltaTime;
        
        if (input != null && player != null && playingReplay)
        {
            var interpolatedYaw = Mathf.Lerp(lastYaw, currentYaw, interpolationDelta / (Time.fixedDeltaTime * currentReplay.everyNTicks));
            var interpolatedPitch = Mathf.Lerp(lastPitch, currentPitch, interpolationDelta / (Time.fixedDeltaTime * currentReplay.everyNTicks));

            player.Yaw = interpolatedYaw;
            player.Pitch = interpolatedPitch;
        }
    }

    private float lastYaw;
    private float currentYaw;
    private float lastPitch;
    private float currentPitch;

    private float interpolationDelta;

    private void FixedUpdate()
    {
        if (input != null && player != null)
        {
            if (playingReplay)
            {
                var endTick = currentReplay.ticks.Keys.Prepend(0).Max();
                if (input.tickCount <= endTick)
                {
                    if (currentReplay.keyPresses.ContainsKey(input.tickCount))
                    {
                        foreach (var key in currentReplay.keyPresses[input.tickCount])
                        {
                            input.SimulateKeyPress(key);
                        }
                    }

                    if (currentReplay.keyReleases.ContainsKey(input.tickCount))
                    {
                        foreach (var key in currentReplay.keyReleases[input.tickCount])
                        {
                            input.SimulateKeyRelease(key);
                        }
                    }

                    if (currentReplay.ticks.ContainsKey(input.tickCount))
                    {
                        var tick = currentReplay.ticks[input.tickCount];
                        player.velocity = tick.pV;
                        player.transform.position = tick.pP;
                        lastYaw = currentYaw;
                        lastPitch = currentPitch;
                        currentYaw = tick.py;
                        currentPitch = tick.pp;
                        interpolationDelta = 0;
                    }
                }
                else
                {
                    Time.timeScale = 0;
                    replayFinishedPlaying = true;
                }
            }
            else
            {
                var properties = typeof(PlayerInput).GetProperties(System.Reflection.BindingFlags.Public |
                                                                   System.Reflection.BindingFlags.Static |
                                                                   System.Reflection.BindingFlags.DeclaredOnly);
                var presses = new List<int>();
                var releases = new List<int>();

                foreach (var prop in properties)
                {
                    var key = (int) prop.GetValue(null, null);
                    if (input.SincePressed(key) == 0)
                    {
                        presses.Add(key);
                    }

                    if (input.SinceReleased(key) == 0)
                    {
                        releases.Add(key);
                    }
                }

                if (presses.Count > 0) currentReplay.keyPresses[input.tickCount] = presses;
                if (releases.Count > 0) currentReplay.keyReleases[input.tickCount] = releases;
                
                if (input.tickCount % currentReplay.everyNTicks == 0)
                {
                    var tick = new ReplayTick
                    {
                        pV = player.velocity,
                        pP = player.transform.position,
                        py = player.Yaw,
                        pp = player.Pitch
                    };
                    currentReplay.ticks[input.tickCount] = tick;
                }
            }
        }
    }
}