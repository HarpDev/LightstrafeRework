using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceLimit : MonoBehaviour
{

    private Collider _collider;
    private MeshRenderer[] _renderers;

    private void Start()
    {
        _collider = GetComponent<Collider>();
        _renderers = GetComponentsInChildren<MeshRenderer>();
    }

    private void Update()
    {
        if (Vector3.Distance(transform.position, Game.Player.transform.position) > 1000)
        {
            foreach (var mesh in _renderers)
            {
                mesh.enabled = false;
            }
            _collider.enabled = false;
        } else
        {
            foreach (var mesh in _renderers)
            {
                mesh.enabled = true;
            }
            _collider.enabled = true;
        }
    }
}
