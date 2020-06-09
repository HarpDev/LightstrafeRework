using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrowHandler : MonoBehaviour
{

    public Rings rings;
    public Transform sphereTransform;
    public Camera viewModel;
    public bool flip;

    public void Throw(float angle)
    {
        rings.Throw(angle, sphereTransform, viewModel, flip);
    }


}
