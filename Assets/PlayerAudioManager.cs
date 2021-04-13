using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAudioManager : MonoBehaviour
{

    public void PlayAudio(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        var obj = new GameObject("Audio-" + clip.name);
        obj.transform.parent = gameObject.transform;
        obj.transform.localPosition = Vector3.zero;

        var audio = obj.AddComponent<AudioSource>();
        audio.clip = clip;
        audio.volume = volume;
        audio.pitch = pitch;
        audio.Play();
    }
}
