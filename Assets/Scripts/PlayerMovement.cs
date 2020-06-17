using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{

    public struct SpeedAccel
    {
        public float speed;
        public float acceleration;
        public SpeedAccel(float speed, float acceleration)
        {
            this.speed = speed;
            this.acceleration = acceleration;
        }
    }

    public delegate void PlayerContact(Vector3 normal, Collider collider);
    public event PlayerContact ContactEvent;

    public new Camera camera;
    public MeshCollider standingCollider;
    public MeshCollider crouchingCollider;
    public GameObject abilityDot;
    public new Rigidbody rigidbody;

    public Rings rings;

    public Vector3 cameraPosition;
    public Vector3 velocity;

    public bool jumpKitEnabled = true;

    public enum Ability
    {
        GRAPPLE,
        DASH
    }

    public float Yaw { get; set; }
    public float YawFutureInterpolation { get; set; }
    public float YawIncrease { get; set; }
    public float Pitch { get; set; }
    public float LookScale { get; set; }
    public Collider CurrentCollider { get; private set; }
    public float CameraRoll { get; set; }
    public Vector3 Wishdir { get; set; }
    public Vector3 CrosshairDirection { get; set; }
    public Vector3 InterpolatedPosition { get { return Vector3.Lerp(_previousPosition, transform.position, _motionInterpolationDelta / Time.fixedDeltaTime); } }
    public bool IsSliding
    {
        get
        {
            if (!jumpKitEnabled) return false;
            if (IsDashing) return true;
            if (Flatten(velocity).magnitude > groundSpeed.speed + 1) return true;
            if (!IsOnGround && Flatten(velocity).magnitude < _previousSpeed) return true;
            if (IsOnGround && Flatten(velocity).magnitude >= groundSpeed.speed - 1) return true;
            if (IsOnWall) return true;
            if (IsOnRail) return true;
            if (GrappleHooked) return true;
            if (_isDownColliding && !IsOnGround) return true;
            return false;
        }
    }
    public float Gravity { get { return (velocity.y - Mathf.Lerp(velocity.y, -terminalVelocity, gravity)) * (IsDashing ? 0 : 1); } }

    // Surfaces have a "level" to make them a little more sticky than being able to come off them in 1 tick.
    // This is to prevent repeated landings on surfaces.
    public bool IsOnWall { get { return _wallLevel > 0; } set { if (value) _wallLevel = surfaceMaxLevel; else _wallLevel = 0; } }
    private int _wallLevel;
    public bool IsOnGround { get { return _groundLevel > 0; } set { if (value) _groundLevel = surfaceMaxLevel; else _groundLevel = 0; } }
    private int _groundLevel;
    private const int surfaceMaxLevel = 5;

    private const float cameraRotationCorrectSpeed = 4f;
    private SpeedAccel groundSpeed = new SpeedAccel(20, 200);
    private const float groundAngle = 45;
    private const float groundFriction = 5f;
    private const float slideMovementScale = 2f;
    private const float slideFriction = 0.2f;
    private int _groundTickCount;
    private int _groundTimestamp = -100000;
    private Vector3 _previousPosition;
    private float _previousSpeed;
    private float _crouchAmount;
    private Vector3 _slideLeanVector;
    private float _motionInterpolationDelta;
    private float _crosshairRotation;
    private bool _isDownColliding;

    private float _cameraRotation;
    private float _cameraRotationSpeed;
    public void SetCameraRotation(float value, float speed)
    {
        _cameraRotation = value;
        _cameraRotationSpeed = speed;
    }

    public bool ApproachingWall { get; set; }
    private const float wallCatchFriction = 10f;
    private SpeedAccel wallSpeed = new SpeedAccel(25, 80);
    private const float wallFriction = 5f;
    private const float wallNeutralFriction = 1f;
    private const int wallFrictionTicks = 5;
    private const float wallJumpAngle = 0.3f;
    private const float wallAngleGive = 10f;
    private const float wallStamina = 200f;
    private SpeedAccel wallEndBoostSpeed = new SpeedAccel(1, 1);
    private const float wallLeanDegrees = 20f;
    private const float wallLeanPreTime = 0.3f;
    private SpeedAccel wallJumpSpeed = new SpeedAccel(40, 0.2f);
    private Vector3 _wallNormal;
    private int _wallTimestamp = -100000;
    private int _wallTickCount;
    private float _wallStamina;
    private int _cancelLeanTickCount;
    private GameObject _lastWall;
    private GameObject _currentWall;

    private const float airSpeed = 2f;
    private const float airStrafeAcceleration = 500f;
    private const float backAirStrafeAcceleration = 80f;

    private const float surfAirAccelMultiplier = 50f;

    public bool IsOnRail { get { return _currentRail != null; } }
    private const int railCooldownTicks = 40;
    private const float railSpeed = 80f;
    private int _railTimestamp = -100000;
    private int _railCooldownTimestamp = -100000;
    private int _railDirection;
    private Vector3 _railLeanVector;
    private GameObject _lastRail;
    private Rail _currentRail;

    public bool IsBeingYoinked { get; set; }
    private const float yoinkAcceleration = 190f;
    private const int yoinkMaxTicks = 60;
    private const float yoinkMinEndSpeed = 40;
    private int _yoinkTicks;
    private Vector3 _yoinkTarget;
    private Vector3 _yoinkDirection;
    private float _yoinkStartSpeed;

    public enum ChargeType
    {
        NONE,
        DASH,
        DOUBLE_JUMP
    }

    private ChargeType _currentCharge = ChargeType.NONE;

    public bool IsDashing { get { return _dashVector.magnitude > 0.05f; } }
    private const float dashSpeed = 25;
    private const int dashCooldown = 300;
    private SpeedAccel dashCancelSpeed = new SpeedAccel(25, 1);
    private const float dashCancelTempSpeed = 20;
    private const float dashCancelTempSpeedDecay = 20;
    private const float dashTime = 0.35f;
    private const float dashDecay = 35f;
    private float _dashTime;
    private float _dashCancelTempSpeed;
    private int _dashTimestamp = -100000;
    private Vector3 _dashVector;

    private const float gravity = 0.5f;
    private const float terminalVelocity = 60;

    private const float jumpHeight = 15f;
    private const int coyoteTicks = 20;
    private const int jumpForgiveness = 6;

    public LineRenderer grappleTether;
    public float grappleYOffset;
    public bool GrappleHooked { get; set; }
    private const float grappleControlAcceleration = 40f;
    private const float grappleDistance = 25f;
    private SpeedAccel grappleSpeed = new SpeedAccel(80, 40);
    private const float grappleCorrectionAcceleration = 0.02f;
    private const int grappleMaxTicks = 200;
    private int _grappleTicks;
    private Vector3 _grappleAttachPosition;
    public bool TimerRunning { get; private set; }

    public bool LevelCompleted { get; private set; }

    public float CurrentTime { get; set; }

    private bool _wishTimerStart;

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
    public AudioClip trick;

    private void Awake()
    {
        ContactEvent += new PlayerContact(ContactCollider);
        LookScale = 1;

        Yaw += transform.rotation.eulerAngles.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        grappleTether.useWorldSpace = false;
        grappleTether.enabled = false;
        grappleDuring.volume = 0;

        standingCollider.enabled = true;
        crouchingCollider.enabled = false;
        CurrentCollider = standingCollider;

        if (Game.I.lastCheckpoint.sqrMagnitude > 0.05f)
        {
            transform.position = Game.I.lastCheckpoint;
        }

        if (Physics.Raycast(transform.position, Vector3.down, out var hit, 5f, 1, QueryTriggerInteraction.Ignore) && Vector3.Angle(hit.normal, Vector3.up) < groundAngle)
        {
            transform.position = hit.point + Vector3.up * 0.3f;
            IsOnGround = true;
            _groundTickCount = 1;
        }
    }

    public bool IsPaused()
    {
        return Game.UiTree.Count != 0;
    }

    public void Pause()
    {
        Time.timeScale = 0;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Game.OpenPauseMenu();

        if (Game.PostProcessVolume.profile.TryGetSettings(out Blur blur))
        {
            blur.BlurIterations.value = 8;
            blur.enabled.value = true;
        }
    }

    public void Unpause()
    {
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Game.PostProcessVolume.profile.TryGetSettings(out Blur blur);
        if (blur != null)
        {
            blur.BlurIterations.value = 0;
            blur.enabled.value = false;
        }
    }

    public void EndTimer()
    {
        if (TimerRunning)
        {
            LevelCompleted = true;
            TimerRunning = false;
            CurrentTime *= 100;
            CurrentTime = (int)CurrentTime;
            CurrentTime /= 100f;
            var level = SceneManager.GetActiveScene().name;
            if (CurrentTime < Game.GetBestLevelTime(level) || Game.GetBestLevelTime(level) < 0f)
            {
                Game.SetBestLevelTime(level, CurrentTime);
                TimerDisplay.color = Color.yellow;
            }
            else
            {
                TimerDisplay.color = Color.green;
            }
        }
    }

    private void Update()
    {
        if (TimerRunning)
        {
            CurrentTime += Time.unscaledDeltaTime;
        }
        if (Input.GetKeyDown(PlayerInput.Pause))
        {
            if (!IsPaused()) Pause();
        }
        if (!IsPaused() && Cursor.visible) Unpause();

        if (Input.GetKeyDown(PlayerInput.PrimaryInteract))
        {
            _wishTimerStart = true;
        }

        if (Cursor.visible) return;

        // Mouse motion
        if (Time.timeScale > 0)
        {
            YawIncrease = Input.GetAxis("Mouse X") * (Game.Sensitivity / 10) * LookScale;
            YawIncrease += Input.GetAxis("Joy 1 X 2") * Game.Sensitivity * LookScale;

            Yaw = (Yaw + YawIncrease) % 360f;

            var interpolation = Mathf.Lerp(Yaw, Yaw + YawFutureInterpolation, Time.deltaTime * 10) - Yaw;
            Yaw += interpolation;
            YawFutureInterpolation -= interpolation;

            Pitch -= Input.GetAxis("Mouse Y") * (Game.Sensitivity / 10) * LookScale;
            Pitch += Input.GetAxis("Joy 1 Y 2") * Game.Sensitivity * LookScale;

            Pitch = Mathf.Max(Pitch, -90);
            Pitch = Mathf.Min(Pitch, 90);
        }

        // This is where orientation is handled, the camera is only adjusted by the pitch, and the entire player is adjusted by yaw
        var cam = camera.transform;

        CameraRoll = Mathf.Lerp(CameraRoll, _cameraRotation, Time.deltaTime * _cameraRotationSpeed);

        camera.transform.localRotation = Quaternion.Euler(new Vector3(Pitch, 0, CameraRoll));
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
            standingCollider.enabled = false;
            crouchingCollider.enabled = true;
            CurrentCollider = crouchingCollider;

            if (_crouchAmount < 1) _crouchAmount += Time.deltaTime * 6;
        }
        else
        {
            standingCollider.enabled = true;
            crouchingCollider.enabled = false;
            CurrentCollider = standingCollider;

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
        //if (!_isDownColliding && !IsGrounded) transform.position -= Vector3.up * crouchChange;

        position.y -= 0.6f * _crouchAmount;

        camera.transform.position = InterpolatedPosition + position;

        var targetFOV = velocity.magnitude + (100 - groundSpeed.speed);
        targetFOV = Mathf.Max(targetFOV, 100);
        targetFOV = Mathf.Min(targetFOV, 120);

        camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, targetFOV, Time.deltaTime * 5);

        if (_currentCharge != ChargeType.NONE)
        {
            _crosshairRotation = Mathf.Lerp(_crosshairRotation, 0, Time.deltaTime * 20);
        }
        else
        {
            _crosshairRotation = Mathf.Lerp(_crosshairRotation, 45, Time.deltaTime * 20);
        }
        Game.Canvas.crosshair.transform.rotation = Quaternion.Euler(new Vector3(0, 0, _crosshairRotation));
    }

    private void FixedUpdate()
    {
        if ((Flatten(velocity).magnitude > 0.01f || _wishTimerStart) && !TimerRunning && CurrentTime == 0 && !LevelCompleted)
        {
            TimerRunning = true;
        }
        _wishTimerStart = false;

        // Set Wishdir
        Wishdir = (transform.right * PlayerInput.GetAxisStrafeRight() + transform.forward * PlayerInput.GetAxisStrafeForward()).normalized;
        if (Wishdir.magnitude <= 0)
        {
            Wishdir = (transform.right * Input.GetAxis("Joy 1 X") + transform.forward * -Input.GetAxis("Joy 1 Y")).normalized;
        }

        // Timestamps used for coyote time
        if (IsOnGround) _groundTimestamp = PlayerInput.tickCount;
        if (IsOnWall) _wallTimestamp = PlayerInput.tickCount;
        if (IsOnRail)
        {
            _railCooldownTimestamp = PlayerInput.tickCount;
            _railTimestamp = PlayerInput.tickCount;
        }

        // Movement happens here
        var factor = Time.fixedDeltaTime;
        if (IsBeingYoinked)
            YoinkMove(factor);
        else if (GrappleHooked)
            GrappleMove(factor);
        else if (IsOnRail)
            RailMove(factor);
        else if (IsOnWall)
            WallMove(factor);
        else if (IsOnGround)
            GroundMove(factor);
        else
            AirMove(factor);

        if (ApproachingWall)
        {
            _cancelLeanTickCount++;
        }
        if (_cancelLeanTickCount >= 5 && !IsOnWall)
        {
            ApproachingWall = false;
            SetCameraRotation(0, cameraRotationCorrectSpeed);
        }

        if (!IsOnWall) _wallTickCount = 0;
        if (!IsOnGround) _groundTickCount = 0;

        if (_dashTime > 0)
        {
            _slideLeanVector = Vector3.Lerp(_slideLeanVector, Flatten(velocity).normalized, factor * 4);

            if (!ApproachingWall)
            {
                var leanProjection = Vector3.Dot(_slideLeanVector, camera.transform.right);
                SetCameraRotation(leanProjection * 15, 6);
            }

            _dashTime -= factor;
        }
        if (_dashTime <= 0)
        {
            _dashTime = 0;
            _dashVector = Vector3.Lerp(_dashVector, new Vector3(), factor * dashDecay);
        }
        if (_dashCancelTempSpeed > 0)
        {
            var loss = factor * dashCancelTempSpeedDecay;
            velocity -= Flatten(velocity).normalized * loss;
            _dashCancelTempSpeed -= loss;
        }
        if (Game.PostProcessVolume != null && Game.PostProcessVolume.profile.TryGetSettings(out MotionBlur motion))
        {
            motion.enabled.value = IsDashing;
        }

        // Interpolation delta resets to 0 every fixed update tick, its used to measure the time since the last fixed update tick
        _motionInterpolationDelta = 0;

        var movement = (velocity + _dashVector) * Time.fixedDeltaTime;
        _previousPosition = transform.position;

        if (IsOnGround) _groundLevel--;
        if (IsOnWall) _wallLevel--;
        _isDownColliding = false;
        var iterations = 0;

        var hold = 0.1f;
        if (IsOnGround)
        {
            movement += Vector3.down * hold;
        }
        if (IsOnWall)
        {
            movement -= _wallNormal * hold;
        }

        while (movement.magnitude > 0f && iterations < 5)
        {
            iterations++;
            if (rigidbody.SweepTest(movement.normalized, out var hit, movement.magnitude, QueryTriggerInteraction.Ignore) && CanCollide(hit.collider))
            {
                transform.position += movement.normalized * hit.distance;
                movement -= movement.normalized * hit.distance;

                var movementProjection = Vector3.Dot(movement, -hit.normal);
                if (movementProjection > 0) movement += hit.normal * movementProjection;
                if (CanCollide(hit.collider)) ContactEvent(hit.normal, hit.collider);
            }
            else
            {
                if (Physics.Raycast(transform.position, movement.normalized, out var ray, movement.magnitude, 1, QueryTriggerInteraction.Ignore) && CanCollide(ray.collider))
                {
                    movement = movement.normalized * ray.distance;
                }
                transform.position += movement;
                movement = new Vector3();
            }
        }

        var overlap = Physics.OverlapBox(CurrentCollider.bounds.center, CurrentCollider.bounds.extents);

        foreach (var collider in overlap)
        {
            if (!CanCollide(collider)) continue;
            if (Physics.ComputePenetration(CurrentCollider, CurrentCollider.transform.position, CurrentCollider.transform.rotation, collider, collider.transform.position, collider.transform.rotation, out var direction, out var distance))
            {
                if (collider.isTrigger)
                {
                    OnTrigger(collider);
                    continue;
                }

                if (CanCollide(collider)) ContactEvent(direction, collider);

                transform.position += direction * distance;
            }
        }

        _previousSpeed = Flatten(velocity).magnitude;
    }

    private bool CanCollide(Collider collider, bool ignoreUninteractable = true)
    {
        if (collider.gameObject == gameObject) return false;
        if (collider.CompareTag("Player")) return false;
        if (collider.CompareTag("Target")) return false;
        if (!ignoreUninteractable && collider.CompareTag("Uninteractable")) return false;
        return true;
    }

    private void ContactCollider(Vector3 normal, Collider collider)
    {
        if (collider.GetComponent<KillCollider>() != null)
        {
            Game.RestartLevel();
        }

        var velocityProjection = Vector3.Dot(velocity, -normal);
        if (velocityProjection > 0)
        {
            var impulse = normal * velocityProjection;
            velocity += impulse;
        }

        var angle = Vector3.Angle(Vector3.up, normal);

        if (angle < groundAngle && !collider.CompareTag("Wall"))
        {
            IsOnGround = true;
        }

        if (!collider.CompareTag("Uninteractable") && Mathf.Abs(angle - 90) < wallAngleGive && !IsOnGround && jumpKitEnabled && collider.gameObject != _lastWall)
        {
            _wallNormal = Flatten(normal).normalized;
            IsOnWall = true;
            _currentWall = collider.gameObject;
        }
        if (angle < 90) _isDownColliding = true;
    }

    private void OnTrigger(Collider other)
    {
        // Rail Grab
        if (other.CompareTag("Rail") && (PlayerInput.tickCount - _railCooldownTimestamp > railCooldownTicks || other.transform.parent.gameObject != _lastRail))
        {
            _lastRail = other.transform.parent.gameObject;
            SetRail(other.gameObject.transform.parent.gameObject.GetComponent<Rail>());
        }

        if (other.CompareTag("Finish"))
        {
            EndTimer();
        }
    }

    public void Yoink(Vector3 target)
    {
        Gun.forwardChange += 2;

        var towardTarget = target - transform.position;

        //velocity = velocity.normalized * yoinkStartSpeed;

        var projection = Vector3.Dot(velocity, -towardTarget.normalized);
        if (projection > 0) velocity += towardTarget.normalized * projection;
        _yoinkTicks = 0;
        _yoinkStartSpeed = velocity.magnitude;

        IsBeingYoinked = true;
        _yoinkTarget = target;
        _yoinkDirection = towardTarget.normalized;
    }

    public void YoinkMove(float f)
    {
        var towardTarget = _yoinkTarget - transform.position;

        if (_yoinkTicks++ > yoinkMaxTicks || (Vector3.Dot(velocity.normalized, towardTarget.normalized) < 0 && towardTarget.magnitude < 10))
        {
            IsBeingYoinked = false;
            velocity = velocity.normalized * Mathf.Max(_yoinkStartSpeed, yoinkMinEndSpeed);
            //velocity = velocity.normalized * yoinkEndSpeed;
        }

        if (towardTarget.magnitude > 5)
        {
            _yoinkDirection = towardTarget.normalized;
        }

        velocity += towardTarget.normalized * f * yoinkAcceleration;
        velocity = Vector3.Lerp(velocity, _yoinkDirection * velocity.magnitude, f * 10);
    }

    public void Dash(Vector3 wishdir)
    {
        source.Play();

        if (velocity.magnitude < groundSpeed.speed) velocity = wishdir * groundSpeed.speed;

        var x1 = Flatten(velocity).magnitude;
        var x2 = Flatten(wishdir).magnitude;
        var y2 = wishdir.y;

        var y1 = x1 * y2 / x2;

        velocity = Flatten(wishdir).normalized * x1;

        var _dashUpSpeed = 30;
        if (Mathf.Abs(y1) > _dashUpSpeed)
        {
            y1 = _dashUpSpeed * Mathf.Sign(y1);

            velocity = Flatten(velocity).normalized * (y1 * x2 / y2);
        }

        velocity.y = y1;


        /*if (wishdir.y > 0.5f)
        {
            velocity.y = _dashUpSpeed * Mathf.Sign(wishdir.y);
        } else
        {
            velocity = wishdir * velocity.magnitude;

            if (Flatten(velocity).magnitude < movementSpeed)
            {
                var y = velocity.y;
                velocity = Flatten(wishdir).normalized * movementSpeed;
                velocity.y = y;
            }

        }*/
        _dashVector = dashSpeed * wishdir.normalized;
        _dashTimestamp = Environment.TickCount;

        //velocity += add;
        Gun.forwardChange += 2;
        _dashTime = dashTime;
    }

    public void StopDash()
    {
        if (IsDashing)
        {
            SetCameraRotation(0, 6);
            _dashTime = 0;
        }
    }

    public void CancelDash()
    {
        if (IsDashing)
        {
            StopDash();
            Accelerate(Flatten(velocity).normalized, dashCancelSpeed);
            velocity += Flatten(velocity).normalized * dashCancelTempSpeed;
            _dashCancelTempSpeed += dashCancelTempSpeed;
        }
    }

    public void SetRail(Rail rail)
    {
        if (IsOnRail) return;
        _currentRail = rail;
        source.PlayOneShot(railLand);
        railSound.Play();
        railSound.volume = 1;
        if (rail.railDirection == Rail.RailDirection.BACKWARD)
        {
            Game.I.lastCheckpoint = rail.smoothedPoints[rail.smoothedPoints.Length - 1];
            _railDirection = -1;
        } else if (rail.railDirection == Rail.RailDirection.FORWARD)
        {
            Game.I.lastCheckpoint = rail.smoothedPoints[0];
            _railDirection = 1;
        }
        if (GrappleHooked) DetachGrapple();
    }

    public void EndRail()
    {
        if (!IsOnRail) return;
        velocity.y += jumpHeight;
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

        var balance = leanVector + Vector3.up * (Gravity * 3);

        return balance.normalized;
    }

    public void RailMove(float f)
    {
        if (!IsOnRail) return;

        _currentCharge = ChargeType.DASH;

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

        var previousDirection = Flatten(velocity).normalized;
        var previousAngle = Mathf.Atan2(previousDirection.z, previousDirection.x);

        var railVector = -(current - next).normalized;

        if ((_railDirection == -1 && closeIndex == 0 || _railDirection == 1 && closeIndex == _currentRail.smoothedPoints.Length - 1) && Vector3.Dot(transform.position - current, railVector) > 0)
        {
            EndRail();
            return;
        }

        _railLeanVector = Vector3.Lerp(_railLeanVector, GetBalanceVector(closeIndex + _railDirection), f * 20);

        var correctionVector = -(transform.position - next).normalized;
        velocity = railSpeed * Vector3.Lerp(railVector, correctionVector, f * 20).normalized;

        if (velocity.y < 0) GravityTick(f);

        var totalAngle = Vector3.Angle(Vector3.up, _railLeanVector) / 2f;
        var projection = Vector3.Dot(_railLeanVector.normalized * totalAngle, -transform.right);
        SetCameraRotation(projection, 6);

        railSound.pitch = Mathf.Min(Mathf.Max(velocity.magnitude / 10, 1), 2);

        var newDirection = Flatten(velocity).normalized;
        var newAngle = Mathf.Atan2(newDirection.z, newDirection.x);
        YawFutureInterpolation += (Mathf.Rad2Deg * previousAngle) - (Mathf.Rad2Deg * newAngle);

        Jump();
    }

    public void AttachGrapple(Vector3 position)
    {
        source.PlayOneShot(grappleAttach);
        if (IsOnRail) EndRail();
        _grappleAttachPosition = position;
        GrappleHooked = true;
        grappleDuring.volume = 0.4f;
        grappleDuring.Play();
        _grappleTicks = 0;

        var list = new List<Vector3> { new Vector3(0, grappleYOffset, 0), position };

        grappleTether.positionCount = list.Count;
        grappleTether.SetPositions(list.ToArray());

        var towardPoint = (position - transform.position).normalized;
        var yankProjection = Vector3.Dot(velocity, towardPoint);
        if (yankProjection < 0) velocity -= towardPoint * yankProjection;
    }

    public void DetachGrapple()
    {
        if (GrappleHooked) GrappleHooked = false;
        if (grappleTether.enabled) grappleTether.enabled = false;
        SetCameraRotation(0, 2);
        grappleDuring.volume = 0;

        source.PlayOneShot(grappleRelease);
    }

    public void GrappleMove(float f)
    {
        var position = _grappleAttachPosition;

        if (!grappleTether.enabled) grappleTether.enabled = true;

        rollSound.volume = 0;

        var list = new List<Vector3> { new Vector3(0, grappleYOffset, 0), camera.transform.InverseTransformPoint(position) };

        grappleTether.positionCount = list.Count;
        grappleTether.SetPositions(list.ToArray());

        var towardPoint = (position - transform.position).normalized;
        var velocityProjection = Vector3.Dot(velocity, towardPoint);
        var tangentVector = (velocity + towardPoint * -velocityProjection).normalized;

        var swingProjection = Mathf.Max(0, Vector3.Dot(velocity, Vector3.Lerp(towardPoint, tangentVector, 0.5f)));

        var projection = Mathf.Sqrt(swingProjection) * Vector3.Dot(-transform.right, towardPoint) * 6;
        SetCameraRotation(projection, 2);

        grappleDuring.pitch = (_grappleTicks * 2f / grappleMaxTicks) + 1;

        if (velocityProjection < 0) velocity -= towardPoint * velocityProjection;

        if (velocity.magnitude < 0.05f) velocity += towardPoint * f * grappleSpeed.acceleration;
        var magnitude = velocity.magnitude;
        velocity += CrosshairDirection * grappleControlAcceleration * f;
        velocity = velocity.normalized * magnitude;

        var target = position + tangentVector * grappleDistance;
        var direction = Vector3.Lerp(velocity.normalized, (target - transform.position).normalized, grappleCorrectionAcceleration);
        velocity = velocity.magnitude * direction.normalized;

        Accelerate(direction, grappleSpeed, f);

        if (Vector3.Dot(towardPoint, CrosshairDirection) < 0 || _grappleTicks++ > grappleMaxTicks)
        {
            DetachGrapple();
        }
        Jump();
    }

    public void WallMove(float f)
    {
        _currentCharge = ChargeType.DOUBLE_JUMP;
        if (Jump()) return;

        if (_wallTickCount == jumpForgiveness)
        {
            source.PlayOneShot(groundLand);
        }

        if (_wallTickCount == 0)
        {
            _wallStamina = wallStamina;
        }
        _wallTickCount++;


        if (_wallTickCount >= jumpForgiveness)
        {
            rollSound.pitch = Mathf.Min(Mathf.Max(velocity.magnitude / 20, 1), 2);
            rollSound.volume = Mathf.Min(velocity.magnitude / 30, 1);

            var angle = Vector3.Dot(CrosshairDirection, _wallNormal);
            if (angle > 0.7f)
            {
                ApplyFriction(wallNeutralFriction * f);
            }
            else
            {
                var direction = Flatten(CrosshairDirection - angle * _wallNormal).normalized;
                if (_wallTickCount - jumpForgiveness < wallFrictionTicks) ApplyFriction(wallFriction * f, groundSpeed.speed);
                Accelerate(direction, wallSpeed, f);
            }
        }

        var normal = Flatten(_wallNormal);

        var projection = Vector3.Dot(CrosshairDirection, new Vector3(-normal.z, normal.y, normal.x));

        SetCameraRotation(wallLeanDegrees * -projection * (_wallStamina / wallStamina), 8);

        if (_wallStamina <= 0)
        {
            _wallStamina = 0;
            IsOnWall = false;
            Accelerate(normal, wallEndBoostSpeed);
        }
        _wallStamina -= f * _wallTickCount;

        source.pitch = 1;

        if (velocity.y < 0)
        {
            velocity.y = Mathf.Lerp(velocity.y, 0, f * wallCatchFriction);
        }
        else
        {
            GravityTick(f);
        }


        /*var direction = new Vector3(_wallNormal.z, 0, -_wallNormal.x);
        if (Mathf.Abs(Vector3.Dot(direction, Wishdir)) > 0.5)
        {
            if (Vector3.Angle(Wishdir, direction) < 90)
                Accelerate(direction, movementSpeed, wallAcceleration * f);
            else
                Accelerate(-direction, movementSpeed, wallAcceleration * f);
        }*/

        /*if (Flatten(velocity).magnitude < wallSpeed)
        {
            ApplyFriction(f * groundFriction);
        }
        else
        {
            if (_landFrictionTicks++ < landFrictionTicks) ApplyFriction(landFriction * f, movementSpeed);
        }*/

        Accelerate(-_wallNormal, new SpeedAccel(10, Gravity), f);
    }

    public void GravityTick(float f)
    {
        velocity.y -= Gravity * f;
    }

    public void GroundMove(float f)
    {
        _currentCharge = ChargeType.DASH;
        GravityTick(f);

        if (Jump()) return;

        _groundTickCount++;

        if (_groundTickCount == jumpForgiveness)
        {
            source.PlayOneShot(groundLand);
        }

        if (_groundTickCount >= jumpForgiveness)
        {
            rollSound.pitch = Mathf.Min(Mathf.Max(velocity.magnitude / 20, 1), 2);
            rollSound.volume = Mathf.Min(velocity.magnitude / 30, 1);

            if (IsSliding || IsDashing)
            {
                _slideLeanVector = Vector3.Lerp(_slideLeanVector, Flatten(velocity).normalized, f * 4);
                var leanProjection = Vector3.Dot(_slideLeanVector, camera.transform.right);
                SetCameraRotation(leanProjection * 15, 6);

                AirAccelerate(f, slideMovementScale);

                ApplyFriction(slideFriction * f, groundSpeed.speed);

                if (PlayerInput.GetAxisStrafeRight() == 0 && Vector3.Angle(Wishdir, velocity) < 90)
                {
                    var speed = Flatten(velocity).magnitude;
                    var y = velocity.y;
                    velocity += Wishdir * f * airStrafeAcceleration;
                    velocity = Flatten(velocity).normalized * speed;
                    velocity.y = y;
                }
            }
            else
            {
                _slideLeanVector = Flatten(velocity).normalized;
                GroundAccelerate(f);
            }
        }
        else
        {
            AirAccelerate(f, slideMovementScale);
        }
    }

    public void GroundAccelerate(float f, float frictionMod = 1f)
    {
        ApplyFriction(f * groundFriction * frictionMod, 0, groundSpeed.speed);
        if (Wishdir.magnitude > 0)
        {
            var speed = Flatten(velocity).magnitude;
            var y = velocity.y;

            velocity += Wishdir * f * groundSpeed.acceleration;

            if (Flatten(velocity).magnitude >= groundSpeed.speed && Vector3.Angle(Wishdir, velocity) < 90)
            {
                velocity = Flatten(velocity).normalized * Mathf.Max(speed, groundSpeed.speed);
                velocity.y = y;
            }
        }
    }

    public void AirMove(float f)
    {
        GravityTick(f);
        _slideLeanVector = Flatten(velocity).normalized;

        rollSound.volume = 0;

        if (!jumpKitEnabled)
        {
            AirAccelerate(f);
            return;
        }

        // Lean in
        var movement = velocity + _dashVector;
        var didHit = rigidbody.SweepTest(movement.normalized, out var hit, movement.magnitude * wallLeanPreTime, QueryTriggerInteraction.Ignore);

        var eatJump = false;
        var currentLean = 0f;

        if (didHit
            && Math.Abs(Vector3.Angle(Vector3.up, hit.normal) - 90) < wallAngleGive
            && CanCollide(hit.collider, false)
            && hit.collider.gameObject != _lastWall)
        {
            if (!ApproachingWall) ApproachingWall = true;

            currentLean = 1 - hit.distance / movement.magnitude / wallLeanPreTime;

            // Eat jump inputs if you are < jumpForgiveness away from the wall to not eat double jump
            if (wallLeanPreTime * (1 - currentLean) / Time.fixedDeltaTime < jumpForgiveness) eatJump = true;

            var curve = currentLean * (2 - currentLean);

            var normal = Flatten(hit.normal);
            var projection = Vector3.Dot(CrosshairDirection, new Vector3(-normal.z, normal.y, normal.x));
            SetCameraRotation(wallLeanDegrees * curve * -projection, 15);
            _cancelLeanTickCount = 0;

            var fromBottom = 1;
            var upCheck = transform.position + (velocity.normalized * hit.distance) + (-hit.normal * 0.55f) + (Vector3.down * 1);
            if (Physics.Raycast(upCheck + Vector3.down * fromBottom, Vector3.up, out var upHit, 2, 1, QueryTriggerInteraction.Ignore))
            {
                var vector = (transform.position + Vector3.down) - (upHit.point + Vector3.up * fromBottom);
                var p = Mathf.Atan2(vector.y, Flatten(vector).magnitude);
                if (p * Mathf.Rad2Deg < -45) p = Mathf.Deg2Rad * -45;
                var y = Mathf.Tan(-p) * Flatten(velocity).magnitude;
                velocity.y = y;
            }
        }
        else if (didHit && Vector3.Angle(Vector3.up, hit.normal) < groundAngle && CanCollide(hit.collider))
        {
            // Eat jump inputs if you are < jumpForgiveness away from the ground to not eat double jump
            if (hit.distance / movement.magnitude / Time.fixedDeltaTime < jumpForgiveness) eatJump = true;
        }

        var groundMod = 1 - Mathf.Min(Flatten(velocity).magnitude / (groundSpeed.speed - 1), 1);
        if (!_isDownColliding) GroundAccelerate(f * groundMod, 0);
        f *= 1 - groundMod;
        AirAccelerate(f, 1 - (currentLean * (2 - currentLean)));

        if (eatJump)
        {
            if (PlayerInput.SincePressed(PlayerInput.Jump) < jumpForgiveness)
            {
                PlayerInput.SimulateKeyPress(PlayerInput.Jump);
            }
        }
        else Jump();
    }

    public void AirAccelerate(float f, float accelMod = 1)
    {

        var accel = airStrafeAcceleration;

        if (_isDownColliding && !IsOnGround) accel *= surfAirAccelMultiplier;
        accel *= accelMod;

        var forward = transform.forward * PlayerInput.GetAxisStrafeForward();
        var forwardspeed = Vector3.Dot(velocity, forward);
        var forwardaddspeed = Mathf.Abs(airSpeed) - forwardspeed;
        if (forwardaddspeed > 0)
        {
            if (accel * f > forwardaddspeed)
                accel = forwardaddspeed / f;

            var addvector = accel * forward;
            var backspeed = Vector3.Dot(addvector, -Flatten(velocity).normalized);
            if (backspeed > backAirStrafeAcceleration)
            {
                var x1 = backspeed;
                var x2 = backAirStrafeAcceleration;
                var y1 = addvector.magnitude;
                var y2 = (x2 * y1) / x1;

                addvector = addvector.normalized * y2;
            }
            velocity += addvector * f;
        }

        var right = transform.right * PlayerInput.GetAxisStrafeRight();
        var rightspeed = Vector3.Dot(velocity, right);
        var rightaddspeed = Mathf.Abs(airSpeed) - rightspeed;
        if (rightaddspeed > 0)
        {
            if (accel * f > rightaddspeed)
                accel = rightaddspeed / f;

            var addvector = accel * right;
            var backspeed = Vector3.Dot(addvector, -Flatten(velocity).normalized);
            if (backspeed > backAirStrafeAcceleration)
            {
                var x1 = backspeed;
                var x2 = backAirStrafeAcceleration;
                var y1 = addvector.magnitude;
                var y2 = (x2 * y1) / x1;

                addvector = addvector.normalized * y2;
            }
            velocity += addvector * f;
        }
    }

    public float ApplyFriction(float f, float minimumSpeed = 0, float deceleration = 0)
    {
        var speed = Flatten(velocity).magnitude;
        if (speed < deceleration)
        {
            minimumSpeed = speed - deceleration;
        }
        var newspeed = Mathf.Lerp(speed, minimumSpeed, f);

        if (newspeed > speed) return 0;
        if (newspeed < 0)
        {
            newspeed = 0;
        }

        var y = velocity.y;
        velocity = Flatten(velocity).normalized * newspeed;
        velocity.y = y;

        return speed - newspeed;
    }

    public void Accelerate(Vector3 wishdir, SpeedAccel speed, float f = 1)
    {
        var currentspeed = Vector3.Dot(velocity, wishdir.normalized);
        var addspeed = Mathf.Abs(speed.speed) - currentspeed;

        if (addspeed <= 0)
            return;

        var accelspeed = Mathf.Lerp(currentspeed, speed.speed, speed.acceleration * f) -  currentspeed;
        //var accelspeed = Mathf.Abs(speed.acceleration * f);
        if (accelspeed > addspeed)
            accelspeed = addspeed;

        velocity.x += accelspeed * wishdir.x;
        velocity.y += accelspeed * wishdir.y;
        velocity.z += accelspeed * wishdir.z;
    }

    public bool Jump()
    {
        int sinceJump = PlayerInput.SincePressed(PlayerInput.Jump);
        if (sinceJump < jumpForgiveness)
        {
            if (GrappleHooked)
            {
                DetachGrapple();
                PlayerInput.ConsumeBuffer(PlayerInput.Jump);
                return true;
            }

            var wallJump = PlayerInput.tickCount - _wallTimestamp < coyoteTicks;

            var groundJump = PlayerInput.tickCount - _groundTimestamp < coyoteTicks;
            _groundTimestamp = -coyoteTicks;
            var railJump = PlayerInput.tickCount - _railTimestamp < coyoteTicks;
            _railTimestamp = -coyoteTicks;

            if (!groundJump && !railJump && !wallJump && _currentCharge == ChargeType.NONE) return false;

            if (!groundJump && !railJump && !wallJump)
            {
                if (_currentCharge == ChargeType.DASH)
                {
                    if (Environment.TickCount - _dashTimestamp > dashCooldown)
                    {
                        var wishdir = CrosshairDirection;
                        source.Play();

                        Dash(wishdir);
                    }
                    else
                    {
                        return false;
                    }
                }
                if (_currentCharge == ChargeType.DOUBLE_JUMP)
                {
                    velocity.y = Mathf.Max(jumpHeight, velocity.y);
                }
                _currentCharge = ChargeType.NONE;
                PlayerInput.ConsumeBuffer(PlayerInput.Jump);
                return true;
            }

            PlayerInput.ConsumeBuffer(PlayerInput.Jump);

            SetCameraRotation(0, cameraRotationCorrectSpeed);

            if (groundJump || wallJump)
            {
                source.PlayOneShot(jump);
            }

            rollSound.volume = 0;
            IsOnGround = false;

            if (railJump)
            {
                EndRail();
                return true;
            }

            if (wallJump)
            {
                IsOnWall = false;
                _wallTimestamp = -coyoteTicks;
                _lastWall = _currentWall;

                var y = velocity.y;
                var velocityDirection = velocity.normalized;
                var angle = Vector3.Dot(CrosshairDirection, _wallNormal);
                if (angle > 0.7f)
                {
                    velocityDirection = CrosshairDirection;
                }
                var direction = Flatten(velocityDirection + _wallNormal * wallJumpAngle).normalized;
                velocity = Mathf.Max(velocity.magnitude, wallSpeed.speed) * direction;
                Accelerate(Flatten(velocityDirection).normalized, wallJumpSpeed);

                velocity.y = y;
                velocity.y = Mathf.Max(jumpHeight, velocity.y);
                if (IsDashing)
                {
                    CancelDash();
                    var cancelForce = Mathf.Min(y * 2.2f, 80);
                    velocity.y = Mathf.Max(cancelForce, jumpHeight / 2);
                    source.PlayOneShot(wallKick);
                }
            }
            else
            {
                if (!groundJump)
                {
                    velocity.y = Mathf.Max(jumpHeight, velocity.y);
                }
                else
                {
                    var height = jumpHeight;
                    if (IsDashing)
                    {
                        source.PlayOneShot(wallKick);
                        CancelDash();

                        height /= 1.5f;

                        if (railJump) source.PlayOneShot(trick);
                    }
                    velocity.y += height;
                }
            }
            return true;
        }
        return false;
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}