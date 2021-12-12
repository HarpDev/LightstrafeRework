using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAudioManager : MonoBehaviour
{
    private Dictionary<string, PlayingAudio> playingAudio = new Dictionary<string, PlayingAudio>();

    public struct PlayingAudio
    {
        public PlayingAudio(GameObject obj, float volume = 1)
        {
            this.obj = obj;
            this.volume = volume;
        }

        public GameObject obj;
        public float volume;
    }

    int i = 0;

    public void PlayOneShot(AudioClip clip, bool looping = false, float volume = 1)
    {
        var name = clip.name + i++;
        if (i > 500000) i = 0;
        if (playingAudio.ContainsKey(name)) return;
        var obj = new GameObject("Audio-" + name);
        obj.transform.parent = gameObject.transform;
        obj.transform.localPosition = Vector3.zero;

        var audio = obj.AddComponent<AudioSource>();
        audio.clip = clip;
        audio.volume = Game.SoundVolume;
        audio.loop = looping;
        audio.Play();

        var playing = new PlayingAudio(obj, volume);

        playingAudio[name] = playing;
    }

    public void PlayAudio(AudioClip clip, bool looping = false, float volume = 1)
    {
        if (IsPlaying(clip))
        {
            StopAudio(clip);
        }

        var obj = new GameObject("Audio-" + clip.name);
        obj.transform.parent = gameObject.transform;
        obj.transform.localPosition = Vector3.zero;

        var audio = obj.AddComponent<AudioSource>();
        audio.clip = clip;
        audio.volume = Game.SoundVolume * volume;
        audio.pitch = Time.timeScale;
        audio.loop = looping;
        audio.Play();

        var playing = new PlayingAudio(obj, volume);

        playingAudio[clip.name] = playing;
    }

    private GameObject music;
    private AudioClip musicLoop;

    public void PlayMusic(AudioClip clip, AudioClip loop)
    {
        if (music != null)
        {
            Destroy(music);
        }

        music = new GameObject("Music-" + clip.name);
        music.transform.parent = gameObject.transform;
        music.transform.localPosition = Vector3.zero;

        musicLoop = loop;

        var audio = music.AddComponent<AudioSource>();
        audio.clip = clip;
        audio.volume = Game.MusicVolume;
        audio.pitch = Time.timeScale;
        audio.loop = false;
        audio.Play();

        var playing = new PlayingAudio(music, Game.MusicVolume);

        playingAudio[clip.name] = playing;
    }

    public void StopAudio(AudioClip clip)
    {
        if (!IsPlaying(clip)) return;

        var playing = playingAudio[clip.name];
        playingAudio.Remove(clip.name);
        Destroy(playing.obj);
    }

    public bool IsPlaying(AudioClip clip)
    {
        return playingAudio.ContainsKey(clip.name);
    }

    public void SetVolume(AudioClip clip, float volume)
    {
        if (!IsPlaying(clip)) return;
        var playing = playingAudio[clip.name];
        playing.volume = volume;
        playingAudio[clip.name] = playing;
    }

    private void Update()
    {
        if (music != null)
        {
            var m = music.GetComponent<AudioSource>();
            if (!m.isPlaying)
            {
                m.clip = musicLoop;
                m.loop = true;
                m.Play();
            }
        }

        foreach (KeyValuePair<string, PlayingAudio> e in playingAudio)
        {
            var playing = e.Value;
            var source = playing.obj.GetComponent<AudioSource>();
            source.pitch = Time.timeScale;
            if (e.Value.obj == music)
            {
                playing.volume = Game.MusicVolume;
            }
            source.volume = Game.SoundVolume * playing.volume;
        }
    }

    private void FixedUpdate()
    {
        var e = playingAudio.GetEnumerator();
        var toRemove = new List<string>();
        while (e.MoveNext())
        {
            if (e.Current.Value.obj == music) continue;
            if (!e.Current.Value.obj.GetComponent<AudioSource>().isPlaying)
            {
                toRemove.Add(e.Current.Key);
                Destroy(e.Current.Value.obj);
            }
        }

        foreach (var remove in toRemove)
        {
            playingAudio.Remove(remove);
        }
    }
}