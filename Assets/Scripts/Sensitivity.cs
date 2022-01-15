
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Sensitivity : MonoBehaviour
{

    public InputField sensitivity;

    private void Start()
    {
        sensitivity.text = GameSettings.Sensitivity + "";
    }

    public void InputUpdated()
    {
        sensitivity.text = sensitivity.text.Where(c => char.IsDigit(c) || c == '.').Aggregate("", (current, c) => current + c);
        
        float result;
        if (float.TryParse(sensitivity.text, out result))
            GameSettings.Sensitivity = result;
    }
}
