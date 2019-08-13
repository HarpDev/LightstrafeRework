using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelect : MonoBehaviour
{
    private void Awake()
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            Debug.Log(i);
            var scene = SceneManager.GetSceneByBuildIndex(i);
            Debug.Log(scene.name);
            
            foreach (var obj in scene.GetRootGameObjects())
            {
                Debug.Log(scene.name + " - " + obj.name);
            }
        } 
    }
}