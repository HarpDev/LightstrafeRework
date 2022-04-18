using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Breakable : MonoBehaviour, MapInteractable
{

    public void Proc(RaycastHit hit)
    {
        gameObject.SetActive(false);
    }
}
