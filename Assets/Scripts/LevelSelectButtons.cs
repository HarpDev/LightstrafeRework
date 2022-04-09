using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelectButtons : MonoBehaviour
{
    public GameObject buttonPrefab;

    public Transform startPosition;

    private GameObject[] generatedButtons;

    public List<string> levels;

    private void Start()
    {
        generatedButtons = new GameObject[levels.Count];

        for (var i = 0; i < levels.Count; i++)
        {
            generatedButtons[i] = Instantiate(buttonPrefab, startPosition);

            generatedButtons[i].GetComponent<RectTransform>().localPosition = Vector3.down * 55 * i;
            
            var button = generatedButtons[i].GetComponent<Button>();
            var text = generatedButtons[i].GetComponentInChildren<Text>();

            var level = levels[i];
            text.text = Path.GetFileNameWithoutExtension(level);

            void Action()
            {
                Game.StartLevel(level);
            }

            button.onClick.AddListener(Action);
        }
    }
}