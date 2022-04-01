using UnityEngine;

[SelectionBase]
public class BuildingGenerator : MonoBehaviour
{
    public GameObject top;
    public GameObject[] slicePrefabs;
    public GameObject sliceContainer;

    public bool randomizeRotation = true;

    // Pattern functions as a list of prefab indexes top to bottom
    public string pattern = "000000000000";
}