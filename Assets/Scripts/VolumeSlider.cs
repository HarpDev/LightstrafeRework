using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VolumeSlider : MonoBehaviour
{
    private Slider slider;

    public bool music;

    private void Start()
    {
        slider = GetComponent<Slider>();
        slider.value = music ? GameSettings.MusicVolume : GameSettings.SoundVolume;
    }

    public void VolumeChanged()
    {
        if (music)
            GameSettings.MusicVolume = slider.value;
        else
            GameSettings.SoundVolume = slider.value;
    }
}