using System;
using System.CodeDom;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{

    public delegate void PlayerContact(Vector3 normal, Collider collider);
    public event PlayerContact ContactEvent;

    public delegate void Jump(ref JumpEvent jumpEvent);
    public event Jump PlayerJumpEvent;

    public struct JumpEvent
    {
        public bool cancelled;
        public float jumpHeight;
        public GameObject currentGround;
        public JumpType type;
    }

    public new Camera camera;
    public MeshCollider standingCollider;
    public MeshCollider crouchingCollider;
    public GameObject abilityDot;
    public new Rigidbody rigidbody;

    public Text speedText;

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
    public float PitchFutureInterpolation { get; set; }
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
            if (Flatten(velocity).magnitude > WSPEED + 1) return true;
            if (!IsOnGround && Flatten(velocity).magnitude < _previousSpeed) return true;
            if (IsOnGround && Flatten(velocity).magnitude >= WSPEED - 1) return true;
            if (IsOnWall) return true;
            if (IsOnRail) return true;
            if (GrappleHooked) return true;
            return false;
        }
    }
    public float Gravity { get { return (velocity.y - Mathf.Lerp(velocity.y, -TERMINAL_VELOCITY, GRAVITY)) * (IsDashing ? 0 : 1); } }

    // Surfaces have a "level" to make them a little more sticky than being able to come off them in 1 tick.
    // This is to prevent repeated landings on surfaces.
    public bool IsOnWall { get { return _wallLevel > 0; } set { if (value) _wallLevel = SURFACE_MAX_LEVEL; else _wallLevel = 0; } }
    private int _wallLevel;
    public bool IsOnGround { get { return _groundLevel > 0; } set { if (value) _groundLevel = SURFACE_MAX_LEVEL; else _groundLevel = 0; } }
    private int _groundLevel;
    public const int SURFACE_MAX_LEVEL = 5;

    public const float CAMERA_ROLL_CORRECT_SPEED = 4f;
    public const float GROUND_ACCELERATION = 200;
    public const float GROUND_ANGLE = 45;
    public const float GROUND_FRICTION = 5f;
    public const float SLIDE_MOVEMENT_SCALE = 2f;
    private int _groundTickCount;
    private int _groundTimestamp = -100000;
    private Vector3 _previousPosition;
    private float _previousSpeed;
    private float _crouchAmount;
    private Vector3 _slideLeanVector;
    private float _motionInterpolationDelta;
    private float _crosshairRotation;
    private GameObject _currentGround;

    private float _cameraRotation;
    private float _cameraRotationSpeed;
    public void SetCameraRotation(float value, float speed)
    {
        _cameraRotation = value;
        _cameraRotationSpeed = speed;
    }

    public const float WSPEED = 20;
    public const float FLOWSPEED = 40;
    public const float FLOW_DECAY = 5f;

    public bool ApproachingWall { get; set; }
    public const float WALL_CATCH_FRICTION = 10f;
    public const float WALL_ACCELERATION = 10;
    public const float WALL_FRICTION = 5f;
    public const float WALL_NEUTRAL_FRICTION = 1f;
    public const int WALL_FRICTION_TICKS = 5;
    public const float WALL_JUMP_ANGLE = 0.3f;
    public const float WALL_VERTICAL_ANGLE_GIVE = 10f;
    public const float WALL_AIR_ACCEL_RECOVERY = 0.3f;
    public const float WALL_UP_CANCEL_SPEED = 80;
    public const float WALL_UP_CANCEL_ACCELERATION = 2.2f;
    public const float WALL_STAMINA = 200f;
    public const float WALL_END_BOOST_SPEED = 1;
    public const float WALL_NEUTRAL_DOT = 0.9f;
    public const float WALL_LEAN_DEGREES = 20f;
    public const float WALL_LEAN_PREDICTION_TIME = 0.3f;
    public const float WALL_JUMP_FLOW_ACCEL = 0.4f;
    public const float WALL_KICK_SPEED = 10;
    public const float WALL_KICK_FADEOFF = 3;
    private Vector3 _wallNormal;
    private int _wallTimestamp = -100000;
    private int _wallTickCount;
    private bool _jumpIsBuffered;
    private float _wallStamina;
    private int _cancelLeanTickCount;
    private GameObject _lastWall;
    private GameObject _currentWall;
    private Vector3 _lastWallNormal;
    private float _wallRecovery;

    private const float AIR_SPEED = 4;
    private const float AIR_ACCELERATION = 100;
    private const float BACKWARDS_AIR_ACCEL_CAP = 80f;

    public bool IsOnRail { get { return _currentRail != null; } }
    public const int RAIL_COOLDOWN_TICKS = 40;
    public const float RAIL_SPEED = 80f;
    private int _railCooldownTimestamp = -100000;
    private int _railDirection;
    private int _railTickCount;
    private Vector3 _railLeanVector;
    private GameObject _lastRail;
    private Rail _currentRail;

    public enum ChargeType
    {
        NONE,
        DASH,
        DOUBLE_JUMP
    }

    private ChargeType _currentCharge = ChargeType.NONE;

    public bool IsDashing { get { return _dashVector.magnitude > 0.05f; } }
    public const float DASH_SPEED = 25;
    public const float DASH_THRESHOLD = -15;
    //public const int DASH_COOLDOWN = 500;
    public const float DASH_CANCEL_FLOW_ACCELERATION = 0.2f;
    public const float DASH_CANCEL_TEMP_SPEED = 20;
    public const float DASH_CANCEL_TEMP_SPEED_DECAY = 20;
    public const float DASH_CANCEL_OVERFLOW_SPEED = 6;
    public const float DASH_TIME = 0.35f;
    public const float DASH_DECAY = 35f;
    private float _dashTime;
    private float _dashCancelTempSpeed;
    private Vector3 _dashVector;

    public const float GRAVITY = 0.5f;
    public const float TERMINAL_VELOCITY = 60;

    public const float JUMP_HEIGHT = 15f;
    public const int COYOTE_TICKS = 20;
    public const int JUMP_FORGIVENESS_TICKS = 6;

    public LineRenderer grappleTether;
    public bool GrappleHooked { get; set; }
    public const float GRAPPLE_Y_OFFSET = -1.2f;
    public const float GRAPPLE_FORWARD_OFFSET = 0.5f;
    public const float GRAPPLE_CONTROL_ACCELERATION = 400f;
    public const float GRAPPLE_DISTANCE = 25f;
    public const float GRAPPLE_SPEED = 80;
    public const float GRAPPLE_ACCELERATION = 1;
    public const float GRAPPLE_CORRECTION_ACCELERATION = 0.02f;
    public const int GRAPPLE_MAX_TICKS = 200;
    private int _grappleTicks;
    private Vector3 _grappleAttachPosition;
    public bool TimerRunning { get; private set; }

    public bool LevelCompleted { get; private set; }

    public float CurrentTime { get; set; }

    private bool _wishTimerStart;

    /* Audio */
    public AudioSource source;
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

        var positionOverride = GameObject.Find("PlayerStartPositionOverride");
        if (positionOverride != null)
        {
            transform.position = positionOverride.transform.position;
        }

        if (Game.I.lastCheckpoint.sqrMagnitude > 0.05f)
        {
            transform.position = Game.I.lastCheckpoint;
            Yaw = Game.I.checkpointYaw;
        }

        if (Physics.Raycast(transform.position, Vector3.down, out var hit, 5f, 1, QueryTriggerInteraction.Ignore) && Vector3.Angle(hit.normal, Vector3.up) < GROUND_ANGLE)
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

            var yawinterpolation = Mathf.Lerp(Yaw, Yaw + YawFutureInterpolation, Time.deltaTime * 10) - Yaw;
            Yaw += yawinterpolation;
            YawFutureInterpolation -= yawinterpolation;

            Pitch -= Input.GetAxis("Mouse Y") * (Game.Sensitivity / 10) * LookScale;
            Pitch += Input.GetAxis("Joy 1 Y 2") * Game.Sensitivity * LookScale;

            var pitchinterpolation = Mathf.Lerp(Pitch, Pitch + PitchFutureInterpolation, Time.deltaTime * 10) - Pitch;
            Pitch += pitchinterpolation;
            PitchFutureInterpolation -= pitchinterpolation;

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

        var targetFOV = velocity.magnitude + (100 - WSPEED);
        targetFOV = Mathf.Max(targetFOV, 100);
        targetFOV = Mathf.Min(targetFOV, 120);

        camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, targetFOV, Time.deltaTime * 5);

        if (Mathf.Atan2(CrosshairDirection.y, Flatten(CrosshairDirection).magnitude) * Mathf.Rad2Deg > DASH_THRESHOLD)
        {
            _crosshairRotation = Mathf.Lerp(_crosshairRotation, 0, Time.deltaTime * 20);
        }
        else
        {
            _crosshairRotation = Mathf.Lerp(_crosshairRotation, 45, Time.deltaTime * 20);
        }
        Game.Canvas.crosshair.transform.rotation = Quaternion.Euler(new Vector3(0, 0, _crosshairRotation));

        if (Input.GetKeyDown(PlayerInput.PrimaryInteract) && !IsDashing && !IsOnRail)
        {
            var wishdir = CrosshairDirection;

            if (Mathf.Atan2(CrosshairDirection.y, Flatten(CrosshairDirection).magnitude) * Mathf.Rad2Deg > DASH_THRESHOLD)
            {
                var x = Mathf.Cos(Mathf.Deg2Rad * DASH_THRESHOLD);
                var y = Mathf.Sin(Mathf.Deg2Rad * DASH_THRESHOLD);

                wishdir = Flatten(wishdir).normalized * x;
                wishdir.y = y;
            }

            source.Play();
            Dash(wishdir);
        }

        if (Input.GetKeyDown(PlayerInput.SecondaryInteract))
        {
            if (Physics.Raycast(camera.transform.position, CrosshairDirection, out var hit, 100, 1, QueryTriggerInteraction.Ignore))
            {
                var target = hit.collider.gameObject.GetComponent<Target>();
                if (target != null)
                {
                    target.Explode(hit);
                }
            }
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
        if (IsOnGround) _groundTimestamp = PlayerInput.tickCount;
        if (IsOnWall) _wallTimestamp = PlayerInput.tickCount;
        if (IsOnRail) _railCooldownTimestamp = PlayerInput.tickCount;

        // Movement happens here
        var factor = Time.fixedDeltaTime;
        if (GrappleHooked)
            GrappleMove(factor);
        else if (IsOnRail)
            RailMove(factor);
        else if (IsOnWall)
            WallMove(factor);
        else if (IsOnGround)
            GroundMove(factor);
        else
            AirMove(factor);

        if (Flatten(velocity).magnitude - _dashCancelTempSpeed > FLOWSPEED)
        {
            var decay = Mathf.Min(FLOW_DECAY * factor, Flatten(velocity).magnitude - FLOWSPEED);
            velocity -= Flatten(velocity).normalized * decay;
        }

        if (ApproachingWall)
        {
            _cancelLeanTickCount++;
            if (_cancelLeanTickCount >= 5 && !IsOnWall)
            {
                ApproachingWall = false;
                SetCameraRotation(0, CAMERA_ROLL_CORRECT_SPEED);
            }
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
            _dashVector = Vector3.Lerp(_dashVector, new Vector3(), factor * DASH_DECAY);
        }
        if (_dashCancelTempSpeed > 0)
        {
            var loss = factor * DASH_CANCEL_TEMP_SPEED_DECAY;
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
        var measurement = (int)(_previousSpeed - _dashCancelTempSpeed - FLOWSPEED);
        if (measurement > 0)
        {
            speedText.text = "" + measurement;
        }
        else
        {
            speedText.text = "";
        }
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

        if (angle < GROUND_ANGLE && !collider.CompareTag("Wall"))
        {
            IsOnGround = true;
            _currentGround = collider.gameObject;
        }

        if (!collider.CompareTag("Uninteractable")
            && Mathf.Abs(angle - 90) < WALL_VERTICAL_ANGLE_GIVE
            && !IsOnGround && jumpKitEnabled
            && (collider.gameObject != _lastWall || Vector3.Dot(Flatten(normal).normalized, _lastWallNormal) < 0.7))
        {
            if (IsOnWall && Vector3.Angle(_wallNormal, Flatten(normal).normalized) > 10)
            {
                IsOnWall = false;
            }
            else
            {
                IsOnWall = true;
            }
            _wallNormal = Flatten(normal).normalized;
            _currentWall = collider.gameObject;
        }
    }

    private void OnTrigger(Collider other)
    {
        // Rail Grab
        if (other.CompareTag("Rail") && (PlayerInput.tickCount - _railCooldownTimestamp > RAIL_COOLDOWN_TICKS || other.transform.parent.gameObject != _lastRail))
        {
            _lastRail = other.transform.parent.gameObject;
            SetRail(other.gameObject.transform.parent.gameObject.GetComponent<Rail>());
        }

        if (other.CompareTag("Finish"))
        {
            EndTimer();
        }
    }

    public void Dash(Vector3 wishdir)
    {
        source.Play();

        if (velocity.magnitude < WSPEED) velocity = wishdir * WSPEED;

        var x1 = Flatten(velocity).magnitude;
        var x2 = Flatten(wishdir).magnitude;
        var y2 = wishdir.y;

        var y1 = x1 * y2 / x2;

        velocity = Flatten(wishdir).normalized * x1;

        var _dashUpSpeed = 30;
        if (Mathf.Abs(y1) > _dashUpSpeed)
        {
            y1 = _dashUpSpeed * Mathf.Sign(y1);

            if (Vector3.Angle(wishdir, Vector3.up) < 22.5)
            {
                velocity = Flatten(velocity).normalized * (y1 * x2 / y2);
            }
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
        _dashVector = DASH_SPEED * wishdir.normalized;

        //velocity += add;
        Gun.forwardChange += 2;
        _dashTime = DASH_TIME;
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
            Accelerate(Flatten(velocity).normalized, FLOWSPEED, DASH_CANCEL_FLOW_ACCELERATION);
            var mod = Mathf.Pow((DASH_TIME - _dashTime) / DASH_TIME, 2) * DASH_CANCEL_OVERFLOW_SPEED;
            var gain = DASH_CANCEL_TEMP_SPEED - _dashCancelTempSpeed;
            velocity += Flatten(velocity).normalized * (gain + mod);
            _dashCancelTempSpeed += gain;

            StopDash();
        }
    }

    public void SetRail(Rail rail)
    {
        if (IsOnRail) return;
        _currentRail = rail;
        source.PlayOneShot(railLand);
        railSound.Play();
        _railTickCount = 0;
        railSound.volume = 1;
        if (IsDashing) StopDash();
        if (rail.railDirection == Rail.RailDirection.BACKWARD)
        {
            Game.I.lastCheckpoint = rail.smoothedPoints[rail.smoothedPoints.Length - 1];
            var direction = rail.smoothedPoints[rail.smoothedPoints.Length - 2] - Game.I.lastCheckpoint;
            var angle = Mathf.Atan2(direction.x, direction.z);
            Game.I.checkpointYaw = Mathf.Rad2Deg * angle;
            _railDirection = -1;
        }
        else if (rail.railDirection == Rail.RailDirection.FORWARD)
        {
            Game.I.lastCheckpoint = rail.smoothedPoints[0];
            var direction = rail.smoothedPoints[1] - Game.I.lastCheckpoint;
            var angle = Mathf.Atan2(direction.x, direction.z);
            Game.I.checkpointYaw = Mathf.Rad2Deg * angle;
            _railDirection = 1;
        }
        if (GrappleHooked) DetachGrapple();
    }

    public void EndRail()
    {
        if (!IsOnRail) return;
        velocity.y += JUMP_HEIGHT;
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
        velocity = RAIL_SPEED * Vector3.Lerp(railVector, correctionVector, f * 20).normalized;

        if (velocity.y < 0) GravityTick(f);

        var totalAngle = Vector3.Angle(Vector3.up, _railLeanVector) / 2f;
        var projection = Vector3.Dot(_railLeanVector.normalized * totalAngle, -transform.right);
        SetCameraRotation(projection, 6);

        railSound.pitch = Mathf.Min(Mathf.Max(velocity.magnitude / 10, 1), 2);

        if (_railTickCount > 0)
        {
            var newDirection = Flatten(velocity).normalized;
            var newAngle = Mathf.Atan2(newDirection.z, newDirection.x);
            var angleChange = (Mathf.Rad2Deg * previousAngle) - (Mathf.Rad2Deg * newAngle);
            YawFutureInterpolation += Mathf.Abs(angleChange) > 180 ? 0 : angleChange;
        }

        PlayerJump();
        _railTickCount++;
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
        _currentCharge = ChargeType.DOUBLE_JUMP;

        var list = new List<Vector3> { new Vector3(0, GRAPPLE_Y_OFFSET, GRAPPLE_FORWARD_OFFSET), position };

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

        var y = velocity.y;
        velocity = Flatten(CrosshairDirection).normalized * Flatten(velocity).magnitude;
        velocity.y = y;
        YawFutureInterpolation = 0;
        PitchFutureInterpolation = 0;

        source.PlayOneShot(grappleRelease);
    }


    private void VectorToYawPitch(Vector3 vector, out float yaw, out float pitch)
    {
        yaw = Mathf.Atan2(vector.x, vector.z) * Mathf.Rad2Deg;
        pitch = -Mathf.Asin(vector.y) * Mathf.Rad2Deg;
    }

    public void GrappleMove(float f)
    {
        var position = _grappleAttachPosition;

        if (!grappleTether.enabled) grappleTether.enabled = true;

        var list = new List<Vector3> { new Vector3(0, GRAPPLE_Y_OFFSET, GRAPPLE_FORWARD_OFFSET), camera.transform.InverseTransformPoint(position) };

        grappleTether.positionCount = list.Count;
        grappleTether.SetPositions(list.ToArray());

        var towardPoint = (position - transform.position).normalized;
        var velocityProjection = Vector3.Dot(velocity, towardPoint);
        var tangentVector = (velocity + towardPoint * -velocityProjection).normalized;

        VectorToYawPitch(tangentVector, out var tangyaw, out var tangpitch);

        var yawChange = tangyaw - Yaw;
        if (Mathf.Abs(yawChange) > 300)
        {
            if (yawChange > 300) yawChange -= 360;
            else if (yawChange < 300) yawChange += 360;
        }
        var pitchChange = tangpitch - Pitch;
        YawFutureInterpolation += yawChange / 25;
        PitchFutureInterpolation += pitchChange / 25;

        var swingProjection = Mathf.Max(0, Vector3.Dot(velocity, Vector3.Lerp(towardPoint, tangentVector, 0.5f)));

        var projection = Mathf.Sqrt(swingProjection) * Vector3.Dot(-transform.right, towardPoint) * 6;
        SetCameraRotation(projection, 2);

        grappleDuring.pitch = (_grappleTicks * 2f / GRAPPLE_MAX_TICKS) + 1;

        if (velocityProjection < 0) velocity -= towardPoint * velocityProjection;

        if (velocity.magnitude < 0.05f) velocity += towardPoint * f * GRAPPLE_ACCELERATION;
        var magnitude = velocity.magnitude;
        velocity += CrosshairDirection * GRAPPLE_CONTROL_ACCELERATION * f;
        velocity = velocity.normalized * magnitude;

        var target = position + tangentVector * GRAPPLE_DISTANCE;
        var direction = Vector3.Lerp(velocity.normalized, (target - transform.position).normalized, GRAPPLE_CORRECTION_ACCELERATION);
        velocity = velocity.magnitude * direction.normalized;

        Accelerate(direction, GRAPPLE_SPEED, GRAPPLE_ACCELERATION, f);

        if (Vector3.Dot(towardPoint, CrosshairDirection) < 0 || _grappleTicks > GRAPPLE_MAX_TICKS)
        {
            //DetachGrapple();
        }
        _grappleTicks++;
        PlayerJump();
    }

    public void WallMove(float f)
    {
        _currentCharge = ChargeType.DOUBLE_JUMP;
        if (PlayerJump()) return;

        if (_wallTickCount == JUMP_FORGIVENESS_TICKS)
        {
            source.PlayOneShot(groundLand);
        }

        if (_wallTickCount == 0)
        {
            _wallStamina = WALL_STAMINA;
        }
        _wallTickCount++;


        if (_wallTickCount >= JUMP_FORGIVENESS_TICKS)
        {
            var angle = Vector3.Dot(Flatten(CrosshairDirection).normalized, _wallNormal);
            if (Mathf.Abs(angle) > WALL_NEUTRAL_DOT)
            {
                ApplyFriction(WALL_NEUTRAL_FRICTION * f);
            }
            else
            {
                var direction = Flatten(CrosshairDirection - angle * _wallNormal).normalized;
                if (_wallTickCount - JUMP_FORGIVENESS_TICKS < WALL_FRICTION_TICKS) ApplyFriction(WALL_FRICTION * f, WSPEED);
                Accelerate(direction, WSPEED, WALL_ACCELERATION, f);
            }
        }

        var normal = Flatten(_wallNormal);

        var projection = Vector3.Dot(CrosshairDirection, new Vector3(-normal.z, normal.y, normal.x));

        SetCameraRotation(WALL_LEAN_DEGREES * -projection * (_wallStamina / WALL_STAMINA), 8);

        if (_wallStamina <= 0)
        {
            _wallStamina = 0;
            IsOnWall = false;
            velocity += normal * WALL_END_BOOST_SPEED;
        }
        _wallStamina -= f * _wallTickCount;

        source.pitch = 1;

        if (velocity.y < 0)
        {
            velocity.y = Mathf.Lerp(velocity.y, 0, f * WALL_CATCH_FRICTION);
        }
        else
        {
            GravityTick(f);
        }

        Accelerate(-_wallNormal, 10, Gravity, f);
    }

    public void GravityTick(float f)
    {
        velocity.y -= Gravity * f;
    }

    public void GroundMove(float f)
    {
        _currentCharge = ChargeType.DASH;
        GravityTick(f);
        _lastWall = null;

        if (PlayerJump()) return;

        _groundTickCount++;

        if (_groundTickCount == JUMP_FORGIVENESS_TICKS)
        {
            source.PlayOneShot(groundLand);
        }

        if (_groundTickCount >= JUMP_FORGIVENESS_TICKS)
        {

            if (IsSliding || IsDashing)
            {
                _slideLeanVector = Vector3.Lerp(_slideLeanVector, Flatten(velocity).normalized, f * 4);
                var leanProjection = Vector3.Dot(_slideLeanVector, camera.transform.right);
                SetCameraRotation(leanProjection * 15, 6);

                AirAccelerate(f, SLIDE_MOVEMENT_SCALE);
            }
            else
            {
                _slideLeanVector = Flatten(velocity).normalized;
                GroundAccelerate(f);
            }
        }
        else
        {
            AirAccelerate(f, SLIDE_MOVEMENT_SCALE);
        }
    }

    public void GroundAccelerate(float f, float frictionMod = 1f)
    {
        ApplyFriction(f * GROUND_FRICTION * frictionMod, 0, WSPEED);
        if (Wishdir.magnitude > 0)
        {
            var speed = Flatten(velocity).magnitude;
            var y = velocity.y;

            velocity += Wishdir * f * GROUND_ACCELERATION;

            if (Flatten(velocity).magnitude >= WSPEED && Vector3.Angle(Wishdir, velocity) < 90)
            {
                velocity = Flatten(velocity).normalized * Mathf.Max(speed, WSPEED);
                velocity.y = y;
            }
        }
    }

    public void AirMove(float f)
    {
        GravityTick(f);
        _slideLeanVector = Flatten(velocity).normalized;

        if (!jumpKitEnabled)
        {
            AirAccelerate(f);
            return;
        }

        // Lean in
        var movement = velocity + (_dashVector * _dashTime);
        var didHit = rigidbody.SweepTest(movement.normalized, out var hit, movement.magnitude * WALL_LEAN_PREDICTION_TIME, QueryTriggerInteraction.Ignore);

        var eatJump = false;
        var currentLean = 0f;

        if (didHit
            && Math.Abs(Vector3.Angle(Vector3.up, hit.normal) - 90) < WALL_VERTICAL_ANGLE_GIVE
            && CanCollide(hit.collider, false)
            && (hit.collider.gameObject != _lastWall || Vector3.Dot(Flatten(hit.normal).normalized, _lastWallNormal) < 0.7))
        {
            if (!ApproachingWall) ApproachingWall = true;

            currentLean = 1 - hit.distance / movement.magnitude / WALL_LEAN_PREDICTION_TIME;

            // Eat jump inputs if you are < jumpForgiveness away from the wall to not eat double jump
            if (WALL_LEAN_PREDICTION_TIME * (1 - currentLean) / Time.fixedDeltaTime < JUMP_FORGIVENESS_TICKS) eatJump = true;

            var curve = currentLean * (2 - currentLean);

            var normal = Flatten(hit.normal);
            var projection = Vector3.Dot(CrosshairDirection, new Vector3(-normal.z, normal.y, normal.x));
            SetCameraRotation(WALL_LEAN_DEGREES * curve * -projection, 15);
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
        else if (didHit && Vector3.Angle(Vector3.up, hit.normal) < GROUND_ANGLE && CanCollide(hit.collider))
        {
            // Eat jump inputs if you are < jumpForgiveness away from the ground to not eat double jump
            if (hit.distance / movement.magnitude / Time.fixedDeltaTime < JUMP_FORGIVENESS_TICKS) eatJump = true;
        }

        var groundMod = 1 - Mathf.Min(Flatten(velocity).magnitude / (WSPEED - 1), 1);
        GroundAccelerate(f * groundMod, 0);
        f *= 1 - groundMod;
        var mod = 1 - Mathf.Min(currentLean * 2, 1);

        AirAccelerate(f, mod);

        if (eatJump)
        {
            if (PlayerInput.SincePressed(PlayerInput.Jump) < JUMP_FORGIVENESS_TICKS)
            {
                _jumpIsBuffered = true;
            }
        }
        else PlayerJump();
    }

    // Returns the amount of speed gained from air strafing
    public float AirAccelerate(float f, float accelMod = 1)
    {

        var accel = AIR_ACCELERATION;
        var airStrafeGains = 0f;

        accel *= accelMod;

        var forward = transform.forward * PlayerInput.GetAxisStrafeForward();
        var forwardspeed = Vector3.Dot(velocity, forward);
        var forwardaddspeed = Mathf.Abs(AIR_SPEED) - forwardspeed;
        if (forwardaddspeed > 0)
        {
            if (accel * f > forwardaddspeed)
                accel = forwardaddspeed / f;

            var addvector = accel * forward;
            var backspeed = Vector3.Dot(addvector, -Flatten(velocity).normalized);
            if (backspeed > BACKWARDS_AIR_ACCEL_CAP)
            {
                var x1 = backspeed;
                var x2 = BACKWARDS_AIR_ACCEL_CAP;
                var y1 = addvector.magnitude;
                var y2 = (x2 * y1) / x1;

                addvector = addvector.normalized * y2;
            }
            airStrafeGains += Mathf.Max(0, (velocity + addvector * f - velocity).magnitude);
            velocity += addvector * f;
        }

        if (PlayerInput.GetAxisStrafeRight() != 0)
        {
            var right = transform.right * PlayerInput.GetAxisStrafeRight();
            var offset = velocity + right * AIR_SPEED;
            var angle = Mathf.Atan2(offset.z, offset.x) - Mathf.Atan2(velocity.z, velocity.x);

            var offsetAngle = Mathf.Atan2(right.z, right.x) - angle;
            right = new Vector3(Mathf.Cos(offsetAngle), 0, Mathf.Sin(offsetAngle));

            var rightspeed = Vector3.Dot(velocity, right);
            var rightaddspeed = Mathf.Abs(AIR_SPEED) - rightspeed;
            if (rightaddspeed > 0)
            {
                if (accel * f > rightaddspeed)
                    accel = rightaddspeed / f;

                var addvector = accel * right;
                var backspeed = Vector3.Dot(addvector, -Flatten(velocity).normalized);
                if (backspeed > BACKWARDS_AIR_ACCEL_CAP)
                {
                    var x1 = backspeed;
                    var x2 = BACKWARDS_AIR_ACCEL_CAP;
                    var y1 = addvector.magnitude;
                    var y2 = (x2 * y1) / x1;

                    addvector = addvector.normalized * y2;
                }
                airStrafeGains += Mathf.Max(0, (velocity + addvector * f - velocity).magnitude);
                velocity += addvector * f;
            }
        }

        if (PlayerInput.GetAxisStrafeRight() == 0 && Vector3.Angle(Wishdir, velocity) < 90)
        {
            accel *= 1 - _wallRecovery / WALL_AIR_ACCEL_RECOVERY;
            var speed = Flatten(velocity).magnitude;
            var y = velocity.y;
            velocity += Wishdir * f * accel;
            velocity = Flatten(velocity).normalized * speed;
            velocity.y = y;
        }
        if (_wallRecovery > 0) _wallRecovery -= f;
        if (_wallRecovery < 0) _wallRecovery = 0;

        return airStrafeGains;
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

    public void Accelerate(Vector3 wishdir, float speed, float acceleration, float f = 1)
    {
        var currentspeed = Vector3.Dot(velocity, wishdir.normalized);
        var addspeed = Mathf.Abs(speed) - currentspeed;

        if (addspeed <= 0)
            return;

        var accelspeed = Mathf.Lerp(currentspeed, speed, acceleration * f) - currentspeed;
        if (accelspeed > addspeed)
            accelspeed = addspeed;

        velocity.x += accelspeed * wishdir.x;
        velocity.y += accelspeed * wishdir.y;
        velocity.z += accelspeed * wishdir.z;
    }

    public enum JumpType { WALL, AIR, GROUND }

    public bool PlayerJump()
    {
        int sinceJump = PlayerInput.SincePressed(PlayerInput.Jump);
        if (sinceJump < JUMP_FORGIVENESS_TICKS || _jumpIsBuffered)
        {
            _jumpIsBuffered = false;
            if (GrappleHooked)
            {
                DetachGrapple();
                PlayerInput.ConsumeBuffer(PlayerInput.Jump);
                return true;
            }

            if (IsOnRail)
            {
                EndRail();
                PlayerInput.ConsumeBuffer(PlayerInput.Jump);
                return true;
            }

            var wallJump = PlayerInput.tickCount - _wallTimestamp < COYOTE_TICKS;
            var groundJump = PlayerInput.tickCount - _groundTimestamp < COYOTE_TICKS;
            var coyoteJump = PlayerInput.tickCount - _wallTimestamp != 0 && PlayerInput.tickCount - _groundTimestamp != 0;
            _groundTimestamp = -COYOTE_TICKS;

            if (!groundJump && !wallJump && _currentCharge == ChargeType.NONE) return false;

            var type = JumpType.AIR;
            if (groundJump) type = JumpType.GROUND;
            if (wallJump) type = JumpType.WALL;
            var jumpEvent = new JumpEvent
            {
                jumpHeight = JUMP_HEIGHT,
                cancelled = false,
                currentGround = _currentGround,
                type = type
            };
            PlayerJumpEvent?.Invoke(ref jumpEvent);
            if (jumpEvent.cancelled) return false;

            if (wallJump)
            {
                // Jumping from wall
                _wallRecovery = WALL_AIR_ACCEL_RECOVERY;
                SetCameraRotation(0, CAMERA_ROLL_CORRECT_SPEED);
                source.PlayOneShot(jump);
                IsOnWall = false;

                _wallTimestamp = -COYOTE_TICKS;
                _lastWall = _currentWall;
                _lastWallNormal = _wallNormal;

                var y = velocity.y;
                var velocityDirection = velocity.normalized;
                velocityDirection *= Mathf.Sign(Vector3.Dot(CrosshairDirection, velocityDirection)); // This ensures you always jump in the direction youre looking, makes backwards wall kicks impossible
                var angle = Vector3.Dot(Flatten(CrosshairDirection).normalized, _wallNormal);
                if (angle > WALL_NEUTRAL_DOT)
                {
                    velocityDirection = CrosshairDirection;
                }
                else if (angle < -WALL_NEUTRAL_DOT)
                {
                    velocityDirection = _wallNormal;
                }
                var direction = Flatten(velocityDirection + _wallNormal * WALL_JUMP_ANGLE).normalized;
                velocity = Mathf.Max(velocity.magnitude, WSPEED) * direction;
                Accelerate(Flatten(velocityDirection).normalized, FLOWSPEED, WALL_JUMP_FLOW_ACCEL);

                if (!coyoteJump)
                {
                    var timing = Mathf.Max(sinceJump, _wallTickCount);
                    var speed = Mathf.Max(0, WALL_KICK_SPEED - (WALL_KICK_FADEOFF * timing));
                    velocity += Flatten(velocityDirection).normalized * speed;

                    if (speed > 0)
                    {
                        source.PlayOneShot(ding);
                    }
                }

                velocity.y = y;
                if (IsDashing)
                {
                    velocity.y = Mathf.Max(jumpEvent.jumpHeight / 2, velocity.y);
                    CancelDash();
                    //var cancelForce = Mathf.Min(y * WALL_UP_CANCEL_ACCELERATION, WALL_UP_CANCEL_SPEED);
                    //velocity.y = Mathf.Max(cancelForce, jumpEvent.jumpHeight / 2);
                    source.PlayOneShot(wallKick);
                }
                else
                {
                    velocity.y = Mathf.Max(jumpEvent.jumpHeight, velocity.y);
                }
            }
            else
            {
                SetCameraRotation(0, CAMERA_ROLL_CORRECT_SPEED);
                source.PlayOneShot(jump);

                var height = jumpEvent.jumpHeight;
                if (!groundJump)
                {
                    _currentCharge = ChargeType.NONE;
                    StopDash();
                }
                else
                {
                    IsOnGround = false;
                    if (IsDashing)
                    {
                        source.PlayOneShot(wallKick);
                        CancelDash();

                        height /= 1.5f;
                    }
                }
                velocity.y = Mathf.Max(height, velocity.y);
                // Using charge
                /*if (_currentCharge == ChargeType.DASH)
                {
                    if (Environment.TickCount - _dashTimestamp > DASH_COOLDOWN)
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
                    velocity.y = Mathf.Max(jumpEvent.jumpHeight, velocity.y);
                }*/
            }

            PlayerInput.ConsumeBuffer(PlayerInput.Jump);
            return true;
        }
        return false;
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}