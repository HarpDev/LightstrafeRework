using UnityEngine;
using UnityEngine.UI;

public class Hitmarker : MonoBehaviour
{
    public Sprite hitmarker;
    public Sprite critmarker;

    private Image image;
    private Image Image
    {
        get
        {
            if (image == null) image = GetComponent<Image>();
            return image;
        }
    }

    public static void Display(bool crit)
    {
        var hit = Instantiate(Game.I.hitmarker, Game.I.Canvas.transform);
        if (crit) hit.Image.sprite = hit.critmarker;
        else hit.Image.sprite = hit.hitmarker;
    }

    private void Start()
    {
        var color = Image.color;
        color.a = 1f;

        Image.color = color;
    }

    private void Update()
    {
        var color = Image.color;
        if (color.a > 0) color.a -= Time.deltaTime;
        else Destroy(gameObject);

        Image.color = color;
    }
}