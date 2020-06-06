using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class Platform : MonoBehaviour
{

    public MeshRenderer glow;
    public Light lightSource;

    private bool _glowing = false;

    private Collider _collider;

    private void Start()
    {
        Game.Player.ContactEvent += new PlayerMovement.PlayerContact(PlayerContact);
        Gun.ShotEvent += new Gun.GunShot(Shot);
        Projectile.ProjectileHitEvent += new Projectile.ProjectileHit(Shot);

        _collider = GetComponent<Collider>();
    }

    private Color _color;
    private float _range;
    private float _value;

    private void Update()
    {
        //_glowing = Vector3.Distance(Game.Player.transform.position, transform.position) < 50;

        var brightness = 1.1f;

        if (_glowing)
        {
            _range = Mathf.Lerp(_range, 35, Time.deltaTime);
            if (_value < brightness) _value += Time.deltaTime;
            if (_value > brightness) _value = brightness;
        }
        else
        {
            _range = Mathf.Lerp(_range, 0, Time.deltaTime);
            if (_value > 0) _value -= Time.deltaTime;
            if (_value < 0) { 
                _value = 0;
                _collider.enabled = false;
            }
        }

        Color.RGBToHSV(Color.cyan * 2, out var h, out var s, out _);
        _color = Color.HSVToRGB(h, s, Mathf.Pow(_value, 2));
        lightSource.range = _range;
        glow.material.SetColor("_EmissionColor", _color);
        glow.material.EnableKeyword("_EMISSION");
    }

    private void Shot(RaycastHit hit)
    {
        if (!_glowing && hit.collider.gameObject == gameObject)
        {
            _glowing = true;
            Game.Canvas.hitmarker.Display();
        }
    }

    private void PlayerContact(Vector3 normal, Collider collider)
    {
        if (_glowing && collider.gameObject == gameObject)
        {
            _glowing = false;
        }
    }
}
