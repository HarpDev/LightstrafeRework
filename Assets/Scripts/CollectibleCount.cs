using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CollectibleCount : MonoBehaviour
{
    private Collectible[] collectibles;
    private Text text;
    private void Start()
    {
        collectibles = FindObjectsOfType<Collectible>();
        text = GetComponent<Text>();
    }
    private void Update()
    {
        int left = collectibles.Length + 1;
        foreach (Collectible collectible in collectibles)
        {
            if (collectible != null && collectible.LeftToCollect < left)
            {
                left = collectible.LeftToCollect;
                break;
            }
        }
        if (left == collectibles.Length + 1) left = 0;
        text.text = (collectibles.Length - left) + "/" + collectibles.Length;
        
    }
}
