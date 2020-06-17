using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class Gun : MonoBehaviour
{

    public delegate void GunShot(RaycastHit hit);
    //public event GunShot ShotEvent;

    public PlayerMovement player;
    public Transform barrel;
    public Transform rifle;
    public Transform stock;
    public Camera viewModel;

    private const float crouchPositionSpeed = 6;

    private float _crouchPositionAmt;

    public static float forwardChange;
    private float _rightChange;

    private float _rightSoften;

    public Projectile projectile;

    public AudioClip fireSound;

    private List<ModelInView> _models;

    public Animator animator;

    private bool _canFire;

    private LineRenderer _lineRenderer;

    private void Start()
    {
        _canFire = true;

        _models = new List<ModelInView>(GetComponentsInChildren<ModelInView>());
        _lineRenderer = barrel.gameObject.GetComponent<LineRenderer>();
        if (_lineRenderer == null) _lineRenderer = barrel.gameObject.AddComponent<LineRenderer>();
    }

    private void FixedUpdate()
    {
        if (animator == null) return;
        _currentInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (_currentInfo.IsName("Fire"))
        {
            animator.SetBool("Fire", false);
            _canFire = true;
        }
    }

    private AnimatorStateInfo _currentInfo;

    private void Update()
    {
        if (_lineRenderer != null && Application.isPlaying)
        {
            var emission = _lineRenderer.material.GetColor("_EmissionColor");
            _lineRenderer.material.SetColor("_EmissionColor", Color.Lerp(emission, Color.black, Time.deltaTime * 4));
            var color = _lineRenderer.material.color;
            if (color.a > 0) color.a -= Time.deltaTime * 4;
            _lineRenderer.material.color = color;
        }
        if (!Input.GetKey(PlayerInput.PrimaryInteract) && !_canFire)
        {
            _canFire = true;
        }

        if (Input.GetKey(PlayerInput.PrimaryInteract) && Time.timeScale > 0 && _currentInfo.IsName("Idle") && _canFire)
        {
            var lineEnd = player.camera.transform.position + player.CrosshairDirection * 300;
            if (Physics.Raycast(player.camera.transform.position, player.CrosshairDirection, out var hit, 300, 1, QueryTriggerInteraction.Ignore))
            {
                if (!hit.collider.CompareTag("Player"))
                {
                    lineEnd = hit.point;
                    //ShotEvent(hit);
                }
            }

            var screen = viewModel.WorldToViewportPoint(barrel.transform.position);
            Vector3[] positions = { player.camera.ViewportToWorldPoint(screen), lineEnd };
            _lineRenderer.endWidth = 0.05f;
            _lineRenderer.startWidth = 0.05f;
            _lineRenderer.material.SetColor("_EmissionColor", Color.white * 4);
            _lineRenderer.SetPositions(positions);

            player.source.PlayOneShot(fireSound);

            if (animator != null)
            {
                animator.SetBool("Fire", true);
            }
            _canFire = false;
        }
    }

    private float _upChange;
    private float _upSoften;

    private Vector3 _prevVelocity;

    private float _crouchReloadMod;

    private void LateUpdate()
    {

        var yawMovement = player.YawIncrease;

        var velocityChange = player.velocity - _prevVelocity;

        if (player.IsSliding && !player.ApproachingWall && !player.IsOnWall)
        {
            if (_crouchPositionAmt < 1) _crouchPositionAmt += Time.deltaTime * crouchPositionSpeed;
        }
        else if (_crouchPositionAmt > 0) _crouchPositionAmt -= Time.deltaTime * crouchPositionSpeed;
        _crouchPositionAmt = Mathf.Max(0, Mathf.Min(1, _crouchPositionAmt));

        _upChange -= velocityChange.y * Time.deltaTime * 70;
        if (!player.IsOnGround) _upChange += Time.deltaTime * Mathf.Lerp(20, 10, _crouchPositionAmt);
        else
        {
            _upChange -= velocityChange.y * Time.deltaTime * Mathf.Lerp(120, 60, _crouchPositionAmt);
        }

        _rightChange -= yawMovement / 3;

        _rightChange = Mathf.Lerp(_rightChange, 0, Time.deltaTime * 20);
        _upChange = Mathf.Lerp(_upChange, 0, Time.deltaTime * 8);

        _rightSoften = Mathf.Lerp(_rightSoften, _rightChange, Time.deltaTime * 20);
        
        if (_upSoften > _upChange)
        {
            _upSoften = Mathf.Lerp(_upSoften, _upChange, Time.deltaTime * 20);
        }
        else
        {
            _upSoften = Mathf.Lerp(_upSoften, _upChange, Time.deltaTime * 10);
        }
        if (_upSoften > 5) _upSoften = 5;
        if (_upSoften < -5) _upSoften = -5;

        _prevVelocity = player.velocity;

        var forward = 0;
        var right = -0.02f * _crouchPositionAmt;
        var up = Mathf.Lerp(0, 0.02f, _crouchPositionAmt);

        var roll = Mathf.Lerp(_rightSoften, 60, _crouchPositionAmt);
        var swing = _rightSoften / Mathf.Lerp(10, 5, _crouchPositionAmt);
        var tilt = _upSoften;

        var barrelPosition = barrel.position;
        var riflePosition = rifle.position;
        var stockPosition = stock.position;

        var rollAxis = rifle.up;
        var swingAxis = rifle.right;
        var tiltAxis = rifle.forward;

        if (_currentInfo.IsTag("Reload"))
            _crouchReloadMod = Mathf.Lerp(_crouchReloadMod, _crouchPositionAmt, Time.deltaTime * 4);
        else
            _crouchReloadMod = Mathf.Lerp(_crouchReloadMod, 0, Time.deltaTime * 2);
        tilt += 5 * _crouchReloadMod;
        up -= 0.02f * _crouchReloadMod;
        right -= 0.005f * _crouchReloadMod;
        roll -= 5 * _crouchReloadMod;

        roll += player.CameraRoll / 2;

        if (Application.isPlaying)
        {
            foreach (var model in _models)
            {
                model.transform.localPosition += new Vector3(forward, right, up);

                model.gameObject.transform.RotateAround(barrelPosition, rollAxis, roll);
                model.gameObject.transform.RotateAround(riflePosition, swingAxis, swing);
                model.gameObject.transform.RotateAround(stockPosition, tiltAxis, tilt);
            }
        }
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}
