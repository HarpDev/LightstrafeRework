using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cannon : WeaponManager.Gun
{

    public override WeaponManager.GunType GetGunType() => WeaponManager.GunType.Cannon;

    public AudioClip fireSound;

    public List<GameObject> parts;

    public Transform barrel;
    public Transform center;
    public Transform stock;

    private const float CROUCH_POSITION_SPEED = 4;

    private float upChange;
    private float upSoften;
    private float rightChange;
    private float rightSoften;
    private float forward;

    private Vector3 prevVelocity;

    private float crouchFactor;
    private float crouchReloadMod;

    public bool UseSideGun
    {
        get
        {
            if (!Game.Player.jumpKitEnabled) return false;
            if (layer0Info.IsName("Unequip")) return false;

            if (Game.Player.IsSliding) return true;
            return false;
        }
    }

    private AnimatorStateInfo layer0Info;
    private AnimatorStateInfo layer1Info;

    private void FixedUpdate()
    {
        if (animator == null) return;
        layer0Info = animator.GetCurrentAnimatorStateInfo(0);
        layer1Info = animator.GetCurrentAnimatorStateInfo(1);
    }

    private float leftHandFactor;
    private bool fireInputConsumed;

    public Cannon(float crouchReloadMod)
    {
        this.crouchReloadMod = crouchReloadMod;
    }

    private void Update()
    {
        if ((layer1Info.normalizedTime <= 1 || layer1Info.IsTag("Hold")) && layer1Info.speed > 0)
        {
            leftHandFactor = Mathf.Lerp(leftHandFactor, 1, Time.deltaTime / 0.05f);
            if (layer1Info.IsTag("Instant")) leftHandFactor = 1;
        }
        else
        {
            leftHandFactor = Mathf.Lerp(leftHandFactor, 0, Time.deltaTime / 0.25f);
        }
        animator.SetLayerWeight(1, leftHandFactor);

        if (fireInputConsumed && !PlayerInput.GetKey(PlayerInput.PrimaryInteract)) fireInputConsumed = false;
        if (PlayerInput.GetKey(PlayerInput.PrimaryInteract) && Time.timeScale > 0 && !animator.GetBool("Unequip") && !fireInputConsumed)
        {
            Fire(QueryTriggerInteraction.Collide, Game.Player.CrosshairDirection);
            fireInputConsumed = true;

            Game.Player.AudioManager.PlayOneShot(fireSound);

            if (animator != null)
            {
                animator.Play("Fire", -1, 0f);
            }
        }
    }

    private void LateUpdate()
    {
        var yawMovement = Game.Player.YawIncrease;

        var velocityChange = Game.Player.velocity - prevVelocity;

        if (UseSideGun)
        {
            if (crouchFactor < 1) crouchFactor += Time.deltaTime * CROUCH_POSITION_SPEED;
        }
        else if (crouchFactor > 0) crouchFactor -= Time.deltaTime * CROUCH_POSITION_SPEED;
        crouchFactor = Mathf.Max(0, Mathf.Min(1, crouchFactor));

        var crouchAmt = -(Mathf.Cos(Mathf.PI * crouchFactor) - 1) / 2;

        upChange -= velocityChange.y / 15;
        if (!Game.Player.IsOnGround && !Game.Player.IsOnWall) upChange += Time.deltaTime * Mathf.Lerp(2, 1, crouchAmt);
        else
        {
            upChange -= velocityChange.y / Mathf.Lerp(25, 50, crouchAmt);
        }

        rightChange -= yawMovement / 3;

        rightChange = Mathf.Lerp(rightChange, 0, Time.deltaTime * 20);
        upChange = Mathf.Lerp(upChange, 0, Time.deltaTime * 8);

        rightSoften = Mathf.Lerp(rightSoften, rightChange, Time.deltaTime * 20);

        if (upSoften > upChange)
        {
            upSoften = Mathf.Lerp(upSoften, upChange, Time.deltaTime * 10);
        }
        else
        {
            upSoften = Mathf.Lerp(upSoften, upChange, Time.deltaTime * 5);
        }
        upSoften = Mathf.Clamp(upSoften, -1.3f, 1.3f);

        prevVelocity = Game.Player.velocity;

        if (Game.Player.IsDashing) forward += Time.deltaTime / 1.2f;
        forward = Mathf.Lerp(forward, 0, Time.deltaTime * 8);

        var localforward = forward;
        var localright = -0.02f * crouchAmt;
        var localup = Mathf.Lerp(0, 0.02f, crouchAmt);
        var globalup = upSoften / 15;

        var roll = Mathf.Lerp(rightSoften, 60, crouchAmt);
        var swing = rightSoften / Mathf.Lerp(10, 5, crouchAmt);
        var tilt = upSoften < 0 ? upSoften * 10 : upSoften;

        var rollAxis = center.up;
        var swingAxis = center.right;
        var tiltAxis = center.forward;

        tilt += 5 * crouchReloadMod;
        localup -= 0.02f * crouchReloadMod;
        localright -= 0.005f * crouchReloadMod;
        roll -= 5 * crouchReloadMod;

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
