using UnityEngine;

public class AnimatedTexture : MonoBehaviour
{

    public Texture2D[] frames;

    private Material mat;
    public int materialIndex;

    private MusicPlayer music;
    
    private void Start()
    {
        music = Game.OnStartResolve<MusicPlayer>();
        mat = GetComponent<Renderer>().materials[materialIndex];
    }

    private int lastIndex;
    private void Update()
    {
        if (music == null) return;
        var time = music.Audio.timeSamples / (float)music.musicLoop.frequency;
        var fps = music.bpm / 60;
        var index = Mathf.RoundToInt(time * fps);
        index %= frames.Length;

        if (index != lastIndex && Time.timeScale > 0)
        {
            mat.mainTexture = frames[index];
        }
        lastIndex = index;
    }
}
