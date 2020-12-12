using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Gun : MonoBehaviour
{

    public delegate void GunShot(RaycastHit hit, ref bool doCatch);
    public event GunShot ShotEvent;

    public PlayerMovement player;
    public Transform barrel;
    public Transform rifle;
    public Transform stock;
    public Camera viewModel;

    public SkinnedMeshRenderer handMesh;

    private const float crouchPositionSpeed = 4;

    private float _crouchFactor;

    public static float forwardChange;
    private float _rightChange;

    private float _rightSoften;

    public Projectile projectile;

    public AudioClip fireSound;

    private List<ModelInView> _models;
    public ModelInView leftHand;

    public Animator animator;

    private LineRenderer _lineRenderer;

    private float _chargeTarget;
    private float _charge;

    private const float FIRE_RATE = 1.5f;
    private float _fireDelay;

    private bool _shootingPistol;

    public void ChargeHands()
    {
        _chargeTarget = 3;
        player.DashAvailable = true;
    }

    private void Start()
    {
        player = Game.Player;

        _models = new List<ModelInView>(GetComponentsInChildren<ModelInView>());
        _lineRenderer = barrel.gameObject.GetComponent<LineRenderer>();
        if (_lineRenderer == null) _lineRenderer = barrel.gameObject.AddComponent<LineRenderer>();

        handMesh.material.SetColor("_EmissionColor", Color.black);
    }

    private bool _doingAbilityCatch;

    private void FixedUpdate()
    {
        if (animator == null) return;
        _layer0Info = animator.GetCurrentAnimatorStateInfo(0);
        _layer1Info = animator.GetCurrentAnimatorStateInfo(1);
    }

    private AnimatorStateInfo _layer0Info;
    private AnimatorStateInfo _layer1Info;

    private float _catchWeight;

    private void Update()
    {
        if (_fireDelay > 0) _fireDelay -= Mathf.Min(_fireDelay, Time.deltaTime);

        if (_layer1Info.IsName("AbilityCatch") && _layer1Info.normalizedTime < 1)
        {
            animator.SetLayerWeight(1, _catchWeight);
            _doingAbilityCatch = true;

            var f = 1 - _layer1Info.normalizedTime;
            f -= 0.1f;
            f *= 3;
            f = Mathf.Max(0, f);
            f = Mathf.Min(1, f);
            _catchWeight = Mathf.Lerp(_catchWeight, f, Time.deltaTime * 10);
        }
        else
        {
            animator.SetLayerWeight(1, 0);
            _doingAbilityCatch = false;
        }

        handMesh.material.SetColor("_EmissionColor", Color.white * _charge);
        handMesh.material.EnableKeyword("_EMISSION");
        _charge = Mathf.Lerp(_charge, _chargeTarget, Time.deltaTime * 2);
        _chargeTarget = Mathf.Lerp(_chargeTarget, 0, Time.deltaTime);

        if (_lineRenderer != null && Application.isPlaying)
        {
            var emission = _lineRenderer.material.GetColor("_EmissionColor");
            _lineRenderer.material.SetColor("_EmissionColor", Color.Lerp(emission, Color.black, Time.deltaTime * 4));
            var color = _lineRenderer.material.color;
            if (color.a > 0) color.a -= Time.deltaTime * 4;
            _lineRenderer.material.color = color;
        }

        if (Input.GetKey(PlayerInput.PrimaryInteract) && Time.timeScale > 0 && _fireDelay == 0)
        {
            bool doCatch = false;
            _fireDelay = FIRE_RATE;

            var lineEnd = player.camera.transform.position + player.CrosshairDirection * 300;
            if (Physics.Raycast(player.camera.transform.position, player.CrosshairDirection, out var hit, 300, 1, QueryTriggerInteraction.Collide))
            {
                if (!hit.collider.CompareTag("Player"))
                {
                    lineEnd = hit.point;
                    //player.Teleport(hit.point - (player.CrosshairDirection * 0.5f));
                    ShotEvent(hit, ref doCatch);
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
                animator.SetBool("Hit", doCatch);
                if (_shootingPistol)
                {
                    animator.Play("PistolFire", -1, 0f);
                    _shootingPistol = false;
                }
                else
                {
                    animator.Play("Fire", -1, 0f);
                    _shootingPistol = true;
                }
                UncrouchDuring(0.4f, 2f);
            }
        }
    }

    public void CatchAbility()
    {
        animator.Play("AbilityCatch", 1, 0f);
    }

    private float _uncrouchTimer;
    private float _uncrouchTimerDelay;

    private void UncrouchDuring(float from, float to)
    {
        _uncrouchTimer = to - from;
        _uncrouchTimerDelay = from;
    }

    private float _upChange;
    private float _upSoften;

    private Vector3 _prevVelocity;

    private float _crouchReloadMod;
    public bool UseSideGun
    {
        get
        {
            if (!player.jumpKitEnabled) return false;
            if (player.ApproachingWall) return false;
            if (_uncrouchTimer > 0 && _uncrouchTimerDelay == 0) return false;
            if (_shootingPistol) return false;

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

        var barrelPosition = barrel.position;
        var riflePosition = rifle.position;
        var stockPosition = stock.position;

        var rollAxis = rifle.up;
        var swingAxis = rifle.right;
        var tiltAxis = rifle.forward;

        if (_layer0Info.IsTag("Reload"))
            _crouchReloadMod = Mathf.Lerp(_crouchReloadMod, crouchAmt, Time.deltaTime * 4);
        else
            _crouchReloadMod = Mathf.Lerp(_crouchReloadMod, 0, Time.deltaTime * 2);
        tilt += 5 * _crouchReloadMod;
        up -= 0.02f * _crouchReloadMod;
        right -= 0.005f * _crouchReloadMod;
        roll -= 5 * _crouchReloadMod;

        roll += player.CameraRoll / 2;

        foreach (var model in _models)
        {
            if (model == leftHand && _doingAbilityCatch)
                continue;
            model.transform.localPosition += new Vector3(forward, right, up);

            model.gameObject.transform.RotateAround(barrelPosition, rollAxis, roll);
            model.gameObject.transform.RotateAround(riflePosition, swingAxis, swing);
            model.gameObject.transform.RotateAround(stockPosition, tiltAxis, tilt);
        }
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}
