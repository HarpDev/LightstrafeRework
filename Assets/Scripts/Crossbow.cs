using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class Crossbow : MonoBehaviour
{

    public PlayerMovement player;

    private Vector3 position = new Vector3(0.2f, -0.35f, 1.7f);

    public Slider slider;

    public Transform barrel;

    private const int clipSize = 0;

    private Vector3 _angleEulers;
    private const float lerpSpeed = 50;

    private float _fireKick = 0;

    private const float crouchPositionSpeed = 10;

    private const float ammoSpacing = 5;

    private float _crouchPositionAmt;

    public static float forwardChange;
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

        if (_currentInfo.IsName("Fire"))
        {
            _animator.SetBool("Fire", false);
            _canFire = true;
        }
    }

    private AnimatorStateInfo _currentInfo;
    public bool UseSideGun
    {
        get
        {
            if (!player.jumpKitEnabled) return false;
            if (!player.ApproachingWall) return false;

            if (player.IsDashing) return true;
            if (Flatten(player.velocity).magnitude > PlayerMovement.BASE_SPEED + 1) return true;
            if (!player.IsOnGround) return true;
            if (player.IsOnRail) return true;
            if (player.GrappleHooked) return true;
            return false;
        }
    }

    private void Update()
    {
        var pos = position;

        var angle = 0f;

        if (UseSideGun)
        {
            if (_crouchPositionAmt < 1) _crouchPositionAmt += Time.deltaTime * crouchPositionSpeed;
        }
        else if (_crouchPositionAmt > 0) _crouchPositionAmt -= Time.deltaTime * crouchPositionSpeed;
        _crouchPositionAmt = Mathf.Max(0, Mathf.Min(1, _crouchPositionAmt));

        angle += 30 * _crouchPositionAmt;
        pos += new Vector3(-0.1f, 0.02f, 0) * _crouchPositionAmt;

        var yawMovement = player.YawIncrease;
        var pitchMovement = _prevPitch - player.Pitch;

        _rightChange -= yawMovement / 400;
        _upChange -= pitchMovement / 400;

        forwardChange = Mathf.Lerp(forwardChange, 0, Time.deltaTime * 8);
        _rightChange = Mathf.Lerp(_rightChange, 0, Time.deltaTime * 7);
        _upChange = Mathf.Lerp(_upChange, 0, Time.deltaTime * 7);

        _forwardSoften = Mathf.Lerp(_forwardSoften, forwardChange, Time.deltaTime * 20);
        _rightSoften = Mathf.Lerp(_rightSoften, _rightChange, Time.deltaTime * 20);

        pos.z += _forwardSoften;
        pos.x += _rightSoften;

        pos.y += _fireKick / 60;
        pos.z -= _fireKick / 30;

        if (_fireKick > 0) _fireKick -= Time.deltaTime * 30; else _fireKick = 0;

        _angleEulers = Vector3.Lerp(_angleEulers, new Vector3(angle, -90, 0), Time.deltaTime * lerpSpeed);
        transform.localRotation = Quaternion.Euler(_angleEulers);
        transform.Rotate(new Vector3(-_fireKick, 0, 0), Space.Self);

        transform.localPosition = Vector3.Lerp(transform.localPosition, pos, Time.deltaTime * lerpSpeed);

        _prevPitch = player.Pitch;

        if (PlayerInput.GetKey(PlayerInput.PrimaryInteract) && Time.timeScale > 0 && _currentInfo.IsName("Loaded") && _canFire)
        {
            var proj = Instantiate(projectile.gameObject).GetComponent<Projectile>();

            var projection = Vector3.Dot(player.velocity, player.CrosshairDirection);
            if (projection < 1) projection = 1;
            proj.Fire(player.CrosshairDirection * projection, player.camera.transform.position, barrel.position);

            player.AudioManager.PlayOneShot(fireSound);

            _fireKick = 3;

            _animator.SetBool("Fire", true);
            _canFire = false;
        }
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}
