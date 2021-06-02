using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VolumeSlider : MonoBehaviour
{

    private Slider slider;

    private void Start()
    {
        slider = GetComponent<Slider>();
        slider.value = Game.SoundVolume;
    }

    public void VolumeChanged()
    {
        Game.SoundVolume = slider.value;
    }
}
