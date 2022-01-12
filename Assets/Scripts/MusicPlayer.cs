using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    public AudioClip musicLoop;

    private AudioSource audio;

    private static MusicPlayer I;

    public void PlayMusic()
    {
        if (audio.isPlaying) audio.Stop();

        audio.clip = musicLoop;
        audio.volume = Game.MusicVolume;
        audio.pitch = 1;
        audio.loop = true;
        audio.Play();
    }

    private void Awake()
    {
        if (I == null)
        {
            DontDestroyOnLoad(gameObject);
            I = this;
            audio = GetComponent<AudioSource>();
            PlayMusic();
        }
        else if (I != this)
        {
            var newMusic = GetComponent<MusicPlayer>();
            if (I.musicLoop.name != newMusic.musicLoop.name)
            {
                I.musicLoop = newMusic.musicLoop;
                I.PlayMusic();
            }

            Destroy(gameObject);
        }
    }


    private void Update()
    {
        if (!audio.isPlaying)
        {
            audio.clip = musicLoop;
            audio.loop = true;
            audio.Play();
        }

        audio.volume = Game.MusicVolume;

        if (Game.Player != null) transform.position = Game.Player.camera.transform.position;
    }
}