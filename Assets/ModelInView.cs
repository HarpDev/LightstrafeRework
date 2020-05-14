using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelInView : MonoBehaviour
{

    private Quaternion _startRotation;

    private void Start()
    {
        _startRotation = transform.localRotation;
    }

    public void ResetRotation()
    {
        transform.localRotation = _startRotation;
    }

}
