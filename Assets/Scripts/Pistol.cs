using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pistol : WeaponManager.Gun
{
    public override WeaponManager.GunType GetGunType() => WeaponManager.GunType.Pistol;

    public AudioClip fireSound;

    public List<GameObject> parts;

    public Transform rollTransform;
    public Transform swingTransform;
    public Transform tiltTransform;

    private const float crouchPositionSpeed = 4;

    private float _upChange;
    private float _upSoften;
    private float _rightChange;
    private float _rightSoften;

    private Vector3 _prevVelocity;

    private float _crouchFactor;
    private float _crouchReloadMod;

    private const float FIRE_RATE = 0.1f;
    private const float EQUIP_TIME = 1.1f;
    private float _fireDelay = EQUIP_TIME;

    public int Shots { get; set; }

    public bool UseSideGun
    {
        get
        {
            if (!Game.Player.jumpKitEnabled) return false;
            if (Game.Player.ApproachingWall) return false;
            if (_layer0Info.IsName("Unequip")) return false;
            if (_layer0Info.IsName("Equip")) return false;

            if (Game.Player.IsSliding) return true;
            return false;
        }
    }

    private void Start()
    {
        Shots = 3;
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
    private bool _fireInputConsumed;

    private void Update()
    {
        if (_fireDelay > 0) _fireDelay -= Mathf.Min(_fireDelay, Time.deltaTime);

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

        if (_fireInputConsumed && !PlayerInput.GetKey(PlayerInput.PrimaryInteract)) _fireInputConsumed = false;
        if (PlayerInput.GetKey(PlayerInput.PrimaryInteract) && Time.timeScale > 0 && _fireDelay == 0 && Shots > 0 && !_fireInputConsumed)
        {
            _fireDelay = FIRE_RATE;

            Fire(QueryTriggerInteraction.Collide, Game.Player.CrosshairDirection);

            Game.Player.AudioManager.PlayOneShot(fireSound);
            _fireInputConsumed = true;

            if (animator != null)
            {
                animator.Play("Fire", -1, 0f);
                if (--Shots <= 0)
                {
                    WeaponManager.EquipGun(WeaponManager.GunType.Rifle);
                }
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
        _upChange -= velocityChange.y / 15;
        if (!Game.Player.IsOnGround && !Game.Player.IsOnWall) _upChange += Time.deltaTime * Mathf.Lerp(2, 1, crouchAmt);
        else
        {
            _upChange -= velocityChange.y / Mathf.Lerp(25, 50, crouchAmt);
        }

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

        var forward = 0;
        var right = -0.02f * crouchAmt;
        var up = Mathf.Lerp(0, 0.02f, crouchAmt);
        var globalup = _upSoften / 15;

        var roll = Mathf.Lerp(_rightSoften, 60, crouchAmt);
        var swing = _rightSoften / Mathf.Lerp(10, 5, crouchAmt);
        var tilt = _upSoften;

        var rollAxis = swingTransform.up;
        var swingAxis = swingTransform.right;
        var tiltAxis = swingTransform.forward;

        tilt += 5 * _crouchReloadMod;
        up -= 0.02f * _crouchReloadMod;
        right -= 0.005f * _crouchReloadMod;
        roll -= 5 * _crouchReloadMod;

        roll += Game.Player.CameraRoll / 2;

        var rollPosition = rollTransform.position;
        var swingPosition = swingTransform.position;
        var tiltPosition = tiltTransform.position;
        foreach (var model in parts)
        {
            model.transform.localPosition += new Vector3(forward, right, up);

            model.gameObject.transform.RotateAround(rollPosition, rollAxis, roll);
            model.gameObject.transform.RotateAround(swingPosition, swingAxis, swing);
            model.gameObject.transform.RotateAround(tiltPosition, tiltAxis, tilt);
            model.transform.position += Vector3.up * globalup;
        }

        rightHand.transform.localPosition += new Vector3(forward, right, up);

        rightHand.gameObject.transform.RotateAround(rollPosition, rollAxis, roll);
        rightHand.gameObject.transform.RotateAround(swingPosition, swingAxis, swing);
        rightHand.gameObject.transform.RotateAround(tiltPosition, tiltAxis, tilt);
        rightHand.transform.position += Vector3.up * globalup;

        var mod = 1 - _catchWeight;

        leftHand.transform.localPosition += new Vector3(forward, right, up * mod);

        leftHand.gameObject.transform.RotateAround(rollPosition, rollAxis, roll * mod);
        leftHand.gameObject.transform.RotateAround(swingPosition, swingAxis, swing * mod);
        leftHand.gameObject.transform.RotateAround(tiltPosition, tiltAxis, tilt * mod);
        leftHand.transform.position += Vector3.up * globalup;
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}
