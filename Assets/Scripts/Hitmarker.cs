
using UnityEngine;
using UnityEngine.UI;

public class Hitmarker : MonoBehaviour
{

    public Image hitmarker;
    private void Start()
    {
        var color = hitmarker.color;
        color.a = 1f;

        hitmarker.color = color;
    }

    private void Update()
    {
        var color = hitmarker.color;
        if (color.a > 0) color.a -= Time.deltaTime;
        else Destroy(gameObject);

        hitmarker.color = color;
    }
}
