using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CanvasContainer : MonoBehaviour
{

    public Hitmarker hitmarker;
    public Image crosshair;
    public SpeedChangeDisplay speedChangeDisplay;
    public GameObject textNotificationPrefab;
    public Text kickFeedback;

    public void SendNotification(string text, float duration = 5f, int fontSize = 40)
    {
        var notif = Instantiate(textNotificationPrefab, transform).GetComponent<TextNotification>();
        fontSize *= Screen.width / 1600;
        notif.SetText(text, duration, fontSize);
    }

}
