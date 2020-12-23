using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rifle : WeaponManager.Gun
{

    public override WeaponManager.GunType GetGunType() => WeaponManager.GunType.Rifle;

    public AudioClip fireSound;

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

    public bool UseSideGun
    {
        get
        {
            if (!Game.Player.jumpKitEnabled) return false;
            if (Game.Player.ApproachingWall) return false;
            if (_layer0Info.IsName("Unequip")) return false;

            if (Game.Player.IsDashing) return true;
            if (Flatten(Game.Player.velocity).magnitude > PlayerMovement.BASE_SPEED + 1) return true;
            if (!Game.Player.IsOnGround) return true;
            if (Game.Player.IsOnRail) return true;
            if (Game.Player.GrappleHooked) return true;
            return false;
        }
    }

    private void FixedUpdate()
    {
        if (animator == null) return;
        _layer0Info = animator.GetCurrentAnimatorStateInfo(0);
        _layer1Info = animator.GetCurrentAnimatorStateInfo(1);
    }

    private AnimatorStateInfo _layer0Info;
    private AnimatorStateInfo _layer1Info;

    private float _catchWeight;

    public void ReloadComplete()
    {
        animator.SetBool("Reload", false);
    }

    private void Update()
    {
        if (_layer1Info.IsName("AbilityCatch") && _layer1Info.normalizedTime < 1)
        {
            animator.SetLayerWeight(1, _catchWeight);

            var f = 1 - _layer1Info.normalizedTime;
            f -= 0.5f;
            f *= 5;
            f = Mathf.Max(0, f);
            f = Mathf.Min(1, f);
            _catchWeight = Mathf.Lerp(_catchWeight, f, Time.deltaTime * 20);
        }
        else
        {
            animator.SetLayerWeight(1, 0);
        }

        if (Input.GetKey(PlayerInput.PrimaryInteract) && Time.timeScale > 0 && !animator.GetBool("Unequip") && !animator.GetBool("Reload"))
        {
            Fire(QueryTriggerInteraction.Collide);

            Game.Player.source.PlayOneShot(fireSound);

            if (animator != null)
            {
                animator.Play("Fire", -1, 0f);
                animator.SetBool("Reload", true);

                WeaponManager.EquipGun(WeaponManager.GunType.Pistol);
            }
        }
    }

    private void LateUpdate()
    {
        var yawMovement = Game.Player.YawIncrease;

        var velocityChange = Game.Player.velocity - _prevVelocity;

        if (UseSideGun)
        {
            if (_crouchFactor < 1) _crouchFactor += Time.deltaTime * crouchPositionSpeed;
        }
        else if (_crouchFactor > 0) _crouchFactor -= Time.deltaTime * crouchPositionSpeed;
        _crouchFactor = Mathf.Max(0, Mathf.Min(1, _crouchFactor));

        var crouchAmt = -(Mathf.Cos(Mathf.PI * _crouchFactor) - 1) / 2;

        _upChange -= velocityChange.y * Time.deltaTime * 70;
        if (!Game.Player.IsOnGround && !Game.Player.IsOnWall) _upChange += Time.deltaTime * Mathf.Lerp(20, 10, crouchAmt);
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

        _prevVelocity = Game.Player.velocity;

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

        roll += Game.Player.CameraRoll / 2;

        var barrelPosition = barrel.position;
        var centerPosition = center.position;
        var stockPosition = stock.position;
        foreach (var model in parts)
        {
            model.transform.localPosition += new Vector3(forward, right, up);

            model.gameObject.transform.RotateAround(barrelPosition, rollAxis, roll);
            model.gameObject.transform.RotateAround(centerPosition, swingAxis, swing );
            model.gameObject.transform.RotateAround(stockPosition, tiltAxis, tilt);
        }

        rightHand.transform.localPosition += new Vector3(forward, right, up);

        rightHand.gameObject.transform.RotateAround(barrelPosition, rollAxis, roll);
        rightHand.gameObject.transform.RotateAround(centerPosition, swingAxis, swing);
        rightHand.gameObject.transform.RotateAround(stockPosition, tiltAxis, tilt);

        var mod = 1 - _catchWeight;

        leftHand.transform.localPosition += new Vector3(forward, right, up * mod);

        leftHand.gameObject.transform.RotateAround(barrelPosition, rollAxis, roll * mod);
        leftHand.gameObject.transform.RotateAround(centerPosition, swingAxis, swing * mod);
        leftHand.gameObject.transform.RotateAround(stockPosition, tiltAxis, tilt * mod);
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}
