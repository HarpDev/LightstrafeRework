using UnityEngine;

public class GameSettings
{

    public static bool UseTimingDisplay
    {
        get => PlayerPrefs.GetInt("UseTimingDisplay", 0) != 0;
        set => PlayerPrefs.SetInt("UseTimingDisplay", value ? 1 : 0);
    }

    public static bool FullGameTimer
    {
        get => PlayerPrefs.GetInt("FullGameTimer", 0) != 0;
        set => PlayerPrefs.SetInt("FullGameTimer", value ? 1 : 0);
    }

    public static float SoundVolume
    {
        get => PlayerPrefs.GetFloat("SoundVolume", 1);
        set => PlayerPrefs.SetFloat("SoundVolume", value);
    }

    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat("MusicVolume", 1);
        set => PlayerPrefs.SetFloat("MusicVolume", value);
    }

    public static float Sensitivity
    {
        get
        {
            if (!PlayerPrefs.HasKey("Sensitivity")) PlayerPrefs.SetFloat("Sensitivity", 1);
            return PlayerPrefs.GetFloat("Sensitivity");
        }
        set => PlayerPrefs.SetFloat("Sensitivity", value);
    }

}