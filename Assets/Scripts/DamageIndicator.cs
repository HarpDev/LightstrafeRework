using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DamageIndicator : MonoBehaviour
{
    public Image topLeft;
    public Image topRight;
    public Image bottomLeft;
    public Image bottomRight;

    private void Update()
    {
        if (topLeft.color.a > 0) topLeft.color -= new Color(0, 0, 0, Time.deltaTime);
        if (topRight.color.a > 0) topRight.color -= new Color(0, 0, 0, Time.deltaTime);
        if (bottomLeft.color.a > 0) bottomLeft.color -= new Color(0, 0, 0, Time.deltaTime);
        if (bottomRight.color.a > 0) bottomRight.color -= new Color(0, 0, 0, Time.deltaTime);
    }

    public void Damage(float amt)
    {
        topLeft.transform.localScale = Vector3.one * amt * 0.6f;
        topRight.transform.localScale = Vector3.one * amt * 0.6f;
        bottomLeft.transform.localScale = Vector3.one * amt * 0.6f;
        bottomRight.transform.localScale = Vector3.one * amt * 0.6f;

        topLeft.color += new Color(0, 0, 0, 1 - topLeft.color.a);
        topRight.color += new Color(0, 0, 0, 1 - topRight.color.a);
        bottomLeft.color += new Color(0, 0, 0, 1 - bottomLeft.color.a);
        bottomRight.color += new Color(0, 0, 0, 1 - bottomRight.color.a);
    }
}
