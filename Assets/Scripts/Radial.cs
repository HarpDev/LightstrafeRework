using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Radial : MonoBehaviour
{

    public float size;
    public float position;

    private const float radius = 28;
    private const float minSize = 0.25f;

    public Image Image { get; set; }

    private float _yScale;

    private void Awake()
    {
        Image = GetComponent<Image>();
    }

    private void Start()
    {
        transform.localPosition = new Vector3(-Mathf.Sin(position * Mathf.Deg2Rad), Mathf.Cos(position * Mathf.Deg2Rad), 0) * radius;
        transform.localRotation = Quaternion.Euler(0, 0, position);
    }

    private void Update()
    {
        size = Mathf.Lerp(size, minSize, Time.deltaTime);
        _yScale = Mathf.Lerp(_yScale, size, Time.deltaTime * 20);

        transform.localScale = new Vector3(1, _yScale, 1);

        if (_yScale >= size || size <= minSize + 0.05f)
        {
            if (Image.color.a > 0) Image.color -= new Color(0, 0, 0, Time.deltaTime);
            else Destroy(gameObject);
        }
    }
}
