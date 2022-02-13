
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Sensitivity : MonoBehaviour
{

    public InputField sensitivity;

    private void Start()
    {
        sensitivity.text = GameSettings.Sensitivity + "";
    }

    private void Update()
    {
        sensitivity.text = sensitivity.text.Where(c => char.IsDigit(c) || c == '.').Aggregate("", (current, c) => current + c);
        
        float result;
        if (float.TryParse(sensitivity.text, out result))
            GameSettings.Sensitivity = result;
    }

    private string preventEscape = "";

    public void OnEditting()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
        {
            preventEscape = sensitivity.text;
        }
    }

    public void OnEditEnd()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            sensitivity.text = preventEscape;
        }
        EventSystem.current.SetSelectedGameObject(null);
    }
}
