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

    public Canvas baseCanvas;

    private List<Canvas> UiTree { get; set; }
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
        if (UiTree.Count > 0)
        {
            var obj = UiTree[^1];
            UiTree.RemoveAt(UiTree.Count - 1);
            Destroy(obj.gameObject);
            if (UiTree.Count > 0)
            {
                UiTree[^1].gameObject.SetActive(true);
            }
            else
            {
                baseCanvas.gameObject.SetActive(true);
            }
        }
    }

    public void OpenMenu(Canvas canvas)
    {
        foreach (var c in UiTree)
        {
            c.gameObject.SetActive(false);
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        baseCanvas.gameObject.SetActive(false);
        UiTree.Add(Instantiate(canvas));
    }

}
