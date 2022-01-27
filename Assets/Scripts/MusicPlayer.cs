using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    public AudioClip musicLoop;

    private new AudioSource audio;

    private static MusicPlayer I;
    private Player player;

    private void Start()
    {
        player = Game.OnStartResolve<Player>();
    }

    public void PlayMusic()
    {
        if (audio.isPlaying) audio.Stop();

        audio.clip = musicLoop;
        audio.volume = GameSettings.MusicVolume * GameSettings.SoundVolume;
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

        audio.volume = GameSettings.MusicVolume * GameSettings.SoundVolume;

        if (player != null) transform.position = player.camera.transform.position;
    }
}