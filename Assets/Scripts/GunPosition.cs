using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunPosition : MonoBehaviour
{
    public PlayerMovement player;

    public GameObject rightHand;
    public GameObject leftHand;
    public List<GameObject> parts;

    public Transform barrel;
    public Transform center;
    public Transform stock;

    private const float crouchPositionSpeed = 4;

    private float _upChange;
    private float _upSoften;
    private float _rightChange;
    private float _rightSoften;

    private Vector3 _prevVelocity;

    private float _crouchFactor;
    private float _crouchReloadMod;

    private float _uncrouchTimer;
    private float _uncrouchTimerDelay;

    private void UncrouchDuring(float from, float to)
    {
        _uncrouchTimer = to - from;
        _uncrouchTimerDelay = from;
    }

    public bool UseSideGun
    {
        get
        {
            if (!player.jumpKitEnabled) return false;
            if (player.ApproachingWall) return false;
            if (_uncrouchTimer > 0 && _uncrouchTimerDelay == 0) return false;
            //if (_shootingPistol && !_layer0Info.IsName("Fire")) return false;
            //if (!_shootingPistol && _layer0Info.IsName("PistolFire")) return false;

            if (player.IsDashing) return true;
            if (Flatten(player.velocity).magnitude > PlayerMovement.BASE_SPEED + 1) return true;
            if (!player.IsOnGround) return true;
            if (player.IsOnRail) return true;
            if (player.GrappleHooked) return true;
            return false;
        }
    }

    private void LateUpdate()
    {
        if (_uncrouchTimerDelay == 0)
        {
            _uncrouchTimer = Mathf.Max(0, _uncrouchTimer - Time.deltaTime);
        }
        else
        {
            _uncrouchTimerDelay = Mathf.Max(0, _uncrouchTimerDelay - Time.deltaTime);
        }
        var yawMovement = player.YawIncrease;

        var velocityChange = player.velocity - _prevVelocity;

        if (UseSideGun)
        {
            if (_crouchFactor < 1) _crouchFactor += Time.deltaTime * crouchPositionSpeed;
        }
        else if (_crouchFactor > 0) _crouchFactor -= Time.deltaTime * crouchPositionSpeed;
        _crouchFactor = Mathf.Max(0, Mathf.Min(1, _crouchFactor));

        //var crouchAmt = Mathf.Pow(_crouchFactor, 2);
        var crouchAmt = -(Mathf.Cos(Mathf.PI * _crouchFactor) - 1) / 2;

        _upChange -= velocityChange.y * Time.deltaTime * 70;
        if (!player.IsOnGround && !player.IsOnWall) _upChange += Time.deltaTime * Mathf.Lerp(20, 10, crouchAmt);
        else
        {
            _upChange -= velocityChange.y * Time.deltaTime * Mathf.Lerp(120, 60, crouchAmt);
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
        var right = -0.02f * crouchAmt;
        var up = Mathf.Lerp(0, 0.02f, crouchAmt);

        var roll = Mathf.Lerp(_rightSoften, 60, crouchAmt);
        var swing = _rightSoften / Mathf.Lerp(10, 5, crouchAmt);
        var tilt = _upSoften;

        var rollAxis = center.up;
        var swingAxis = center.right;
        var tiltAxis = center.forward;

        /*if (_layer0Info.IsTag("Reload"))
            _crouchReloadMod = Mathf.Lerp(_crouchReloadMod, crouchAmt, Time.deltaTime * 4);
        else
            _crouchReloadMod = Mathf.Lerp(_crouchReloadMod, 0, Time.deltaTime * 2);*/

        tilt += 5 * _crouchReloadMod;
        up -= 0.02f * _crouchReloadMod;
        right -= 0.005f * _crouchReloadMod;
        roll -= 5 * _crouchReloadMod;

        roll += player.CameraRoll / 2;

        var barrelPosition = barrel.position;
        var centerPosition = center.position;
        var stockPosition = stock.position;
        foreach (var model in parts)
        {
            var mod = 1f;
            //if (model == leftHand && _doingAbilityCatch)
                //mod = 1 - _catchWeight;
            model.transform.localPosition += new Vector3(forward, right, up * mod);

            model.gameObject.transform.RotateAround(barrelPosition, rollAxis, roll * mod);
            model.gameObject.transform.RotateAround(centerPosition, swingAxis, swing * mod);
            model.gameObject.transform.RotateAround(stockPosition, tiltAxis, tilt * mod);
        }
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}
