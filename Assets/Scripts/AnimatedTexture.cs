using UnityEngine;

public class AnimatedTexture : MonoBehaviour
{

    public Texture2D[] frames;
    public float fps = 2f;

    private Material mat;
    
    private void Start()
    {
        mat = GetComponent<Renderer>().material;
    }

    private void Update()
    {
        var index = (int) (Time.time * fps);
        index %= frames.Length;
        mat.mainTexture = frames[index];
    }
}
