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
        slider.value = music ? Game.MusicVolume : Game.SoundVolume;
    }

    public void VolumeChanged()
    {
        if (music)
            Game.MusicVolume = slider.value;
        else
            Game.SoundVolume = slider.value;
    }
}