using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAudioManager : MonoBehaviour
{
    private Dictionary<string, GameObject> playingAudio = new Dictionary<string, GameObject>();

    public void PlayAudio(AudioClip clip, float volume = 1f, float pitch = 1f, bool looping = false)
    {
        if (playingAudio.ContainsKey(clip.name)) return;
        var obj = new GameObject("Audio-" + clip.name);
        obj.transform.parent = gameObject.transform;
        obj.transform.localPosition = Vector3.zero;

        var audio = obj.AddComponent<AudioSource>();
        audio.clip = clip;
        audio.volume = volume;
        audio.pitch = pitch;
        audio.loop = looping;
        audio.Play();

        playingAudio[clip.name] = obj;
    }

    public void StopAudio(AudioClip clip)
    {
        if (playingAudio.ContainsKey(clip.name))
        {
            var obj = playingAudio[clip.name];
            playingAudio.Remove(clip.name);
            Destroy(obj);
        }
    }

    private void FixedUpdate()
    {
        var e = playingAudio.GetEnumerator();
        var toRemove = new List<string>();
        while (e.MoveNext())
        {
            if (!e.Current.Value.GetComponent<AudioSource>().isPlaying)
            {
                toRemove.Add(e.Current.Key);
                Destroy(e.Current.Value);
            }
        }
        foreach (var remove in toRemove)
        {
            playingAudio.Remove(remove);
        }
    }
}
