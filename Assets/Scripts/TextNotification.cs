using System;
using UnityEngine;
using UnityEngine.UI;

public class TextNotification : MonoBehaviour
{

    public Text text;
    public Image bg;

    public float xMargin = 10;
    public float yMargin = 10;
    public float fadeInMultiplier = 3;
    public float fadeOutMultiplier = 1;

    private float timeUntilFade;

    public void SetText(string t, float duration = 5, int fontSize = 40)
    {
        timeUntilFade = duration;
        text.text = t;
        text.fontSize = fontSize;
    }

    private void Start()
    {
        var textcolor = text.color;
        textcolor.a = 0;
        text.color = textcolor;

        var bgcolor = bg.color;
        bgcolor.a = 0;
        bg.color = bgcolor;
    }

    private void Update()
    {
        var rectSize = bg.rectTransform.sizeDelta;

        rectSize.x = text.preferredWidth + xMargin * 2;
        rectSize.y = text.preferredHeight + yMargin * 2;
        
        bg.rectTransform.sizeDelta = rectSize;
        text.rectTransform.sizeDelta = rectSize;
        
        if (timeUntilFade < 0)
        {
            var textcolor = text.color;
            textcolor.a -= Mathf.Min(Time.deltaTime * fadeOutMultiplier, textcolor.a);
            text.color = textcolor;

            var bgcolor = bg.color;
            bgcolor.a -= Mathf.Min(Time.deltaTime * fadeOutMultiplier, bgcolor.a);
            bg.color = bgcolor;

            if (textcolor.a <= 0 && bgcolor.a <= 0)
            {
                Destroy(this);
            }
        }
        else
        {
            var textcolor = text.color;
            textcolor.a += Mathf.Min(Time.deltaTime * fadeInMultiplier, 1 - textcolor.a);
            text.color = textcolor;

            var bgcolor = bg.color;
            bgcolor.a += Mathf.Min(Time.deltaTime * fadeInMultiplier, 1 -bgcolor.a);
            bg.color = bgcolor;
            
            timeUntilFade -= Time.deltaTime;
        }
    }
}
