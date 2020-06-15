using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class Ocean : MonoBehaviour
{

    private MeshRenderer _meshRenderer;
    private float _y;
    private const float distance = 20;
    private float _rise;

    private void Start()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        //_distance = Mathf.Abs(Game.Player.transform.position.y - transform.position.y);
    }

    private void Update()
    {

        var position = transform.position;
        position.x = Game.Player.camera.transform.position.x;
        position.z = Game.Player.camera.transform.position.z;
        position.y = _y;
        transform.position = position;

        if (Application.isPlaying)
        {
            var target = Game.Player.camera.transform.position.y - (distance - _rise);
            if (_y < target)
            {
                _y = Mathf.Lerp(_y, target, Time.deltaTime);
            }
            else
            {
                _rise = 0;
            }
            _rise += Time.deltaTime;
            _meshRenderer.material.SetTextureOffset("_MainTex", new Vector2(position.x / transform.localScale.x, position.z / transform.localScale.z));
        }
    }
}
