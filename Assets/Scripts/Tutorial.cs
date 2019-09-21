using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Tutorial : MonoBehaviour
{

    private int _stage = -1;
    private Notification _notification;

    public Image rightStrafeKey;
    public Image leftStrafeKey;

    private void Update()
    {
        // velocity 0, 20, 30
        if (_stage == -1)
        {
            _stage++;
            Game.I.Player.LookScale = 0;
            Time.timeScale = 0;
            _notification = Instantiate(Game.I.notification.gameObject, Game.I.Canvas.transform).GetComponent<Notification>();
            _notification.text.text = "Press and hold 'D'";
            _notification.text.fontSize = 30;
        }

        if (_stage == 0)
        {
            if (Input.GetAxisRaw("Right") > 0)
            {
                Game.I.Player.velocity = new Vector3(0, 20, 30);
                Game.I.Player.LookScale = 1;
                Time.timeScale = 1;
                _stage++;
            }
        }
    }
}
