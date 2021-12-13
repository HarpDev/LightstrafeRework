using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Rifle : WeaponManager.Gun
{

    public override WeaponManager.GunType GetGunType() => WeaponManager.GunType.Rifle;

    public AudioClip fireSound;
    public AudioClip boltUp;
    public AudioClip boltBack;
    public AudioClip boltForward;
    public AudioClip boltDown;

    public List<GameObject> parts;
    public GameObject animatedBoomerang;
    public GameObject projectileBoomerang;
    public Transform boomerangBone;

    public Transform barrel;
    public Transform center;
    public Transform stock;

    public SkinnedMeshRenderer rifleMesh;

    private const float crouchPositionSpeed = 4;

    private float _upChange;
    private float _upSoften;
    private float _rightChange;
    private float _rightSoften;
    private float _forward;

    private Vector3 _prevVelocity;

    private float _crouchFactor;
    private float _crouchReloadMod;

    public bool UseSideGun
    {
        get
        {
            if (!Game.Player.jumpKitEnabled) return false;
            if (_layer0Info.IsName("Unequip")) return false;

            if (Game.Player.IsSliding) return true;
            return false;
        }
    }

    public void ReloadComplete()
    {
        animator.SetBool("Reload", false);
    }

    public void BoltUp()
    {
        Game.Player.AudioManager.PlayAudio(boltUp);
    }

    public void BoltBack()
    {
        Game.Player.AudioManager.PlayAudio(boltBack);
    }

    public void BoltForward()
    {
        Game.Player.AudioManager.PlayAudio(boltForward);
    }

    public void BoltDown()
    {
        Game.Player.AudioManager.PlayAudio(boltDown);
    }

    private AnimatorStateInfo _layer0Info;
    private AnimatorStateInfo _layer1Info;

    private void FixedUpdate()
    {
        if (animator == null) return;
        _layer0Info = animator.GetCurrentAnimatorStateInfo(0);
        _layer1Info = animator.GetCurrentAnimatorStateInfo(1);
    }

    protected float leftHandFactor;
    private bool fireInputConsumed;

    private bool shotAvailable;

    private void Update()
    {
        if ((_layer1Info.normalizedTime <= 1 || _layer1Info.IsTag("Hold")) && _layer1Info.speed > 0)
        {
            leftHandFactor = Mathf.Lerp(leftHandFactor, 1, Time.deltaTime / 0.05f);
            if (_layer1Info.IsTag("Instant")) leftHandFactor = 1;
        }
        else
        {
            leftHandFactor = Mathf.Lerp(leftHandFactor, 0, Time.deltaTime / 0.25f);
        }
        animator.SetLayerWeight(1, leftHandFactor);

        animatedBoomerang.GetComponent<SkinnedMeshRenderer>().enabled = boomerangVisible;

        if (Game.Player.IsOnGround)
        {
            shotAvailable = true;
        }

        shotAvailable = true;

        if (fireInputConsumed && !PlayerInput.GetKey(PlayerInput.PrimaryInteract)) fireInputConsumed = false;
        if (PlayerInput.GetKey(PlayerInput.PrimaryInteract) && Time.timeScale > 0 && !animator.GetBool("Unequip") 
            && !animator.GetBool("Reload") && !fireInputConsumed && shotAvailable)
            //&& (Game.Player.IsOnGround || Game.Player.IsOnWall || Game.Player.ApproachingGround || Game.Player.ApproachingWall || Game.Player.IsInCoyoteTime()))
        {
            shotAvailable = false;
            //Fire(QueryTriggerInteraction.Collide, Game.Player.CrosshairDirection);
            var direction = Game.Player.CrosshairDirection;
            var triggerInteraction = QueryTriggerInteraction.Ignore;
            var obj = new GameObject("Tracer");
            obj.AddComponent<TracerDecay>();
            var line = obj.AddComponent<LineRenderer>();
            var positions = new Vector3[2];
            positions[0] = GetTracerStartWorldPosition();
            
            if (Physics.Raycast(Game.Player.camera.transform.position, direction, out var hit, 200, 1, triggerInteraction))
            {
                positions[1] = hit.point;
                if (!hit.collider.CompareTag("Player") && !hit.collider.CompareTag("Kill Block"))
                {
                    Game.Player.Teleport(hit.point - direction.normalized / 2);
                }
            } else positions[1] = Game.Player.camera.transform.position + (direction * 300);
            line.material = WeaponManager.tracerMaterial;
            line.endWidth = 0.1f;
            line.startWidth = 0.1f;
            line.SetPositions(positions);
            fireInputConsumed = true;

            Game.Player.AudioManager.PlayOneShot(fireSound);
            //Game.Player.weaponManager.EquipGun(WeaponManager.GunType.None);

            if (animator != null)
            {
                animator.Play("Fire", -1, 0f);
                animator.SetBool("Reload", true);
            }
        }

        if (PlayerInput.SincePressed(PlayerInput.TertiaryInteract) == 0 && Time.timeScale > 0 && boomerangAvailable)
        {
            if (animator != null)
            {
                animator.Play("BoomerangThrow", 1, 0f);
                boomerangVisible = true;
                boomerangAvailable = false;
            }
        }
    }

    private float aimFactor;
    private Vector3 toTargetVector;

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

        if (Game.Player.IsDashing) _forward += Time.deltaTime / 1.2f;
        _forward = Mathf.Lerp(_forward, 0, Time.deltaTime * 8);

        var localforward = _forward;
        var localright = -0.02f * crouchAmt;
        var localup = Mathf.Lerp(0, 0.02f, crouchAmt);
        var globalup = _upSoften / 15;

        var roll = Mathf.Lerp(_rightSoften, 60, crouchAmt);
        var swing = _rightSoften / Mathf.Lerp(10, 5, crouchAmt);
        var tilt = _upSoften < 0 ? _upSoften * 10 : _upSoften;

        var rollAxis = center.up;
        var swingAxis = center.right;
        var tiltAxis = center.forward;

        tilt += 5 * _crouchReloadMod;
        localup -= 0.02f * _crouchReloadMod;
        localright -= 0.005f * _crouchReloadMod;
        roll -= 5 * _crouchReloadMod;

        roll += Game.Player.CameraRoll / 2;

        /*if (closest != null)
        {
            aimFactor = Mathf.Lerp(aimFactor, 1, Time.deltaTime * 10);
            toTargetVector = closest.transform.position - Game.Player.camera.transform.position;
        } else
        {
            aimFactor = Mathf.Lerp(aimFactor, 0, Time.deltaTime * 10);
        }

        if (toTargetVector.magnitude > 0)
        {
            var aimT = Mathf.Atan2(Game.Player.CrosshairDirection.z, Game.Player.CrosshairDirection.x);
            var targetT = Mathf.Atan2(toTargetVector.z, toTargetVector.x);
            swing += Mathf.Rad2Deg * ((targetT - aimT) / 3.5f) * aimFactor;

            var aimP = Mathf.Acos(Game.Player.CrosshairDirection.y / Game.Player.CrosshairDirection.magnitude);
            var targetP = Mathf.Acos(toTargetVector.y / toTargetVector.magnitude);
            tilt += Mathf.Rad2Deg * ((aimP - targetP) / 3.5f) * aimFactor;
            tilt += 1 * aimFactor;
        }*/

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

        rightHand.transform.localPosition += new Vector3(localforward, localright, localup);

        rightHand.gameObject.transform.RotateAround(barrelPosition, rollAxis, roll);
        rightHand.gameObject.transform.RotateAround(centerPosition, swingAxis, swing);
        rightHand.gameObject.transform.RotateAround(stockPosition, tiltAxis, tilt);
        rightHand.transform.position += Vector3.up * globalup;

        var mod = 1 - leftHandFactor;

        leftHand.transform.localPosition += new Vector3(localforward, localright, localup * mod);

        leftHand.gameObject.transform.RotateAround(barrelPosition, rollAxis, roll * mod);
        leftHand.gameObject.transform.RotateAround(centerPosition, swingAxis, swing * mod);
        leftHand.gameObject.transform.RotateAround(stockPosition, tiltAxis, tilt * mod);
        leftHand.transform.position += Vector3.up * globalup;
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}
