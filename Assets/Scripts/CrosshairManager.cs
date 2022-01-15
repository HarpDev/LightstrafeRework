using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CrosshairManager : MonoBehaviour
{
    public Image crosshair;
    
    private void Awake()
    {
        Game.OnAwakeBind(this);
    }

    private float crosshairColor;

    private void Update()
    {
        if (IsActivated)
        {
            crosshair.transform.rotation = Quaternion.Euler(0, 0,
                Mathf.Lerp(crosshair.transform.rotation.eulerAngles.z, 45, Time.deltaTime * 20));
            crosshairColor = Mathf.Lerp(crosshairColor, 1, Time.deltaTime * 20);
        }
        else
        {
            crosshair.transform.rotation = Quaternion.Euler(0, 0,
                Mathf.Lerp(crosshair.transform.rotation.eulerAngles.z, 0, Time.deltaTime * 20));
            crosshairColor = Mathf.Lerp(crosshairColor, 100f / 255f, Time.deltaTime * 20);
        }

        crosshair.color = new Color(crosshairColor, crosshairColor, crosshairColor, crosshairColor);
    }
    
    public bool IsActivated { get; set; }
}
