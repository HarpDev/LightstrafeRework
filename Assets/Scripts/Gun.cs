using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class Gun : MonoBehaviour
{

    public PlayerMovement player;

    private Vector3 position = new Vector3(0.2f, -0.35f, 1.7f);

    public Slider slider;

    public Transform barrel;

    private float _shotsInClip;
    private const int clipSize = 0;

    private Vector3 _angleEulers;
    private const float lerpSpeed = 50;

    private float _fireKick = 0;

    private const float crouchPositionSpeed = 10;
    private const float reloadFactor = 0.4f;

    private const float ammoSpacing = 5;

    private float _crouchPositionAmt;

    private float _forwardChange;
    private float _rightChange;
    private float _upChange;

    private float _prevPitch;

    private float _forwardSoften;
    private float _rightSoften;

    public Projectile projectile;

    public AudioClip fireSound;

    private List<GameObject> _sliders;

    private Animator _animator;

    private bool _canFire;

    private void Start()
    {
        _animator = GetComponent<Animator>();
        _shotsInClip = clipSize;
        _canFire = true;

        if (Application.IsPlaying(gameObject))
        {
            _sliders = new List<GameObject>();
            for (var i = 0; i < clipSize; i++)
            {
                var s = Instantiate(slider.gameObject, Game.Canvas.transform).GetComponent<Slider>();
                _sliders.Add(s.gameObject);

                var rect = (RectTransform)s.transform;
                var spacing = rect.rect.width + ammoSpacing;
                var index = (i + 1) - (clipSize / 2f + 0.5f);

                s.transform.position += Vector3.right * index * spacing;
            }
        }
    }

    private void FixedUpdate()
    {
        _currentInfo = _animator.GetCurrentAnimatorStateInfo(0);

        if (_currentInfo.IsName("Reload"))
        {
            _animator.SetBool("Fire", false);
            _canFire = true;
        }
    }

    private AnimatorStateInfo _currentInfo;

    private void Update()
    {
        var pos = position;

        var angle = 0f;

        if (player.IsSliding && !player.approachingWall && !player.IsOnWall)
        {
            if (_crouchPositionAmt < 1) _crouchPositionAmt += Time.deltaTime * crouchPositionSpeed;
        }
        else if (_crouchPositionAmt > 0) _crouchPositionAmt -= Time.deltaTime * crouchPositionSpeed;

        angle += 30 * _crouchPositionAmt;
        pos += new Vector3(-0.1f, 0.02f, 0) * _crouchPositionAmt;

        var yawMovement = player.YawIncrease;
        var pitchMovement = _prevPitch - player.Pitch;

        _upChange -= player.CollisionImpulse.y * Time.deltaTime;

        _rightChange -= yawMovement / 400;
        _upChange -= pitchMovement / 400;

        _forwardChange = Mathf.Lerp(_forwardChange, 0, Time.deltaTime * 8);
        _rightChange = Mathf.Lerp(_rightChange, 0, Time.deltaTime * 7);
        _upChange = Mathf.Lerp(_upChange, 0, Time.deltaTime * 7);

        _forwardSoften = Mathf.Lerp(_forwardSoften, _forwardChange, Time.deltaTime * 20);
        _rightSoften = Mathf.Lerp(_rightSoften, _rightChange, Time.deltaTime * 20);

        pos.z += _forwardSoften;
        pos.x += _rightSoften;
        pos.y += _upChange;

        pos.y += _fireKick / 60;
        pos.z -= _fireKick / 30;

        if (_fireKick > 0) _fireKick -= Time.deltaTime * 30; else _fireKick = 0;

        _angleEulers = Vector3.Lerp(_angleEulers, new Vector3(angle, -90, 0), Time.deltaTime * lerpSpeed);
        transform.localRotation = Quaternion.Euler(_angleEulers);
        transform.Rotate(new Vector3(-_fireKick, 0, 0), Space.Self);

        transform.localPosition = Vector3.Lerp(transform.localPosition, pos, Time.deltaTime * lerpSpeed);

        _prevPitch = player.Pitch;

        if (_shotsInClip < clipSize)
        {
            _shotsInClip += Time.deltaTime * reloadFactor;
        }
        else
        {
            _shotsInClip = clipSize;
        }

        if (Input.GetKey(PlayerInput.PrimaryInteract) && Time.timeScale > 0 && _currentInfo.IsName("Loaded") && _canFire)
        {
            var proj = Instantiate(projectile.gameObject).GetComponent<Projectile>();

            var projection = Vector3.Dot(player.velocity, player.CrosshairDirection);
            if (projection < 1) projection = 1;
            proj.Fire(player.CrosshairDirection * projection, player.camera.transform.position, barrel.position);

            player.source.PlayOneShot(fireSound);

            _fireKick = 3;

            _animator.SetBool("Fire", true);
            _canFire = false;

            HudMovement.RotationSlamVector -= Vector3.up * 10;
        }


        if (Application.IsPlaying(gameObject))
        {
            for (var i = 0; i < clipSize; i++)
            {
                var s = _sliders[i].GetComponent<Slider>();

                var fill = Mathf.Max(Mathf.Min(_shotsInClip - i, 1), 0);

                s.value = Mathf.Lerp(s.minValue, s.maxValue, fill);
            }
        }
    }

    public void ReplenishClip()
    {
        _shotsInClip = clipSize;
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}
