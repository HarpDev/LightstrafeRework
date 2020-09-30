using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class Ocean : MonoBehaviour
{

    private MeshRenderer _meshRenderer;
    private float _y;
    private const float distance = 25;
    private float _rise;

    private void Start()
    {
        gameObject.SetActive(true);
        _meshRenderer = GetComponent<MeshRenderer>();
        _y = Game.Player.camera.transform.position.y - distance;
        //_distance = Mathf.Abs(Game.Player.transform.position.y - transform.position.y);
    }

    private void Update()
    {

        var position = transform.position;
        position.x = Game.Player.camera.transform.position.x;
        position.z = Game.Player.camera.transform.position.z;
        position.y = Mathf.Lerp(position.y, _y + _rise, Time.deltaTime);
        transform.position = position;

        if (Application.isPlaying)
        {
            if (_y < Game.Player.camera.transform.position.y - distance)
            {
                _y = Game.Player.camera.transform.position.y - distance;
                _rise = 0;
            }
            //_rise += Time.deltaTime;
            _meshRenderer.material.SetTextureOffset("_MainTex", new Vector2(position.x / transform.localScale.x, position.z / transform.localScale.z));
            //_meshRenderer.material.SetTextureOffset("_SurfaceDistortion", new Vector2(position.x / transform.localScale.x, position.z / transform.localScale.z));
        } else
        {
            _y = Game.Player.camera.transform.position.y - distance;
        }
    }
}
