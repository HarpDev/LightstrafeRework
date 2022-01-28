using UnityEngine;

public class AnimatedTexture : MonoBehaviour
{

    public Texture2D[] frames;
    public float fps = 2f;

    private Material mat;
    public int materialIndex;
    
    private void Start()
    {
        mat = GetComponent<Renderer>().materials[materialIndex];
    }

    private void Update()
    {
        var index = (int) (Time.time * fps);
        index %= frames.Length;
        mat.mainTexture = frames[index];
    }
}
