using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

public class ReplayButtons : MonoBehaviour
{
    public GameObject buttonPrefab;

    public Transform startPosition;

    private GameObject[] generatedButtons;

    private void Start()
    {
        var replaysFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Lightstrafe\replays";
        var files = Directory.GetFiles(replaysFolder);

        generatedButtons = new GameObject[files.Length];
        for (var i = 0; i < files.Length; i++)
        {
            var file = files[i];

            var json = File.ReadAllText(file);
            var replay = JsonConvert.DeserializeObject<Game.Replay>(json);
            generatedButtons[i] = Instantiate(buttonPrefab, (i == 0 ? startPosition : generatedButtons[i - 1].transform));

            generatedButtons[i].GetComponent<RectTransform>().localPosition = Vector3.down * 55;
            
            var button = generatedButtons[i].GetComponent<Button>();
            var text = generatedButtons[i].GetComponentInChildren<Text>();
            text.text = Path.GetFileNameWithoutExtension(file);

            void Action()
            {
                Game.PlayReplay(replay);
            }

            button.onClick.AddListener(Action);
        }
    }
}