using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuAnimation : MonoBehaviour
{
    public Camera MenuCamera;
    public Transform OptionsPosition;
    public Transform LevelSelectPosition;

    public float lerpSpeed = 2;

    public struct PosRot
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    private PosRot startPosition;
    private PosRot nextPosition;
    private PosRot previousPosition;

    private float currentLerpValue;

    private void Start()
    {
        var trans = MenuCamera.transform;
        startPosition = new PosRot
        {
            position = trans.position,
            rotation = trans.rotation
        };
        nextPosition = startPosition;
        previousPosition = startPosition;
    }

    public void SendToOptionsPosition()
    {
        SendToTransform(OptionsPosition);
    }

    public void SendToLevelSelectPosition()
    {
        SendToTransform(LevelSelectPosition);
    }

    public void SendToStartPosition()
    {
        SendToPosRot(startPosition);
    }

    public void SendToTransform(Transform trans)
    {
        SendToPosRot(new PosRot
        {
            position = trans.position,
            rotation = trans.rotation
        });
    }

    public void SendToPosRot(PosRot posRot)
    {
        currentLerpValue = 0;
        var trans = MenuCamera.transform;
        var prev = new PosRot
        {
            position = trans.position,
            rotation = trans.rotation
        };
        previousPosition = prev;
        nextPosition = posRot;
    }

    private void Update()
    {
        var x = currentLerpValue;
        //var ease = x < 0.5 ? 4 * x * x * x : 1 - Mathf.Pow(-2 * x + 2, 3) / 2;
        var ease = 1 - Mathf.Pow(1 - x, 3);
        
        MenuCamera.transform.position =
            Vector3.Lerp(previousPosition.position, nextPosition.position, ease);
        MenuCamera.transform.rotation =
            Quaternion.Lerp(previousPosition.rotation, nextPosition.rotation, ease);
        currentLerpValue += Mathf.Min(1 - currentLerpValue, Time.deltaTime * lerpSpeed);
    }
}