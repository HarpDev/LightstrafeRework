using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuFlow : MonoBehaviour
{

    public Canvas MainMenu;
    public Canvas Chapter1Select;
    public Canvas Options;

    private void Start()
    {
        Chapter1Select.gameObject.SetActive(false);
        Options.gameObject.SetActive(false);
        MainMenu.gameObject.SetActive(true);
    }

    public void GotoChapter1Select()
    {
        MainMenu.gameObject.SetActive(false);
        Options.gameObject.SetActive(false);
        Chapter1Select.gameObject.SetActive(true);
    }

    public void GotoOptions()
    {
        MainMenu.gameObject.SetActive(false);
        Options.gameObject.SetActive(true);
        Chapter1Select.gameObject.SetActive(false);
    }
}
