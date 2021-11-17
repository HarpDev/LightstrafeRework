using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Radial : MonoBehaviour
{

    public float size;
    public float position;

    private const float RADIUS = 28;
    private const float MIN_SIZE = 0.25f;

    public Image Image { get; set; }

    private float yScale;

    private void Awake()
    {
        Image = GetComponent<Image>();
    }

    private void Start()
    {
        transform.localPosition = new Vector3(-Mathf.Sin(position * Mathf.Deg2Rad), Mathf.Cos(position * Mathf.Deg2Rad), 0) * RADIUS;
        transform.localRotation = Quaternion.Euler(0, 0, position);
    }

    private void Update()
    {
        size = Mathf.Lerp(size, MIN_SIZE, Time.deltaTime);
        yScale = Mathf.Lerp(yScale, size, Time.deltaTime * 20);

        transform.localScale = new Vector3(1, yScale, 1);

        if (yScale >= size || size <= MIN_SIZE + 0.05f)
        {
            if (Image.color.a > 0) Image.color -= new Color(0, 0, 0, Time.deltaTime);
            else Destroy(gameObject);
        }
    }
}
