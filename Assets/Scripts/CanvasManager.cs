using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CanvasManager : MonoBehaviour
{

    public GameObject textNotificationPrefab;
    
    public Canvas Chapter1Select;
    public Canvas Options;
    public Canvas Pause;
    public Canvas Replays;

    public Canvas baseCanvas;

    public Canvas screenSizeCanvas;

    public List<Canvas> UiTree { get; set; }
    public int MenuLayerCount => UiTree.Count;

    public Canvas GetCanvasAtLayer(int layer)
    {
        return layer >= MenuLayerCount ? null : UiTree[layer];
    }

    public Canvas GetActiveCanvas()
    {
        return MenuLayerCount > 0 ? UiTree[MenuLayerCount - 1] : baseCanvas;
    }

    private void Awake()
    {
        UiTree = new List<Canvas>();
        Game.OnAwakeBind(this);
    }

    public void SendNotification(string text, float duration = 5f, int fontSize = 40)
    {
        var notif = Instantiate(textNotificationPrefab, baseCanvas.transform).GetComponent<TextNotification>();
        fontSize *= Screen.width / 1600;
        notif.SetText(text, duration, fontSize);
    }

    public void SendNotification(string text, IEnumerable<int> keys, int fontSize = 40)
    {
        var notif = Instantiate(textNotificationPrefab, baseCanvas.transform).GetComponent<TextNotification>();
        fontSize *= Screen.width / 1600;
        notif.SetText(text, keys, 1, fontSize);
    }

    public void OpenMenuAndSetAsBaseCanvas(Canvas canvas)
    {
        var o = baseCanvas.gameObject;
        var parent = o.transform.parent;
        Destroy(o);
        baseCanvas = Instantiate(canvas, parent);
    }

    public void CloseMenu()
    {
        if (menuActionThisFrame) return;
        if (UiTree.Count > 0)
        {
            var obj = UiTree[UiTree.Count - 1];
            UiTree.RemoveAt(UiTree.Count - 1);
            Destroy(obj.gameObject);
            if (UiTree.Count > 0)
            {
                UiTree[UiTree.Count - 1].GetComponent<Canvas>().enabled = true;
            }
            else
            {
                baseCanvas.GetComponent<Canvas>().enabled = true;
            }
            menuActionThisFrame = true;
        }
    }

    public void OpenMenu(Canvas canvas)
    {
        if (menuActionThisFrame) return;
        foreach (var c in UiTree)
        {
            c.gameObject.GetComponent<Canvas>().enabled = false;
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        baseCanvas.GetComponent<Canvas>().enabled = false;
        UiTree.Add(Instantiate(canvas));
        menuActionThisFrame = true;
    }

    private void Update()
    {
        if (Input.GetKeyDown((KeyCode)PlayerInput.Pause))
        {
            CloseMenu();
        }
    }

    private void LateUpdate()
    {
        menuActionThisFrame = false;
    }

    private bool menuActionThisFrame;

}
