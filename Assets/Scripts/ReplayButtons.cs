using FullSerializer;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ReplayButtons : MonoBehaviour
{
    public GameObject buttonPrefab;

    public Transform startPosition;

    private GameObject[] generatedButtons;

    private void Start()
    {
        var replaysFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Lightstrafe", "replays");
        Directory.CreateDirectory(replaysFolder);
        var files = Directory.GetFiles(replaysFolder);

        generatedButtons = new GameObject[files.Length];
        for (var i = 0; i < files.Length; i++)
        {
            var file = files[i];
            generatedButtons[i] = Instantiate(buttonPrefab, startPosition);

            generatedButtons[i].GetComponent<RectTransform>().localPosition = Vector3.down * 55 * i;

            var button = generatedButtons[i].GetComponent<Button>();
            var text = generatedButtons[i].GetComponentInChildren<Text>();
            text.fontSize = 18;

            text.text = Path.GetFileNameWithoutExtension(file);

            void Action()
            {
                var json = File.ReadAllText(file);
                fsData data = fsJsonParser.Parse(json);
                var fs = new fsSerializer();
                Game.Replay replay = null;
                fs.TryDeserialize<Game.Replay>(data, ref replay);
                Game.PlayReplay(replay);
            }

            button.onClick.AddListener(Action);
        }
    }
}