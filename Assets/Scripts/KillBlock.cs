using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillBlock : MonoBehaviour
{
    public Transform to;
    
    public bool IsHit { get; private set; }

    private Vector3 _toPosition;
    private Quaternion _toRotation;

    private void Update()
    {
        if (!IsHit) return;
        var t = transform;
        t.position = Vector3.Lerp(t.position, _toPosition, Time.deltaTime * 4);
        t.rotation = Quaternion.Lerp(t.rotation, _toRotation, Time.deltaTime * 4);
    }

    public void Hit()
    {
        if (IsHit) return;
        IsHit = true;
        _toPosition = to.position;
        _toRotation = to.rotation;
        Game.I.Hitmarker.Display();
    }
}
