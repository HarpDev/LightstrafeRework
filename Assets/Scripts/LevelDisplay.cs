using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelDisplay : MonoBehaviour
{
    public Text levelText;
    public string prefix;

    private void Update()
    {
        levelText.text = prefix + SceneManager.GetActiveScene().name;
    }
}