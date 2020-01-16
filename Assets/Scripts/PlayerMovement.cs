using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    public new Rigidbody rigidbody;
    public new Camera camera;
    public Collider standingHitbox;
    public Image crosshair;

    public Vector3 cameraPosition;
    public Vector3 velocity;

    public Slider grappleStaminaIndicator;
    public Bow bow;

    public enum Gimmick
    {
        NONE,
        GRAPPLE,
        DASH,
        BOW
    }

    public Gimmick gimmick;
    public bool jumpKitEnabled = true;

    private const float wallFriction = 0.5f;
    private const float wallCatchFriction = 10f;
    private const float wallCorrectionSpeed = 4;
    private const float wallCorrectionUpMultiplier = 4;
    private const float wallCorrectionAcceleration = 15;
    private const float wallSpeed = 15f;
    private const float wallAcceleration = 30f;
    private const float wallJumpSpeed = 10f;
    private const float wallAngleGive = 10f;

    private const float wallLeanDegrees = 20f;
    private const float wallLeanPreTime = 0.3f;

    private const float cameraRotationCorrectSpeed = 4f;

    private const float movementSpeed = 13f;
    private const float groundAcceleration = 30f;
    private const float groundFriction = 2f;
    private const float slideAccelMultiplier = 2f;

    private const float airMinSpeed = 8;
    private const float doubleJumpForce = 12;
    private const float airAcceleration = 40f;
    private const float airAccelSpeed = 1f;

    private const float groundAngle = 45;

    private const float railSpeed = 20f;
    private const float railAcceleration = 24f;
    private const int railCooldownTicks = 40;

    private const float dashSpeed = 15f;
    private const float dashDistance = 15f;

    private const float dashCancelTempSpeed = 30f;
    private const float dashCancelSpeed = 25f;
    private const float dashCancelAcceleration = 3f;
    private const float dashCancelPotentialMultiplier = 1.65f;

    private const float excededFriction = 4;

    private const float gravity = 0.45f;
    private const float fallSpeed = 40f;

    private const float jumpHeight = 12f;
    private const int coyoteTicks = 20;
    private const int jumpForgiveness = 10;
    private const float jumpCameraThunk = 5f;

    private const float grappleControlSpeed = 10f;
    private const float grappleControlAcceleration = 10f;
    private const float grappleDistance = 30f;
    private const float grappleAcceleration = 30f;
    private const float grappleTopSpeed = 25f;
    private const float grappleStaminaDecay = 1;
    private const float grappleStaminaMax = 200;
    private const float grappleStaminaRecharge = 1;
    private const float grappleStaminaRechargeDelay = 1000;

    private const float bouncePadSpeed = 36f;

    /* Audio */
    public AudioSource source;
    public AudioSource rollSound;
    public AudioSource grappleDuring;
    public AudioClip jump;
    public AudioClip jumpair;
    public AudioClip ding;
    public AudioClip groundLand;
    public AudioClip wallKick;
    public AudioClip wallJump;
    public AudioClip grappleAttach;
    public AudioClip grappleRelease;
    public AudioSource railSound;
    public AudioClip railLand;
    public AudioClip railEnd;

    public LineRenderer grappleTether;
    public float grappleYOffset;
    public bool GrappleHooked { get; set; }

    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public float LookScale { get; set; }

    private bool _approachingWall;
    private float _approachingWallDistance;
    private float _wallHighestY;
    private float _wallLowestY;
    private Vector3 _wallNormal;
    private int _wallTimestamp = -100000;
    private int _cancelLeanTickCount;
    private float _currentLean;

    private int _railTimestamp = -100000;
    private int _railCooldownTimestamp = -100000;
    private int _railDirection;
    private Vector3 _railLeanVector;
    private GameObject _lastRail;
    private Rail _currentRail;

    private int _grappleTimestamp = -100000;
    private Vector3 _grappleAttachPosition;
    private Transform _grappleAttachTransform;
    private float _grappleStamina;

    private float _dashAddPotential;
    private float _dashAddSpeed;
    private float _excededSpeed;
    private int _dashTicks;

    private int _groundTimestamp = -100000;
    private bool _landed;
    private Vector3 _previousPosition;
    private Collision _previousCollision;
    private Vector3 _previousCollisionLocalPosition;
    private Vector3 _displacePosition;
    private float _crouchAmount;
    private int _sinceJumpCounter;
    private Vector3 _slideLeanVector;
    private float _cameraRotation;
    private float _cameraRotationSpeed;
    private float _motionInterpolationDelta;
    private float _crosshairRotation;

    public float CameraRoll { get; set; }

    public static bool DashAvailable { get; set; }

    public static bool DoubleJumpAvailable { get; set; }

    public bool IsGrounded { get; set; }

    public bool IsSliding
    {
        get { return (Flatten(velocity).magnitude > movementSpeed && jumpKitEnabled) || IsOnRail; }
    }

    public bool IsOnWall { get; set; }

    public bool IsDashing { get; set; }

    public bool IsStrafing
    {
        get
        {
            return PlayerInput.GetAxisStrafeRight() != 0 && PlayerInput.GetAxisStrafeForward() == 0;
        }
    }

    public bool IsOnRail
    {
        get { return _currentRail != null; }
    }

    public Vector3 Wishdir { get; set; }

    public Vector3 CrosshairDirection { get; set; }

    public Vector3 InterpolatedPosition
    {
        get
        {
            return Vector3.Lerp(_previousPosition, rigidbody.transform.position,
                _motionInterpolationDelta / Time.fixedDeltaTime);
        }
    }

    public void SetCameraRotation(float value, float speed)
    {
        _cameraRotation = value;
        _cameraRotationSpeed = speed;
    }

    private void Awake()
    {
        LookScale = 1;
        _grappleStamina = grappleStaminaMax;

        Game.Level.StopTimer();
        Game.Level.ResetTimer();
        Yaw += transform.rotation.eulerAngles.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        grappleTether.useWorldSpace = false;
        grappleTether.enabled = false;
        grappleDuring.volume = 0;
    }

    private void Update()
    {
        if (Cursor.visible) return;

        // Wallkick display fade out
        var c = crosshair.color;
        if (c.r < 1) c.r += Time.deltaTime;
        if (c.g < 1) c.g += Time.deltaTime;
        if (c.b < 1) c.b += Time.deltaTime;
        crosshair.color = c;

        // Mouse motion
        Yaw = (Yaw + Input.GetAxis("Mouse X") * (Game.Sensitivity / 10) * LookScale) % 360f;
        Pitch -= Input.GetAxis("Mouse Y") * (Game.Sensitivity / 10) * LookScale;
        Pitch = Mathf.Max(Pitch, -90);
        Pitch = Mathf.Min(Pitch, 90);

        // This is where orientation is handled, the camera is only adjusted by the pitch, and the entire player is adjusted by yaw
        var cam = camera.transform;

        CameraRoll = Mathf.Lerp(CameraRoll, _cameraRotation, Time.deltaTime * _cameraRotationSpeed);

        camera.transform.localRotation =
            Quaternion.Euler(new Vector3(Pitch + HudMovement.rotationSlamVectorLerp.y, 0, CameraRoll));
        CrosshairDirection = cam.forward;
        transform.rotation = Quaternion.Euler(0, Yaw, 0);

        // This value is used to calcuate the positions in between each fixedupdate tick
        _motionInterpolationDelta += Time.deltaTime;

        // Check for level restart
        if (Input.GetKeyDown(PlayerInput.RestartLevel)) Game.RestartLevel();

        var position = cameraPosition;
        var crouchChange = _crouchAmount;
        if (IsSliding)
        {
            if (standingHitbox.enabled) standingHitbox.enabled = false;
            if (_crouchAmount < 1) _crouchAmount += Time.deltaTime * 6;

        }
        else
        {
            if (!standingHitbox.enabled) standingHitbox.enabled = true;
            if (_crouchAmount > 0)
            {
                _crouchAmount -= Time.deltaTime * 6;
                if (!IsOnRail && !IsOnWall)
                {
                    SetCameraRotation(0, 5);
                }
            }
        }

        crouchChange -= _crouchAmount;
        transform.position -= Vector3.up * crouchChange;

        position.y -= 0.6f * _crouchAmount;
        camera.transform.position = InterpolatedPosition + position;

        var targetFOV = Flatten(velocity).magnitude + (100 - movementSpeed);
        targetFOV = Mathf.Max(targetFOV, 100);
        targetFOV = Mathf.Min(targetFOV, 120);
        targetFOV -= bow.Drawback * 10;

        camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, targetFOV, Time.deltaTime * 5);

        if (DashAvailable)
        {
            _crosshairRotation = Mathf.Lerp(_crosshairRotation, 0, Time.deltaTime * 20);
        }
        else
        {
            _crosshairRotation = Mathf.Lerp(_crosshairRotation, 45, Time.deltaTime * 20);
        }
        crosshair.transform.rotation = Quaternion.Euler(new Vector3(0, 0, _crosshairRotation));

        if (gimmick == Gimmick.GRAPPLE && Input.GetKeyDown(PlayerInput.PrimaryInteract))
        {
            if (!GrappleHooked)
            {
                if (Physics.Raycast(camera.transform.position, CrosshairDirection, out var grapple, 100, 1, QueryTriggerInteraction.Ignore))
                {
                    AttachGrapple(grapple.transform, grapple.point);
                }
            }
        }
        if (gimmick == Gimmick.GRAPPLE && !Input.GetKey(PlayerInput.PrimaryInteract))
        {
            if (GrappleHooked) DetachGrapple();
        }
        if (gimmick == Gimmick.DASH && Input.GetKeyDown(PlayerInput.PrimaryInteract))
        {
            Dash(CrosshairDirection);
        }

        if (gimmick == Gimmick.GRAPPLE && !grappleStaminaIndicator.gameObject.activeSelf)
        {
            grappleStaminaIndicator.gameObject.SetActive(true);
        }
        if (gimmick != Gimmick.GRAPPLE && grappleStaminaIndicator.gameObject.activeSelf)
        {
            grappleStaminaIndicator.gameObject.SetActive(false);
        }

        if (gimmick == Gimmick.BOW && !bow.gameObject.activeSelf)
        {
            bow.gameObject.SetActive(true);
        }
        if (gimmick != Gimmick.BOW && bow.gameObject.activeSelf)
        {
            bow.gameObject.SetActive(false);
        }
    }

    private void FixedUpdate()
    {
        // Set Wishdir
        Wishdir = (transform.right * PlayerInput.GetAxisStrafeRight() +
                   transform.forward * PlayerInput.GetAxisStrafeForward()).normalized;

        // Start the timer when the player moves
        if ((Wishdir.magnitude > 0 || PlayerInput.SincePressed(PlayerInput.PrimaryInteract) <= 1) && !Game.Level.TimerRunning)
        {
            Game.Level.StartTimer();
        }

        // Timestamps used for coyote time
        if (IsGrounded) _groundTimestamp = PlayerInput.tickCount;
        if (IsOnWall) _wallTimestamp = PlayerInput.tickCount;
        if (IsOnRail)
        {
            _railCooldownTimestamp = PlayerInput.tickCount;
            _railTimestamp = PlayerInput.tickCount;
        }

        // This value gets set to 0 on successful jump
        _sinceJumpCounter++;

        // Movement happens here
        var factor = Time.fixedDeltaTime;
        if (GrappleHooked)
            GrappleMove(factor);
        else if (IsOnRail)
            RailMove(factor);
        else if (IsOnWall)
            WallMove(factor);
        else if (IsGrounded)
            GroundMove(factor);
        else
            AirMove(factor);

        if (gimmick == Gimmick.GRAPPLE)
        {
            if (!GrappleHooked && _grappleStamina < grappleStaminaMax && Environment.TickCount - _grappleTimestamp > grappleStaminaRechargeDelay) _grappleStamina += grappleStaminaRecharge;
            grappleStaminaIndicator.value = _grappleStamina;
            grappleStaminaIndicator.maxValue = grappleStaminaMax;
            grappleStaminaIndicator.minValue = 0;
        }

        if (_excededSpeed > 0 && !IsDashing)
        {
            var speed = Flatten(velocity).magnitude;
            var y = velocity.y;

            var newspeed = Mathf.Lerp(speed, speed - _excededSpeed, factor * excededFriction);

            _excededSpeed -= speed - newspeed;
            velocity = Flatten(velocity).normalized * newspeed;
            velocity.y = y;
        }

        if (IsDashing && _dashTicks > 0)
        {
            _dashTicks--;
        }
        else if (_dashTicks == 0)
        {
            StopDash();
        }

        // Interpolation delta resets to 0 every fixed update tick, its used to measure the time since the last fixed update tick
        _motionInterpolationDelta = 0;

        // Here we measure how much the current collider the player is standing on has moved, and move their position by that amount
        var platformMotion = new Vector3();
        if (_previousCollision != null)
        {
            if (_previousCollision.transform.hasChanged && IsGrounded && _previousCollisionLocalPosition.magnitude > 0)
            {
                var world = _previousCollision.transform.TransformPoint(_previousCollisionLocalPosition);
                platformMotion = world - _previousPosition;
                platformMotion.y = 0;
                _previousCollision.transform.hasChanged = false;
            }

            _previousCollisionLocalPosition = _previousCollision.transform.InverseTransformPoint(rigidbody.transform.position);
        }
        else _previousCollisionLocalPosition = new Vector3();

        // Calculate the players position for the next tick
        _previousPosition = rigidbody.transform.position;
        var movement = platformMotion + velocity * Time.fixedDeltaTime;

        // The previous collision is set in oncollision, executed after fixedupdate
        _previousCollision = null;
        // cant use MovePosition() because it doesnt use continuous collision
        // rigidbody.MovePosition(_previousPosition + movement + _displacePosition);
        rigidbody.velocity = (movement + _displacePosition) / Time.fixedDeltaTime;
        _previousPosition -= _displacePosition;
        _displacePosition = new Vector3();

        if (!IsGrounded && !IsOnWall) _landed = false;
        IsGrounded = false;

        _wallHighestY = Mathf.Lerp(_wallHighestY, 0, factor / 10);
        _wallLowestY = Mathf.Lerp(_wallLowestY, 0, factor / 10);
    }

    private void OnCollisionExit(Collision other)
    {
        _previousCollision = null;

        if (IsOnWall)
        {
            rollSound.volume = 0;
            IsOnWall = false;
            IsGrounded = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Rail Grab
        if (other.CompareTag("Rail") && (PlayerInput.tickCount - _railCooldownTimestamp > railCooldownTicks ||
                                         other.transform.parent.gameObject != _lastRail))
        {
            _lastRail = other.transform.parent.gameObject;
            SetRail(other.gameObject.transform.parent.gameObject.GetComponent<Rail>());
        }
    }

    private void OnCollisionStay(Collision other)
    {
        _previousCollision = other;
        _previousCollision.transform.hasChanged = false;

        if (other.collider.CompareTag("Instant Kill Block"))
        {
            Game.RestartLevel();
        }

        velocity += other.impulse;

        foreach (var point in other.contacts)
        {
            var projection = Vector3.Dot(velocity, -point.normal);
            var angle = Vector3.Angle(Vector3.up, point.normal);

            if (angle < groundAngle)
            {
                if (_sinceJumpCounter > jumpForgiveness)
                {
                    if (!_landed)
                    {
                        _landed = true;
                        DashAvailable = true;
                        DoubleJumpAvailable = true;
                        source.PlayOneShot(groundLand);
                    }
                    IsGrounded = true;
                }

                if (other.collider.CompareTag("Bounce Block"))
                {
                    Accelerate(Vector3.up, bouncePadSpeed, bouncePadSpeed);
                    return;
                }
                if (other.collider.CompareTag("Kill Block"))
                {
                    Game.RestartLevel();
                    return;
                }
            }
            if (other.collider.CompareTag("Wall") && Mathf.Abs(Vector3.Angle(Vector3.up, point.normal) - 90) < wallAngleGive && !IsGrounded && jumpKitEnabled &&
                Vector3.Distance(Flatten(point.point), Flatten(transform.position)) >= 0.5)
            {
                var y = transform.position.y - point.point.y;
                if (_landed)
                {
                    if (y < _wallLowestY) _wallLowestY = y;
                    if (y > _wallHighestY) _wallHighestY = y;
                }
                // Wall Grab
                _wallNormal = point.normal;
                if (_sinceJumpCounter > jumpForgiveness)
                {
                    if (!_landed)
                    {
                        _landed = true;
                        DoubleJumpAvailable = true;
                        source.PlayOneShot(groundLand);
                    }
                    IsOnWall = true;
                }
            }
        }
    }

    public void Dash(Vector3 wishdir)
    {
        if (!DashAvailable) return;
        StopDash();
        IsDashing = true;
        DashAvailable = false;
        source.Play();

        HudMovement.RotationSlamVector += Vector3.up * jumpCameraThunk;

        var angle = Vector3.Angle(wishdir, Flatten(wishdir));

        if (angle < 45)
        {
            var x1 = Flatten(wishdir.normalized).magnitude;
            var y1 = wishdir.normalized.y;
            var x2 = Flatten(velocity).magnitude;
            var y2 = (y1 * x2) / x1;

            velocity = Flatten(wishdir).normalized * x2;
            velocity.y = y2;
        }
        else
        {
            if (wishdir.y < 0 || velocity.y > 0)
            {
                velocity = wishdir.normalized * velocity.magnitude;
            }
            else
            {
                var ogspeed = Flatten(velocity).magnitude;
                var y1 = wishdir.normalized.y;
                var x1 = Flatten(wishdir.normalized).magnitude;
                var y2 = velocity.y;
                var x2 = (x1 * y2) / y1;

                velocity = Flatten(wishdir).normalized * x2;
                velocity.y = y2;
                velocity = velocity.magnitude * wishdir.normalized;
                if (velocity.magnitude < ogspeed) velocity = velocity.normalized * ogspeed;
            }
        }

        if (velocity.magnitude < wallSpeed) velocity = CrosshairDirection * wallSpeed;

        var add = velocity.normalized * dashSpeed;
        _dashAddSpeed = Flatten(add).magnitude;
        _dashAddPotential = add.y;
        velocity += velocity.normalized * dashSpeed;

        var time = dashDistance / velocity.magnitude;
        var ticks = Mathf.RoundToInt(time / Time.fixedDeltaTime);
        _dashTicks = ticks;
    }

    public bool CancelDash()
    {
        if (StopDash())
        {
            Accelerate(Flatten(velocity).normalized, dashCancelSpeed, dashCancelAcceleration);
            var beforeSpeed = Flatten(velocity).magnitude;
            velocity += Flatten(velocity).normalized * dashCancelTempSpeed;
            _excededSpeed += Flatten(velocity).magnitude - beforeSpeed;
            velocity.y *= dashCancelPotentialMultiplier;
            source.PlayOneShot(wallKick);
            return true;
        }
        return false;
    }

    public bool StopDash()
    {
        if (IsDashing)
        {
            IsDashing = false;
            velocity -= Flatten(velocity).normalized * _dashAddSpeed;
            if (velocity.y > 0 && !GrappleHooked) velocity.y = Mathf.Max(0, velocity.y - jumpHeight);
            return true;
        }
        return false;
    }

    public void SetRail(Rail rail)
    {
        if (IsOnRail) return;
        _currentRail = rail;
        source.PlayOneShot(railLand);
        railSound.Play();
        railSound.volume = 1;
        if (GrappleHooked) DetachGrapple();
    }

    public void EndRail()
    {
        if (!IsOnRail) return;
        SetCameraRotation(0, 8);
        _currentRail = null;
        railSound.Stop();
        _railDirection = 0;
        railSound.PlayOneShot(railEnd);
    }

    private Vector3 GetBalanceVector(int i)
    {
        var range = Mathf.Min((_currentRail.smoothedPoints.Length - 1) / 2, 4);
        var index = i;
        if (index < range) index = range;
        if (index >= _currentRail.smoothedPoints.Length - range) index = _currentRail.smoothedPoints.Length - range - 1;
        var point = _currentRail.smoothedPoints[index];

        var p1 = _currentRail.smoothedPoints[index - range];
        var p2 = _currentRail.smoothedPoints[index + range];

        var a = Vector3.Dot(point - p1, (p2 - p1).normalized);
        var b = Vector3.Dot(point - p2, (p1 - p2).normalized);

        var ratio = a / (a + b);

        var p3 = Vector3.Lerp(p1, p2, ratio);

        var leanVector = p3 - point;
        leanVector.y = Mathf.Abs(leanVector.y);

        var balance = leanVector + Vector3.up * (gravity * 3);

        return balance.normalized;
    }

    public void RailMove(float f)
    {
        if (!IsOnRail) return;

        var closeIndex = 0;
        var closeDistance = float.MaxValue;
        for (var i = 0; i < _currentRail.smoothedPoints.Length; i++)
        {
            var close = _currentRail.smoothedPoints[i] + GetBalanceVector(i);
            var distance = Vector3.Distance(transform.position, close);
            if (distance > closeDistance) continue;
            closeDistance = distance;
            closeIndex = i;
        }

        DashAvailable = true;
        DoubleJumpAvailable = true;

        if (_railDirection == 0)
        {
            var c = _currentRail.smoothedPoints[closeIndex];
            var p1Angle = 90f;
            if (closeIndex != 0)
            {
                var a = _currentRail.smoothedPoints[closeIndex - 1];
                p1Angle = Vector3.Angle(Flatten(velocity), Flatten(a - c));
            }

            var p2Angle = 90f;
            if (closeIndex < _currentRail.smoothedPoints.Length - 1)
            {
                var a = _currentRail.smoothedPoints[closeIndex + 1];
                p2Angle = Vector3.Angle(Flatten(velocity), Flatten(a - c));
            }

            if (p1Angle < p2Angle)
            {
                _railDirection = -1;
            }
            else
            {
                _railDirection = 1;
            }

            try
            {
                var forward = _currentRail.smoothedPoints[closeIndex - _railDirection] -
                              _currentRail.smoothedPoints[closeIndex];
                var p = Vector3.Dot(velocity, forward.normalized);
                velocity = velocity.normalized * p;
            }
            catch (IndexOutOfRangeException)
            {
            }
        }

        if (Jump()) return;

        var current = _currentRail.smoothedPoints[closeIndex] + _railLeanVector;

        Vector3 next;
        if (_railDirection == 1 && closeIndex == _currentRail.smoothedPoints.Length - 1)
        {
            next = current + (current - (_currentRail.smoothedPoints[closeIndex - 1] + _railLeanVector));
        }
        else if (_railDirection == -1 && closeIndex == 0)
        {
            next = current + (current - (_currentRail.smoothedPoints[1] + _railLeanVector));
        }
        else
        {
            next = _currentRail.smoothedPoints[closeIndex + _railDirection] + _railLeanVector;
        }


        var railVector = -(current - next).normalized;

        if ((_railDirection == -1 && closeIndex == 0 || _railDirection == 1 && closeIndex == _currentRail.smoothedPoints.Length - 1) && Vector3.Dot(transform.position - current, railVector) > 0)
        {
            EndRail();
            return;
        }

        _railLeanVector = Vector3.Lerp(_railLeanVector, GetBalanceVector(closeIndex + _railDirection), f * 20);

        var correctionVector = -(transform.position - next).normalized;
        velocity = velocity.magnitude * Vector3.Lerp(railVector, correctionVector, f * 20).normalized;

        Accelerate(velocity.normalized, railSpeed, f * railAcceleration);
        if (velocity.y < 0) Gravity(f);

        var totalAngle = Vector3.Angle(Vector3.up, _railLeanVector) / 2f;
        var projection = Vector3.Dot(_railLeanVector.normalized * totalAngle, -transform.right);
        SetCameraRotation(projection, 6);

        railSound.pitch = Mathf.Min(Mathf.Max(velocity.magnitude / 10, 1), 2);
    }

    public void Teleport(Vector3 position)
    {
        _displacePosition += position - _displacePosition - transform.position;
    }

    public void AttachGrapple(Transform t, Vector3 position)
    {
        if (!enabled) return;
        source.PlayOneShot(grappleAttach);
        Game.Level.hitmarker.Display();
        if (GrappleHooked) return;
        if (IsOnRail) EndRail();
        _grappleAttachPosition = t.TransformPoint(position);
        _grappleAttachTransform = t;
        GrappleHooked = true;
        grappleTether.enabled = true;
        grappleDuring.volume = 0.4f;
        grappleDuring.Play();

        var towardPoint = (position - transform.position).normalized;
        var yankProjection = Vector3.Dot(velocity, towardPoint);
        if (yankProjection < 0) velocity -= towardPoint * yankProjection;
    }

    public void DetachGrapple()
    {
        if (GrappleHooked) GrappleHooked = false;
        if (grappleTether.enabled) grappleTether.enabled = false;
        SetCameraRotation(0, 5);
        grappleDuring.volume = 0;

        source.PlayOneShot(grappleRelease);
    }

    public void GrappleMove(float f)
    {
        _grappleTimestamp = Environment.TickCount;
        Jump();
        var camTrans = camera.transform;
        var position = _grappleAttachTransform.InverseTransformPoint(_grappleAttachPosition);

        var list = new List<Vector3>
            {new Vector3(0, grappleYOffset, 0), camTrans.InverseTransformPoint(position)};

        grappleTether.positionCount = list.Count;
        grappleTether.SetPositions(list.ToArray());

        camTrans.localPosition = new Vector3();

        var towardPoint = (position - camTrans.position).normalized;
        var velocityProjection = Mathf.Abs(Vector3.Dot(velocity, camTrans.right));
        var relativePoint = transform.InverseTransformPoint(position);

        var value = Mathf.Atan2(relativePoint.z, relativePoint.x) * Mathf.Rad2Deg;
        value = Mathf.Abs(value);
        value -= 90;
        value /= 90;

        grappleDuring.pitch = velocity.magnitude / 30f;

        SetCameraRotation(velocityProjection * value, 6);

        var yankProjection = Vector3.Dot(velocity, towardPoint);
        if (yankProjection < 0) velocity -= towardPoint * yankProjection;

        Accelerate(towardPoint, grappleTopSpeed, f * grappleAcceleration);
        Accelerate(Wishdir, grappleControlSpeed, f * grappleControlAcceleration);

        if (Vector3.Distance(position, camTrans.position) > grappleDistance)
        {
            var mag = velocity.magnitude;
            velocity = Vector3.Lerp(velocity, towardPoint * velocity.magnitude, f / 5);
            velocity = velocity.normalized * mag;
        }

        _grappleStamina -= grappleStaminaDecay;
        if (_grappleStamina <= 0)
        {
            DetachGrapple();
        }
    }

    public void WallMove(float f)
    {
        if (Jump()) return;

        rollSound.pitch = Mathf.Min(Mathf.Max(velocity.magnitude / 20, 1), 2);
        rollSound.volume = Mathf.Min(velocity.magnitude / 30, 1);

        DoubleJumpAvailable = true;

        var normal = Flatten(_wallNormal);

        var projection = Vector3.Dot(CrosshairDirection, new Vector3(-normal.z, normal.y, normal.x));

        SetCameraRotation(wallLeanDegrees * -projection, 8);
        _currentLean = 1;

        source.pitch = 1;

        if (velocity.y < 0)
        {
            velocity.y = Mathf.Lerp(velocity.y, 0, f * wallCatchFriction);
        }
        else
        {
            Gravity(f);
        }

        if (Wishdir.magnitude > 0)
        {
            var direction = new Vector3(_wallNormal.z, 0, -_wallNormal.x);
            if (Vector3.Angle(Wishdir, direction) < 90)
                Accelerate(direction, wallSpeed, wallAcceleration * f);
            else
                Accelerate(-direction, wallSpeed, wallAcceleration * f);
        }

        if (_wallLowestY > -0.4f)
        {
            Accelerate(Vector3.down, wallCorrectionSpeed, wallCorrectionAcceleration * f);
        }
        if (_wallHighestY < 0.4f)
        {
            Accelerate(Vector3.up, wallCorrectionSpeed, wallCorrectionAcceleration * f * wallCorrectionUpMultiplier);
        }

        ApplyFriction(f * wallFriction, 0, wallSpeed);

        Accelerate(-_wallNormal, fallSpeed, gravity * f);
    }

    public void Gravity(float f, bool inverse = false)
    {
        if (!IsDashing && velocity.y > -fallSpeed) velocity.y = Mathf.Lerp(velocity.y, -fallSpeed, gravity * f);
    }

    public void GroundMove(float f)
    {
        rollSound.pitch = Mathf.Min(Mathf.Max(velocity.magnitude / 20, 1), 2);
        rollSound.volume = Mathf.Min(velocity.magnitude / 30, 1);

        Gravity(f);
        DashAvailable = true;
        DoubleJumpAvailable = true;
        if (IsSliding)
        {
            _slideLeanVector = Vector3.Lerp(_slideLeanVector, Flatten(velocity).normalized, f * 4);
            var leanProjection = Vector3.Dot(_slideLeanVector, camera.transform.right);
            SetCameraRotation(leanProjection * 15, 6);
        }
        else
        {
            _slideLeanVector = Flatten(velocity).normalized;
        }

        if (IsSliding)
        {
            AirAccelerate(f * slideAccelMultiplier);
        }
        else
        {
            GroundAccelerate(f);
        }

        Jump();
    }

    public void GroundAccelerate(float f)
    {
        ApplyFriction(f * groundFriction, 0, movementSpeed);
        if (Wishdir.magnitude > 0)
        {
            var y = velocity.y;
            var speed = Flatten(velocity).magnitude;

            velocity += Wishdir * f * groundAcceleration * slideAccelMultiplier;

            if (Flatten(velocity).magnitude >= movementSpeed && Vector3.Angle(Wishdir, velocity) < 90)
            {
                velocity = Flatten(velocity).normalized * speed;
                velocity.y = y;
            }
        }
    }

    public void AirMove(float f)
    {
        Gravity(f);
        _slideLeanVector = Flatten(velocity).normalized;

        AirAccelerate(f);

        rollSound.volume = 0;

        Jump();

        if (!jumpKitEnabled) return;

        // Lean in
        var pos = InterpolatedPosition;
        var didHit = rigidbody.SweepTest(velocity.normalized, out RaycastHit hit, velocity.magnitude * wallLeanPreTime,
            QueryTriggerInteraction.Ignore);
        if (didHit && Math.Abs(Vector3.Angle(Vector3.up, hit.normal) - 90) < wallAngleGive && hit.collider.CompareTag("Wall"))
        {
            var close = hit.point;

            var distance = Vector3.Dot(close - pos, hit.normal) - 0.5f;
            if (!_approachingWall)
            {
                _approachingWall = true;
                _approachingWallDistance = distance;
            }

            var rotation = (_approachingWallDistance - distance) / _approachingWallDistance;
            _currentLean = rotation;

            rotation *= 2 - rotation;

            var normal = Flatten(hit.normal);
            var projection = Vector3.Dot(CrosshairDirection, new Vector3(-normal.z, normal.y, normal.x));
            SetCameraRotation(wallLeanDegrees * rotation * -projection, 15);
            _cancelLeanTickCount = 0;
        }
        else
        {
            if (_approachingWall)
            {
                _cancelLeanTickCount++;
            }
            if (_cancelLeanTickCount >= 5)
            {
                _approachingWall = false;
                SetCameraRotation(0, cameraRotationCorrectSpeed);
                _currentLean = 0;
            }
        }
    }

    public void AirAccelerate(float f)
    {

        var frictionMod = 1 - Mathf.Min(Flatten(velocity).magnitude / movementSpeed, 1);
        GroundAccelerate(f * frictionMod * 0.65f);
        f *= 1 - frictionMod;

        Accelerate(Wishdir, airAccelSpeed, airAcceleration * f);

        if (!_approachingWall && !IsStrafing && Vector3.Angle(Wishdir, velocity) < 90)
        {
            var speed = Flatten(velocity).magnitude;
            var y = velocity.y;
            velocity += Wishdir * f * airAcceleration;
            velocity = Flatten(velocity).normalized * speed;
            velocity.y = y;
        }
    }

    public void ApplyFriction(float f, float minimumSpeed = 0, float deceleration = 0)
    {
        var speed = Flatten(velocity).magnitude;
        if (speed < deceleration)
        {
            minimumSpeed = speed - deceleration;
        }
        var newspeed = Mathf.Lerp(speed, minimumSpeed, f);

        if (newspeed > speed) return;
        if (newspeed < 0)
        {
            newspeed = 0;
        }

        var y = velocity.y;
        velocity = Flatten(velocity).normalized * newspeed;
        velocity.y = y;
    }

    public void Accelerate(Vector3 wishdir, float wishspeed, float accel)
    {
        var currentspeed = Vector3.Dot(velocity - (Flatten(velocity).normalized * _excededSpeed), wishdir);
        var addspeed = Mathf.Abs(wishspeed) - currentspeed;

        if (addspeed <= 0)
            return;

        var accelspeed = Mathf.Abs(accel);
        if (accelspeed > addspeed)
            accelspeed = addspeed;

        velocity.x += accelspeed * wishdir.x;
        velocity.y += accelspeed * wishdir.y;
        velocity.z += accelspeed * wishdir.z;
    }

    public bool Jump()
    {
        if (PlayerInput.SincePressed(PlayerInput.Jump) < jumpForgiveness)
        {
            var wallJump = PlayerInput.tickCount - _wallTimestamp < coyoteTicks;

            if (wallLeanPreTime * (1 - _currentLean) / Time.fixedDeltaTime < jumpForgiveness && !wallJump)
            {
                return false;
            }

            var groundJump = PlayerInput.tickCount - _groundTimestamp < coyoteTicks;
            _groundTimestamp = -coyoteTicks;
            var railJump = PlayerInput.tickCount - _railTimestamp < coyoteTicks;
            _railTimestamp = -coyoteTicks;

            if (!groundJump && !railJump && !wallJump && !DoubleJumpAvailable) return false;
            _sinceJumpCounter = 0;

            SetCameraRotation(0, cameraRotationCorrectSpeed);

            if (wallJump)
            {
                IsOnWall = false;
                _wallTimestamp = -coyoteTicks;

                velocity += _wallNormal * wallJumpSpeed;

                CancelDash();

                DoubleJumpAvailable = true;

                Accelerate(Vector3.up, 0, jumpHeight);
                velocity.y = Mathf.Max(jumpHeight, velocity.y);

                rollSound.volume = 0;
                PlayerInput.ConsumeBuffer(PlayerInput.Jump);
                return true;
            }
            PlayerInput.ConsumeBuffer(PlayerInput.Jump);

            CancelDash();

            if (!groundJump && !railJump)
            {
                if (!jumpKitEnabled) return false;
                DoubleJumpAvailable = false;

                if (Wishdir.magnitude > 0)
                {
                    var speedvector = Flatten(velocity);
                    var addvector = Wishdir * doubleJumpForce;

                    var projection = Vector3.Dot(addvector, speedvector.normalized);
                    if (projection < 0) addvector += addvector.normalized * projection;

                    velocity += addvector;
                    if (Flatten(velocity).magnitude > speedvector.magnitude)
                    {
                        var y = velocity.y;
                        velocity = Flatten(velocity).normalized * speedvector.magnitude;
                        velocity.y = y;
                    }

                    Accelerate(Wishdir, airMinSpeed, airMinSpeed / 2);
                }
            }

            if (groundJump || railJump)
            {
                DoubleJumpAvailable = true;
                DashAvailable = true;
            }

            velocity.y = Mathf.Max(jumpHeight, velocity.y + (railJump ? jumpHeight : 0));

            rollSound.volume = 0;
            IsGrounded = false;

            if (IsOnRail) EndRail();

            var slam = HudMovement.RotationSlamVector;
            slam.y += jumpCameraThunk;
            HudMovement.RotationSlamVector = slam;
            return true;
        }
        return false;
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}