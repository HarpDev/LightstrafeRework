using UnityEngine;
using UnityEngine.UI;

public class Hitmarker : MonoBehaviour
{

    private Image image;
    private Image Image
    {
        get
        {
            if (image == null) image = GetComponent<Image>();
            return image;
        }
    }

    public void Display()
    {
        var c = Image.color;
        c.a = 1f;
        Image.color = c;
    }

    private void Awake()
    {
        var color = Image.color;
        color.a = 0f;

        Image.color = color;
    }

    private void Update()
    {
        var color = Image.color;
        if (color.a > 0) color.a -= Time.deltaTime;

        Image.color = color;
    }
}