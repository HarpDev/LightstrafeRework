using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    public AudioClip musicLoop;
    public float bpm = 140f;

    public AudioSource Audio { get; set; }

    private static MusicPlayer I;
    private Player player;

    private void Awake()
    {
        if (I == null) Game.OnAwakeBind(this);
    }

    private void Start()
    {
        if (I == null)
        {
            DontDestroyOnLoad(gameObject);
            I = this;
            Audio = GetComponent<AudioSource>();
            PlayMusic();
        }
        else if (I != this)
        {
            I.Start();
            Destroy(gameObject);
            return;
        }

        player = Game.OnStartResolve<Player>();
        var newMusic = GetComponent<MusicPlayer>();
        if (I.musicLoop.name != newMusic.musicLoop.name)
        {
            I.musicLoop = newMusic.musicLoop;
            I.PlayMusic();
        }
    }

    public void PlayMusic()
    {
        if (Audio.isPlaying) Audio.Stop();

        Audio.clip = musicLoop;
        Audio.volume = GameSettings.MusicVolume * GameSettings.SoundVolume;
        Audio.pitch = 1;
        Audio.loop = true;
        Audio.Play();
    }


    private void Update()
    {
        if (!Audio.isPlaying)
        {
            Audio.clip = musicLoop;
            Audio.loop = true;
            Audio.Play();
        }

        Audio.volume = GameSettings.MusicVolume * GameSettings.SoundVolume;

        if (player != null) transform.position = player.camera.transform.position;
    }
}