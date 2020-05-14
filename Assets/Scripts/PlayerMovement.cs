using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    public new Camera camera;
    public MeshCollider standingCollider;
    public MeshCollider crouchingCollider;
    public Image crosshair;
    public GameObject abilityDot;
    public new Rigidbody rigidbody;

    public Vector3 cameraPosition;
    public Vector3 velocity;

    public bool jumpKitEnabled = true;

    public enum Ability
    {
        GRAPPLE,
        DASH
    }

    public float Yaw { get; set; }
    public float YawIncrease { get; set; }
    public float Pitch { get; set; }
    public float LookScale { get; set; }
    public Collider CurrentCollider { get; private set; }
    public float CameraRoll { get; set; }
    public Vector3 Wishdir { get; set; }
    public Vector3 CrosshairDirection { get; set; }
    public Vector3 InterpolatedPosition { get { return Vector3.Lerp(_previousPosition, transform.position, _motionInterpolationDelta / Time.fixedDeltaTime); } }
    public bool IsStrafing { get { return PlayerInput.GetAxisStrafeForward() == 0 && (Input.GetKey(PlayerInput.MoveLeft) || Input.GetKey(PlayerInput.MoveRight)); } }
    public bool IsSliding
    {
        get
        {
            if (!jumpKitEnabled) return false;
            if (Flatten(velocity).magnitude > movementSpeed + 1) return true;
            if (GroundLevel > 0 && Flatten(velocity).magnitude < _previousSpeed) return true;
            if (GroundLevel == 0 && Flatten(velocity).magnitude >= movementSpeed - 1) return true;
            if (WallLevel > 0) return true;
            if (IsOnRail) return true;
            if (GrappleHooked) return true;
            if (_isDownColliding && GroundLevel == 0) return true;
            return false;
        }
    }
    public float Gravity { get { return (velocity.y - Mathf.Lerp(velocity.y, -terminalVelocity, gravity)) * (1 - (_dashSpeed / dashSpeed)); } }

    public int GroundLevel { get; set; }
    private const float cameraRotationCorrectSpeed = 4f;
    private const float movementSpeed = 20f;
    private const float groundAngle = 45;
    private const float groundAcceleration = 200f;
    private const float groundFriction = 5f;
    private const float slideMovementScale = 2f;
    private const float slideFriction = 0.2f;
    private const int surfaceMaxLevel = 5;
    private const float stepHeight = 0.8f;
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
    public int WallLevel { get; set; }
    private const float wallCatchFriction = 10f;
    private const float wallAcceleration = 80f;
    private const float wallSpeed = 25f;
    private const float wallFriction = 10f;
    private const float wallJumpAngle = 0.3f;
    private const float wallAngleGive = 10f;
    private const float wallStamina = 100f;
    private const float wallEndBoostSpeed = 1f;
    private const float wallLeanDegrees = 20f;
    private const float wallLeanPreTime = 0.3f;
    private const float wallAirAccelRecovery = 0.3f;
    private const float wallJumpSpeed = 5f;
    private Vector3 _wallNormal;
    private float _wallRecovery;
    private int _wallTimestamp = -100000;
    private int _wallTickCount;
    private float _wallStamina;
    private int _cancelLeanTickCount;

    private const float airSpeed = 2f;
    private const float airSpeedCap = 30;
    private const float airFriction = 0.4f;
    private const float airStrafeAcceleration = 150f;
    private const float airMultiplier = 0.65f;

    private const float surfAirAccelMultiplier = 50f;

    public bool IsOnRail { get { return _currentRail != null; } }
    private const float railSpeed = 40f;
    private const int railCooldownTicks = 40;
    private int _railTimestamp = -100000;
    private int _railCooldownTimestamp = -100000;
    private int _railDirection;
    private Vector3 _railLeanVector;
    private GameObject _lastRail;
    private Rail _currentRail;

    public bool IsBeingYoinked { get; set; }
    private const float yoinkStartSpeed = 40f;
    private const float yoinkAcceleration = 190f;
    private const float yoinkEndSpeed = 40f;
    private const int yoinkMaxTicks = 60;
    private int _yoinkTicks;
    private Vector3 _yoinkTarget;
    private Vector3 _yoinkDirection;

    public bool IsDashing { get { return _dashSpeed > 4; } }
    public static bool DashAvailable { get; set; }
    private const float dashDownLimit = 0.8f;
    private const float dashUpLimit = 0.8f;
    private const float dashSpeed = 20;
    private const int dashCooldown = 300;
    private const float dashCancelSpeed = 3f;
    private const float dashFriction = 8;
    private const float dashTime = 0.5f;
    private const float dashEndUpStopAccel = 80f;
    private float _dashTime;
    private int _dashTimestamp = -100000;
    private float _dashSpeed;

    public float MaxSpeed { get { return 50f; } }
    private const float gravity = 0.5f;
    private const float terminalVelocity = 60;

    private const float jumpHeight = 20f;
    private const float maxJumpHeight = 16f;
    private const float minJumpDistance = 15f;
    private const int coyoteTicks = 20;
    private const int jumpForgiveness = 6;
    private const int wallJumpForgiveness = 2;
    private const float jumpGracePeriod = 0.5f;
    private const float jumpGraceBonusAccel = 0;
    private float _landThunkMax;

    public LineRenderer grappleTether;
    public float grappleYOffset;
    public bool GrappleHooked { get; set; }
    private const float grappleControlAcceleration = 40f;
    private const float grappleDistance = 25f;
    private const float grappleAcceleration = 40f;
    private const float grappleSpeed = 80f;
    private const float grappleCorrectionAcceleration = 0.02f;
    private const int grappleMaxTicks = 200;
    private int _grappleTicks;
    private Vector3 _grappleAttachPosition;
    public bool TimerRunning { get; private set; }

    public bool LevelCompleted { get; private set; }

    public Hitmarker hitmarker;

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

        if (Physics.Raycast(transform.position, Vector3.down, out var hit, 5f, 1, QueryTriggerInteraction.Ignore) && Vector3.Angle(hit.normal, Vector3.up) < groundAngle)
        {
            transform.position = hit.point + Vector3.up * 0.3f;
            GroundLevel = surfaceMaxLevel;
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

        Game.PostProcessVolume.profile.TryGetSettings(out Blur blur);
        blur.BlurIterations.value = 8;
        blur.enabled.value = true;
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
                if (!IsOnRail && WallLevel == 0)
                {
                    SetCameraRotation(0, 5);
                }
            }
        }

        crouchChange -= _crouchAmount;
        //if (!_isDownColliding && !IsGrounded) transform.position -= Vector3.up * crouchChange;

        position.y -= 0.6f * _crouchAmount;

        camera.transform.position = InterpolatedPosition + position;

        var targetFOV = velocity.magnitude + (100 - movementSpeed);
        targetFOV = Mathf.Max(targetFOV, 100);
        targetFOV = Mathf.Min(targetFOV, 120);

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

        if (PlayerInput.SincePressed(PlayerInput.SecondaryInteract) < jumpForgiveness && Time.timeScale > 0 && DashAvailable && Environment.TickCount - _dashTimestamp > dashCooldown)
        {
            var wishdir = CrosshairDirection;
            var wishY = wishdir.y;
            wishdir = Flatten(wishdir).normalized;
            source.Play();
            DashAvailable = false;

            wishdir.y = Mathf.Max(-Mathf.Abs(dashDownLimit), Mathf.Min(Mathf.Abs(dashUpLimit), wishY));
            PlayerInput.ConsumeBuffer(PlayerInput.SecondaryInteract);

            Dash(wishdir);
        }
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
        if (GroundLevel > 0) _groundTimestamp = PlayerInput.tickCount;
        if (WallLevel > 0) _wallTimestamp = PlayerInput.tickCount;
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
        else if (WallLevel > 0)
            WallMove(factor);
        else if (GroundLevel > 0)
            GroundMove(factor);
        else
            AirMove(factor);

        if (ApproachingWall)
        {
            _cancelLeanTickCount++;
        }
        if (_cancelLeanTickCount >= 5 && WallLevel == 0)
        {
            ApproachingWall = false;
            SetCameraRotation(0, cameraRotationCorrectSpeed);
        }

        if (WallLevel == 0) _wallTickCount = 0;
        if (GroundLevel == 0) _groundTickCount = 0;

        if (_dashTime > 0) _dashTime -= factor;
        if (_dashTime < 0) _dashTime = 0;
        if (_dashSpeed < 0) _dashSpeed = 0;
        if (_dashSpeed > 0 && _dashTime <= 0)
        {
            var speed = Flatten(velocity).magnitude;
            var y = velocity.y;

            var newspeed = Mathf.Lerp(speed, speed - _dashSpeed, factor * dashFriction);
            _dashSpeed -= speed - newspeed;
            velocity = Flatten(velocity).normalized * newspeed;
            //if (y > 0) y = Mathf.Max(0, y - factor * dashEndUpStopAccel);
            velocity.y = y;
        }

        // Interpolation delta resets to 0 every fixed update tick, its used to measure the time since the last fixed update tick
        _motionInterpolationDelta = 0;

        var movement = velocity * Time.fixedDeltaTime;
        _previousPosition = transform.position;

        if (GroundLevel > 0) GroundLevel--;
        if (WallLevel > 0) WallLevel--;
        _isDownColliding = false;
        var iterations = 0;

        var hold = 0.1f;
        if (GroundLevel > 0)
        {
            movement += Vector3.down * hold;
        }
        if (WallLevel > 0)
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

                /*var stepCheck = StepCheck(hit.normal, transform.position, hit.collider);
                if (stepCheck > 0)
                {
                    transform.position += Vector3.up * (stepHeight - stepCheck);
                }
                else
                {
                    var movementProjection = Vector3.Dot(movement, -hit.normal);
                    if (movementProjection > 0) movement += hit.normal * movementProjection;
                    OnCollision(hit.normal, hit.collider);
                }*/
                var movementProjection = Vector3.Dot(movement, -hit.normal);
                if (movementProjection > 0) movement += hit.normal * movementProjection;
                OnCollision(hit.normal, hit.collider);
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

                /*var stepCheck = StepCheck(direction, transform.position, collider);
                if (stepCheck > 0)
                {
                    direction = Vector3.up;
                    distance = stepHeight - stepCheck;
                }
                else
                {
                    OnCollision(direction, collider);
                }*/
                OnCollision(direction, collider);

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

    private float StepCheck(Vector3 normal, Vector3 origin, Collider collider = null)
    {
        var stepCheck = origin + (-Flatten(normal).normalized * 0.51f) + (Vector3.down * (1 - stepHeight));
        if (Flatten(normal).normalized.magnitude > 0.05f
            && Physics.Raycast(stepCheck, Vector3.down, out var rayHit, stepHeight, 1, QueryTriggerInteraction.Ignore)
            && rayHit.normal.y == 1
            && Vector3.Dot(Flatten(velocity), Flatten(normal)) < 0)
        {
            if (collider != null && rayHit.collider != collider)
            {
                return 0;
            }
            return rayHit.distance;
        }
        return 0;
    }

    private void OnCollision(Vector3 normal, Collider collider)
    {
        if (!CanCollide(collider)) return;
        if (collider.CompareTag("Instant Kill Block"))
        {
            Game.RestartLevel();
        }
        if (collider.CompareTag("Kill Block") && GroundLevel > 0)
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

        if (angle < groundAngle)
        {
            GroundLevel = surfaceMaxLevel;
        }

        if (!collider.CompareTag("Uninteractable") && Mathf.Abs(angle - 90) < wallAngleGive && GroundLevel == 0 && jumpKitEnabled)
        {
            _wallNormal = Flatten(normal).normalized;
            WallLevel = surfaceMaxLevel;
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

        velocity = velocity.normalized * yoinkStartSpeed;

        var projection = Vector3.Dot(velocity, -towardTarget.normalized);
        if (projection > 0) velocity += towardTarget.normalized * projection;
        _yoinkTicks = 0;

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
            velocity = velocity.normalized * yoinkEndSpeed;
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
        //velocity.y = 0;
        var flat = Flatten(wishdir).normalized;
        velocity = flat * Flatten(velocity).magnitude;
        if (velocity.magnitude < movementSpeed) velocity = flat * movementSpeed;
        _dashTimestamp = Environment.TickCount;

        var x = Flatten(velocity).magnitude;
        var t = Mathf.Atan2(wishdir.y, Flatten(wishdir).magnitude);

        var y = Mathf.Tan(t) * x;

        velocity.y = y;

        var add = dashSpeed * wishdir;
        velocity += add;
        _dashSpeed += add.magnitude;
        Gun.forwardChange += 2;
        _dashTime = dashTime;
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

        var balance = leanVector + Vector3.up * (Gravity * 3);

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

        //velocity = velocity.normalized * grappleStartSpeed;

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
        //velocity = velocity.normalized * grappleEndSpeed;

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

        if (velocity.magnitude < 0.05f) velocity += towardPoint * f * grappleAcceleration;
        var magnitude = velocity.magnitude;
        velocity += CrosshairDirection * grappleControlAcceleration * f;
        velocity = velocity.normalized * magnitude;

        var target = position + tangentVector * grappleDistance;
        var direction = Vector3.Lerp(velocity.normalized, (target - transform.position).normalized, grappleCorrectionAcceleration);
        velocity = velocity.magnitude * direction.normalized;

        Accelerate(direction, grappleSpeed, swingProjection * f);

        if (Vector3.Dot(towardPoint, CrosshairDirection) < 0 || _grappleTicks++ > grappleMaxTicks)
        {
            DetachGrapple();
        }
        Jump();
    }

    public void WallMove(float f)
    {
        if (Jump()) return;

        if (_wallTickCount == wallJumpForgiveness)
        {
            source.PlayOneShot(groundLand);
        }

        if (_wallTickCount == 0)
        {
            _wallStamina = wallStamina;
        }
        _wallTickCount++;


        if (_wallTickCount >= wallJumpForgiveness)
        {
            rollSound.pitch = Mathf.Min(Mathf.Max(velocity.magnitude / 20, 1), 2);
            rollSound.volume = Mathf.Min(velocity.magnitude / 30, 1);

            ApplyFriction(wallFriction * f, movementSpeed);
            Accelerate(Flatten(velocity).normalized, wallSpeed, wallAcceleration * f);
        }

        var normal = Flatten(_wallNormal);

        var projection = Vector3.Dot(CrosshairDirection, new Vector3(-normal.z, normal.y, normal.x));

        SetCameraRotation(wallLeanDegrees * -projection * (_wallStamina / wallStamina), 8);

        if (_wallStamina <= 0)
        {
            _wallStamina = 0;
            WallLevel = 0;
            Accelerate(normal, wallEndBoostSpeed, wallEndBoostSpeed);
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

        Accelerate(-_wallNormal, 10, Gravity * f);
    }

    public void GravityTick(float f)
    {
        //if (_sinceJumpCounter < 25 && Input.GetKey(PlayerInput.Jump)) velocity.y = jumpHeight + Mathf.Max(0, _sinceJumpCounter - 5) / 3f;
        velocity.y -= Gravity * f;
        //Accelerate(Vector3.down, terminalVelocity, Gravity * f);
    }

    public void GroundMove(float f)
    {
        DashAvailable = true;
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
        }

        if (IsSliding)
        {
            _slideLeanVector = Vector3.Lerp(_slideLeanVector, Flatten(velocity).normalized, f * 4);
            var leanProjection = Vector3.Dot(_slideLeanVector, camera.transform.right);
            SetCameraRotation(leanProjection * 15, 6);

            AirAccelerate(f, slideMovementScale);

            ApplyFriction(slideFriction * f, movementSpeed);
        }
        else
        {
            _slideLeanVector = Flatten(velocity).normalized;
            GroundAccelerate(f);
        }
        //_slideLeanVector = Vector3.Lerp(_slideLeanVector, Flatten(velocity).normalized, f * 4);
        //var leanProjection = Vector3.Dot(_slideLeanVector, camera.transform.right);
        //SetCameraRotation(leanProjection * 15, 6);

        //AirAccelerate(f, slideMovementScale);

        //ApplyFriction(slideFriction * f, movementSpeed);
        /*if (CameraPositionThunkTarget > CameraPositionThunk && CameraPositionThunk > 0.05f)
        {
            _slideLeanVector = Vector3.Lerp(_slideLeanVector, Flatten(velocity).normalized, f * 4);
            var leanProjection = Vector3.Dot(_slideLeanVector, camera.transform.right);
            SetCameraRotation(leanProjection * 15, 6);

            //if (!IsDashing) ApplyFriction(wallFriction * f, movementSpeed);
            AirAccelerate(f, slideMovementScale);
            _landThunkMax = CameraPositionThunk;
        } else
        {
            if (_landThunkMax == 0) _landThunkMax = 0.05f;
            var factor = Mathf.Max(CameraPositionThunk, 0) / _landThunkMax;
            _slideLeanVector = Flatten(velocity).normalized;
            AirAccelerate(f * factor, slideMovementScale);
            GroundAccelerate(f * (1 - factor));
            SetCameraRotation(0, 1);
        }*/
    }

    public void GroundAccelerate(float f, float frictionMod = 1f)
    {
        ApplyFriction(f * groundFriction * frictionMod, 0, movementSpeed);
        if (Wishdir.magnitude > 0)
        {
            var speed = Flatten(velocity).magnitude;
            var y = velocity.y;

            velocity += Wishdir * f * groundAcceleration;

            if (Flatten(velocity).magnitude >= movementSpeed && Vector3.Angle(Wishdir, velocity) < 90)
            {
                velocity = Flatten(velocity).normalized * Mathf.Max(speed, movementSpeed);
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
        //ApplyFriction(f * airFriction, airSpeedCap);

        // Lean in
        var didHit = rigidbody.SweepTest(velocity.normalized, out var hit, velocity.magnitude * wallLeanPreTime, QueryTriggerInteraction.Ignore);

        //var eatJump = false;
        var currentLean = 0f;

        if (didHit
            && Math.Abs(Vector3.Angle(Vector3.up, hit.normal) - 90) < wallAngleGive
            //&& StepCheck(hit.normal, transform.position + (velocity.normalized * hit.distance)) == 0
            && CanCollide(hit.collider, false))
        {
            if (!ApproachingWall) ApproachingWall = true;

            currentLean = 1 - hit.distance / velocity.magnitude / wallLeanPreTime;

            // Eat jump inputs if you are < jumpForgiveness away from the wall to not eat double jump
            //if (wallLeanPreTime * (1 - currentLean) / Time.fixedDeltaTime < jumpForgiveness) eatJump = true;

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

            //if (velocity.y < 0) velocity.y += f * 90f;
        }
        /*else if (didHit && Vector3.Angle(Vector3.up, hit.normal) < groundAngle && CanCollide(hit.collider))
        {
            // Eat jump inputs if you are < jumpForgiveness away from the ground to not eat double jump
            if (hit.distance / velocity.magnitude / Time.fixedDeltaTime < jumpForgiveness) eatJump = true;
        }*/

        var groundMod = 1 - Mathf.Min(Flatten(velocity).magnitude / (movementSpeed - 1), 1);
        if (!_isDownColliding) GroundAccelerate(f * groundMod * airMultiplier, 0);
        f *= 1 - groundMod;
        AirAccelerate(f, 1 - currentLean);

        /*if (eatJump)
        {
            if (PlayerInput.SincePressed(PlayerInput.Jump) < jumpForgiveness)
            {
                PlayerInput.SimulateKeyPress(PlayerInput.Jump);
            }
        }
        else Jump();*/
        Jump();
    }

    public void AirAccelerate(float f, float accelMod = 1)
    {
        //var groundMod = 1 - Mathf.Min(Flatten(velocity).magnitude / (movementSpeed - 1), 1);
        //if (!_isDownColliding) GroundAccelerate(f * groundMod * airLowSpeedGroundMultiplier, 0);
        //f *= 1 - groundMod;

        /*if (groundMod == 0)
        {
            var speed = Flatten(velocity).magnitude;
            var magnitude = velocity.magnitude;

            var push = Mathf.Min(Mathf.Max(CrosshairDirection.y, -0.5f), 0) * 55f;
            Debug.Log(push);

            velocity += Vector3.up * push * f;
            velocity = velocity.normalized * magnitude;

            if (Flatten(velocity).magnitude > speed && velocity.y < 0)
            {
                velocity = velocity.normalized * (magnitude * speed / Flatten(velocity).magnitude);
            }
            /*var speed = Flatten(velocity).magnitude;
            var magnitude = velocity.magnitude;

            var lookT = Mathf.Atan2(CrosshairDirection.y, Flatten(CrosshairDirection).magnitude);
            var velT = Mathf.Atan2(velocity.normalized.y, Flatten(velocity.normalized).magnitude);

            if (lookT > velT)
            {
                var truncate = slowFallLimitDegrees * Mathf.Deg2Rad;
                if (lookT > velT + truncate) lookT = velT + truncate;

                var x = Mathf.Cos(lookT);
                var y = Mathf.Sin(lookT);

                var push = Flatten(velocity).normalized * x;
                push.y = y;

                velocity += push * slowFallForce * f;
                velocity = velocity.normalized * magnitude;

                if (Flatten(velocity).magnitude > speed && velocity.y < 0)
                {
                    velocity = velocity.normalized * (magnitude * speed / Flatten(velocity).magnitude);
                }
            }*/
        //}

        //var ticksPerSecond = 1 / Time.fixedDeltaTime;
        //var sinceJump = 1 - Mathf.Min(_sinceJumpCounter / (ticksPerSecond * jumpGracePeriod), 1);

        var accel = airStrafeAcceleration;

        if (_isDownColliding && GroundLevel == 0) accel *= surfAirAccelMultiplier;
        accel *= accelMod;
        accel *= 1 - _wallRecovery / wallAirAccelRecovery;
        if (_wallRecovery > 0) _wallRecovery -= f;
        if (_wallRecovery < 0) _wallRecovery = 0;

        var currentspeed = Vector3.Dot(velocity, Wishdir);
        var addspeed = Mathf.Abs(airSpeed) - currentspeed;

        if (addspeed > 0)
        {
            if (accel * f > addspeed)
                accel = addspeed / f;

            var addvector = accel * Wishdir;
            velocity += addvector * f;
        }

        if (!IsStrafing && Vector3.Angle(Wishdir, velocity) < 90)
        {
            var speed = Flatten(velocity).magnitude;
            var y = velocity.y;
            velocity += Wishdir * f * accel;
            velocity = Flatten(velocity).normalized * speed;
            velocity.y = y;
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

    public void Accelerate(Vector3 wishdir, float wishspeed, float accel)
    {
        var currentspeed = Vector3.Dot(velocity, wishdir.normalized);
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

            if (!groundJump && !railJump && !wallJump) return false;
            if (wallJump && sinceJump > wallJumpForgiveness) return false;

            PlayerInput.ConsumeBuffer(PlayerInput.Jump);

            SetCameraRotation(0, cameraRotationCorrectSpeed);

            if (groundJump || wallJump)
            {
                source.PlayOneShot(jump);
            }

            rollSound.volume = 0;
            GroundLevel = 0;

            if (IsOnRail) EndRail();

            if (wallJump)
            {
                _wallRecovery = wallAirAccelRecovery;
                WallLevel = 0;
                _wallTimestamp = -coyoteTicks;

                var y = velocity.y;
                var direction = Flatten(velocity.normalized + _wallNormal * wallJumpAngle).normalized;
                velocity = Flatten(velocity).magnitude * direction;
                velocity += Flatten(velocity).normalized * wallJumpSpeed;
                velocity.y = y;
                if (IsDashing)
                {
                    velocity = Flatten(velocity).normalized * (Flatten(velocity).magnitude - _dashSpeed);
                    _dashSpeed = 0;
                    velocity.y = y * 1.65f;
                    if (velocity.y > jumpHeight)
                    source.PlayOneShot(wallKick);
                }
            }

            var height = jumpHeight;

            if (IsDashing)
            {
                velocity = Flatten(velocity).normalized * (Flatten(velocity).magnitude - dashCancelSpeed);
                _dashSpeed -= dashCancelSpeed;

                velocity += Flatten(velocity).normalized * dashCancelSpeed;
                source.PlayOneShot(wallKick);

                if (!wallJump) height /= 1.5f;

                if (railJump) source.PlayOneShot(trick);
            }

            if (railJump) velocity.y += height; else velocity.y = Mathf.Max(height, velocity.y);
            return true;
        }
        return false;
    }

    private bool FeetCast(float time, out RaycastHit hit)
    {
        var origin = transform.position - Vector3.up;
        var ticks = Mathf.RoundToInt(time * 100);
        var simulatedVelocity = velocity;
        for (int i = 0; i < ticks; i++)
        {
            var to = origin + simulatedVelocity * Time.fixedDeltaTime;
            simulatedVelocity += Vector3.down * gravity * Time.fixedDeltaTime;
            if (Physics.Linecast(origin, to, out var lineHit, 1, QueryTriggerInteraction.Ignore) && CanCollide(lineHit.collider))
            {
                hit = lineHit;
                return true;
            }
            origin = to;
        }
        hit = new RaycastHit();
        return false;
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}