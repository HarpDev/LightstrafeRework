using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class BoolToggleButton : MonoBehaviour
{

    public Text text;
    public string boolName;

    private PropertyInfo property;

    private void Start()
    {
        var properties = typeof(GameSettings).GetProperties(BindingFlags.Public |
                                                    BindingFlags.Static |
                                                    BindingFlags.DeclaredOnly);
        foreach (var prop in properties)
        {
            if (prop.Name == boolName)
            {
                property = prop;
            }
        }
    }

    private void Update()
    {
        var check = (bool)property.GetValue(null);
        text.text = check ? "Yes" : "No";
    }

    public void Toggle()
    {
        property.SetValue(null, !(bool)property.GetValue(null));
    }
}
