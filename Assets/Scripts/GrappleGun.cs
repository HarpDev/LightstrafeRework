using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrappleGun : WeaponManager.Gun
{
    public override WeaponManager.GunType GetGunType() => WeaponManager.GunType.GrappleGun;

    public Transform barrel;
    public Transform center;
    public Transform stock;
    public List<GameObject> parts;

    public GameObject projectile;
    public Material ropeMaterial;

    private float _upChange;
    private float _upSoften;
    private float _rightChange;
    private float _rightSoften;
    private float _forward;

    private Vector3 _prevVelocity;

    private bool fireInputConsumed;

    private GameObject firedProjectile;

    private const float RANGE = 35;
    private float distanceTravelled;

    private void Start()
    {
        transform.parent = Game.Player.camera.transform;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    private bool rightClick;

    private void Update()
    {
        if (animator == null) return;
        if (fireInputConsumed && !PlayerInput.GetKey(PlayerInput.PrimaryInteract) && !PlayerInput.GetKey(PlayerInput.SecondaryInteract)) fireInputConsumed = false;
        if ((PlayerInput.GetKey(PlayerInput.PrimaryInteract) || PlayerInput.GetKey(PlayerInput.SecondaryInteract)) && Time.timeScale > 0 && !fireInputConsumed &&
            firedProjectile == null)
        {
            fireInputConsumed = true;
            rightClick = PlayerInput.GetKey(PlayerInput.SecondaryInteract);

            firedProjectile = Instantiate(projectile);
            var firePosition = barrel.transform.position;
            firedProjectile.transform.position = firePosition;
            firedProjectile.transform.LookAt(firePosition + Game.Player.CrosshairDirection);
            distanceTravelled = 0;
            var line = firedProjectile.AddComponent<LineRenderer>();
            line.startWidth = 0.1f;
            line.endWidth = 0.1f;
            line.material = ropeMaterial;


            animator.Play("Fire", -1, 0f);
        }

        var info = animator.GetCurrentAnimatorStateInfo(0);
        if (firedProjectile == null && info.IsName("Fired"))
        {
            animator.Play("Idle", -1, 0f);
        }
    }

    private void LateUpdate()
    {
        var yawMovement = Game.Player.YawIncrease;

        var velocityChange = Game.Player.velocity - _prevVelocity;

        _upChange -= velocityChange.y / 15;
        if (!Game.Player.IsOnGround && !Game.Player.IsOnWall) _upChange += Time.deltaTime * 2;

        _rightChange -= yawMovement / 3;

        _rightChange = Mathf.Lerp(_rightChange, 0, Time.deltaTime * 20);
        _upChange = Mathf.Lerp(_upChange, 0, Time.deltaTime * 8);

        _rightSoften = Mathf.Lerp(_rightSoften, _rightChange, Time.deltaTime * 20);

        if (_upSoften > _upChange)
        {
            _upSoften = Mathf.Lerp(_upSoften, _upChange, Time.deltaTime * 10);
        }
        else
        {
            _upSoften = Mathf.Lerp(_upSoften, _upChange, Time.deltaTime * 5);
        }

        _upSoften = Mathf.Clamp(_upSoften, -1.3f, 1.3f);

        _prevVelocity = Game.Player.velocity;

        _forward = Mathf.Lerp(_forward, 0, Time.deltaTime * 8);

        var localforward = _forward;
        var localright = 0;
        var localup = 0;
        var globalup = _upSoften / 15;

        var roll = _rightSoften;
        var swing = _rightSoften / 10;
        var tilt = _upSoften < 0 ? _upSoften * 10 : _upSoften;

        var rollAxis = center.up;
        var swingAxis = center.right;
        var tiltAxis = center.forward;

        roll += Game.Player.CameraRoll / 2;

        var barrelPosition = barrel.position;
        var centerPosition = center.position;
        var stockPosition = stock.position;
        foreach (var model in parts)
        {
            model.transform.localPosition += new Vector3(localforward, localright, localup);

            model.gameObject.transform.RotateAround(barrelPosition, rollAxis, roll);
            model.gameObject.transform.RotateAround(centerPosition, swingAxis, swing);
            model.gameObject.transform.RotateAround(stockPosition, tiltAxis, tilt);
            model.transform.position += Vector3.up * globalup;
        }

        if (firedProjectile != null)
        {
            if (distanceTravelled < RANGE)
            {
                var d = Time.deltaTime * 180;
                if (Physics.Raycast(firedProjectile.transform.position - firedProjectile.transform.forward, firedProjectile.transform.forward, out var hit,
                    d, 1, QueryTriggerInteraction.Ignore))
                {
                    firedProjectile.transform.position = hit.point;
                    if (rightClick)
                    {
                        var vec = hit.point - Game.Player.transform.position;
                        //Game.Player.Dash(vec.normalized, vec.magnitude + 10f);
                    }
                    else
                    {
                        Game.Player.AttachGrapple(hit.point);
                    }
                    Destroy(firedProjectile);
                }
                else
                {
                    firedProjectile.transform.position += firedProjectile.transform.forward * d;
                    distanceTravelled += d;
                }

                var line = firedProjectile.GetComponent<LineRenderer>();
                var positions = new Vector3[2];
                positions[0] = GetTracerStartWorldPosition();
                positions[1] = firedProjectile.transform.position;
                line.SetPositions(positions);
            }
            else
            {
                Destroy(firedProjectile);
            }
        }
    }
}