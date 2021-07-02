using System;
using System.CodeDom;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    public enum AbilityType
    {
        NONE,
        DASH
    }

    public Collider hitbox;
    public WeaponManager weaponManager;

    public new Camera camera;
    public Transform cameraParent;
    public GameObject abilityDot;
    public new Rigidbody rigidbody;

    public Text wallkickText;

    public Vector3 cameraPosition;
    public Vector3 velocity;

    public bool jumpKitEnabled = true;

    public PlayerAudioManager audioManager { get; set; }

    public float Yaw { get; set; }
    public float YawFutureInterpolation { get; set; }
    public float YawIncrease { get; set; }
    public float Pitch { get; set; }
    public float PitchFutureInterpolation { get; set; }
    public float LookScale { get; set; }
    public float CameraRoll { get; set; }
    public Vector3 Wishdir { get; set; }
    public Vector3 CrosshairDirection { get; set; }
    public Vector3 InterpolatedPosition { get { return Vector3.Lerp(_previousPosition, transform.position, _motionInterpolationDelta / Time.fixedDeltaTime); } }
    public float Gravity { get { return (velocity.y - Mathf.Lerp(velocity.y, -TERMINAL_VELOCITY, GRAVITY)) * (IsDashing ? 0 : 1); } }

    private bool _sliding;
    public const bool TOGGLE_CROUCH = false;
    public bool IsSliding
    {
        get { return TOGGLE_CROUCH ? _sliding : PlayerInput.GetKey(PlayerInput.Slide); }
        set
        {
            _sliding = value;
        }
    }
    // Surfaces have a "level" to make them a little more sticky than being able to come off them in 1 tick.
    // This is to prevent repeated landings on surfaces.
    public bool IsOnWall { get { return _wallLevel > 0; } set { if (value) _wallLevel = SURFACE_MAX_LEVEL; else _wallLevel = 0; } }
    private int _wallLevel;
    public bool IsOnGround { get { return _groundLevel > 0; } set { if (value) _groundLevel = SURFACE_MAX_LEVEL; else _groundLevel = 0; } }
    private int _groundLevel;
    public const int SURFACE_MAX_LEVEL = 5;

    public const float CAMERA_ROLL_CORRECT_SPEED = 40f;
    public const float GROUND_ACCELERATION = 15;
    public const float GROUND_ANGLE = 45;
    public const float GROUND_FRICTION = 5f;
    public const float SLIDE_FRICTION = 0.2f;
    public const float SLIDE_MOVEMENT_SCALE = 2f;
    private int _groundTickCount;
    private int _airTickCount;
    private int _groundTimestamp = -100000;
    private Vector3 _previousPosition;
    private float _previousSpeed;
    private Vector3 _slideLeanVector;
    private float _motionInterpolationDelta;
    private GameObject _currentGround;
    private float _crouchAmount;
    private bool _tpThisTick;

    private float _cameraRotation;
    private float _cameraRotationSpeed;
    public void SetCameraRoll(float target, float speed)
    {
        _cameraRotation = target;
        _cameraRotationSpeed = speed;
    }

    public const float BASE_SPEED = 16;

    public bool ApproachingWall { get; set; }
    public const float WALL_CATCH_FRICTION = 10f;
    public const float WALL_BACK_FRICTION = 1f;
    public const float WALL_JUMP_ANGLE = 0.4f;
    public const float WALL_VERTICAL_ANGLE_GIVE = 10f;
    public const float WALL_AIR_ACCEL_RECOVERY = 0.35f;
    public const float WALL_UP_CANCEL_SPEED = 80;
    public const float WALL_UP_CANCEL_ACCELERATION = 2.2f;
    public const float WALL_END_BOOST_SPEED = 2;
    public const float WALL_NEUTRAL_DOT = 0.9f;
    public const float WALL_LEAN_DEGREES = 15f;
    public const float WALL_SPEED = 25;
    public const float WALL_ACCELERATION = 0.4f;
    public const float WALL_LEAN_PREDICTION_TIME = 0.25f;
    public const float WALL_JUMP_SPEED = 4;
    public const bool WALL_ALLOW_SAME_FACING = false;
    private Vector3 _wallNormal;
    private bool _wallLeanCancelled;
    private int _wallTimestamp = -100000;
    private int _wallTickCount;
    private float _jumpBuffered;
    private int _jumpTimestamp;
    private GameObject _lastGround;
    private GameObject _lastWall;
    private GameObject _currentWall;
    private float _wallRecovery;
    private float _wallLeanAmount;

    private const float FINISH_HOVER_DISTANCE = 6;

    private const float AIR_SPEED = 2f;
    private const float SIDE_AIR_ACCELERATION = 45;
    private const float FORWARD_AIR_ACCELERATION = 100;
    private const float BACKWARD_AIR_ACCELERATION = 35;

    public bool DoubleJumpAvailable { get; set; }
    public bool DashAvailable { get; set; }

    public bool IsOnRail { get { return _currentRail != null; } }
    public const int RAIL_COOLDOWN_TICKS = 40;
    private int _railTimestamp = -100000;
    private int _railDirection;
    private float _railSpeed;
    private int _railTickCount;
    private Vector3 _railVector;
    private Vector3 _railLeanVector;
    private GameObject _lastRail;
    private Rail _currentRail;

    public bool IsDashing { get { return _dashVector.magnitude > 0.05f; } }
    public const float DASH_SPEED = 25;
    public const float DASH_CANCEL_TEMP_SPEED = 20;
    public const float DASH_CANCEL_TEMP_SPEED_DECAY = 20;
    public const float DASH_CANCEL_SPEED = 4;
    public const float DASH_CANCEL_UP_SPEED = 12;
    public const float DASH_CANCEL_DOWN_OVERFLOW_SPEED = 10;
    public const float DASH_TIME = 0.6f;
    private float _dashTime;
    private float _dashCancelTempSpeed;
    private Vector3 _dashVector;

    private float _teleportTime;
    private float _teleportTotalTime;
    private Vector3 _teleportToPosition;
    private Vector3 _teleportStartPosition;

    public const float GRAVITY = 0.5f;
    public const float TERMINAL_VELOCITY = 60;

    public const float MAX_JUMP_HEIGHT = 18f;
    public const float MIN_JUMP_HEIGHT = 16f;
    public const int JUMP_STAMINA_RECOVERY_TICKS = 5;
    public const float JUMP_STAMINA_RECOVERY_FRICTION = 4;
    public const int COYOTE_TICKS = 20;

    public const int WALL_JUMP_FORGIVENESS_TICKS = 1;
    public const int GROUND_JUMP_FORGIVENESS_TICKS = 6;

    public LineRenderer grappleTether;
    public bool GrappleHooked { get; set; }
    public const float GRAPPLE_Y_OFFSET = -1.2f;
    public const float GRAPPLE_FORWARD_OFFSET = 0.5f;
    public const float GRAPPLE_CONTROL_ACCELERATION = 400f;
    public const float GRAPPLE_DISTANCE = 10f;
    public const float GRAPPLE_SPEED = 80;
    public const float GRAPPLE_ACCELERATION = 0.7f;
    public const float GRAPPLE_CORRECTION_ACCELERATION = 0.1f;
    public const int GRAPPLE_MAX_TICKS = 150;
    private int _grappleTicks;
    private Vector3 _grappleAttachPosition;

    /* Audio */
    public AudioClip jump;
    public AudioClip jumpair;
    public AudioClip dash;
    public AudioClip dashCancel;
    public AudioClip groundLand;
    public AudioClip slide;
    public AudioClip wallJump;
    public AudioClip wallRun;
    public AudioClip grappleAttach;
    public AudioClip grappleRelease;
    public AudioClip railLand;
    public AudioClip railDuring;
    public AudioClip railEnd;
    public AudioClip viz;

    private void Awake()
    {
        audioManager = GetComponent<PlayerAudioManager>();
        LookScale = 1;

        Yaw += transform.rotation.eulerAngles.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        grappleTether.useWorldSpace = false;
        grappleTether.enabled = false;

        var positionOverride = GameObject.Find("PlayerStartPositionOverride");
        if (positionOverride != null)
        {
            transform.position = positionOverride.transform.position;
        }

        if (Game.lastCheckpoint.sqrMagnitude > 0.05f && Game.checkpointScene == SceneManager.GetActiveScene().name)
        {
            transform.position = Game.lastCheckpoint;
            Yaw = Game.checkpointYaw;
        }

        if (Physics.Raycast(transform.position, Vector3.down, out var hit, 5f, 1, QueryTriggerInteraction.Ignore) && Vector3.Angle(hit.normal, Vector3.up) < GROUND_ANGLE)
        {
            transform.position = hit.point + Vector3.up * 0.8f;
            IsOnGround = true;
            _currentGround = hit.collider.gameObject;
            _groundTickCount = GROUND_JUMP_FORGIVENESS_TICKS;
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

    private void Update()
    {
        _jumpBuffered = Mathf.Max(_jumpBuffered - Time.deltaTime, 0);

        if (PlayerInput.GetKeyDown(PlayerInput.Pause))
        {
            if (!IsPaused()) Pause();
        }
        if (!IsPaused() && Cursor.visible) Unpause();

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

        camera.transform.localRotation = Quaternion.Euler(new Vector3(Pitch, 0, CameraRoll));
        CrosshairDirection = cam.forward;
        transform.rotation = Quaternion.Euler(0, Yaw, 0);

        // This value is used to calcuate the positions in between each fixedupdate tick
        _motionInterpolationDelta += Time.deltaTime;

        CameraRoll -= Mathf.Sign(CameraRoll - _cameraRotation) * Mathf.Min(_cameraRotationSpeed * Time.deltaTime, Mathf.Abs(CameraRoll - _cameraRotation));

        // Check for level restart
        if (PlayerInput.GetKeyDown(PlayerInput.LastCheckpoint)) Game.ReturnToLastCheckpoint();
        if (PlayerInput.GetKeyDown(PlayerInput.RestartLevel)) Game.RestartLevel();

        var position = cameraPosition;

        if (IsSliding)
        {
            _crouchAmount = Mathf.Lerp(_crouchAmount, 1, Time.deltaTime * 5);
        }
        else
        {
            _crouchAmount = Mathf.Lerp(_crouchAmount, 0, Time.deltaTime * 5);
        }
        //_crouchAmount = Mathf.Lerp(_crouchAmount, Mathf.Clamp01((Flatten(velocity).magnitude - (BASE_SPEED / 2)) / 20), Time.deltaTime * 5);

        var ease = 1 - Mathf.Pow(1 - _crouchAmount, 2);
        position.y -= 0.7f * ease;

        camera.transform.position = InterpolatedPosition + position;

        var targetFOV = Flatten(velocity).magnitude + (100 - BASE_SPEED);
        targetFOV = Mathf.Max(targetFOV, 100);
        targetFOV = Mathf.Min(targetFOV, 120);

        if (_teleportTime > 0)
        {
            targetFOV += 20;
        }

        camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, targetFOV, Time.deltaTime * 5);

        if (PlayerInput.GetKeyDown(PlayerInput.Slide))
        {
            IsSliding = !IsSliding;
        }

        if (PlayerInput.SincePressed(PlayerInput.SecondaryInteract) < GROUND_JUMP_FORGIVENESS_TICKS && !IsDashing && !IsOnRail && DashAvailable)
        {
            PlayerInput.ConsumeBuffer(PlayerInput.SecondaryInteract);
            var wishdir = CrosshairDirection;

            Dash(wishdir);
        }
    }

    private void FixedUpdate()
    {
        _wallRecovery -= Mathf.Min(_wallRecovery, Time.fixedDeltaTime);

        // Set Wishdir
        Wishdir = (transform.right * PlayerInput.GetAxisStrafeRight() + transform.forward * PlayerInput.GetAxisStrafeForward()).normalized;
        if (Wishdir.magnitude <= 0)
        {
            Wishdir = (transform.right * Input.GetAxis("Joy 1 X") + transform.forward * -Input.GetAxis("Joy 1 Y")).normalized;
        }

        // Timestamps used for coyote time
        if (IsOnGround) _groundTimestamp = PlayerInput.tickCount;
        if (IsOnWall) _wallTimestamp = PlayerInput.tickCount;
        if (IsOnRail) _railTimestamp = PlayerInput.tickCount;

        if (bonusGravityCooldownTicks > 0) bonusGravityCooldownTicks--;

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
            AirMove(ref velocity, factor);
        if (IsDashing) AirMove(ref _dashVector, factor);

        // do teleport after movement so collision is done first
        // makes buffering work so you can tp onto a surface and be registered as on the surface before inputs are calculated
        _tpThisTick = false;
        if (_teleportTime > 0)
        {
            _teleportTime -= Time.fixedDeltaTime;
            if (_teleportTime <= 0)
            {
                _teleportTime = 0;

                var tpVector = _teleportToPosition - _teleportStartPosition;
                velocity = Flatten(velocity).magnitude * Flatten(tpVector).normalized;
                transform.position = _teleportToPosition;
                _tpThisTick = true;
            }
        }

        if (!IsOnWall) _wallTickCount = 0;
        if (!IsOnGround) _groundTickCount = 0;
        if (IsOnGround) _airTickCount = 0;

        if (_dashTime > 0)
        {
            _slideLeanVector = Vector3.Lerp(_slideLeanVector, Flatten(velocity).normalized, factor * 4);

            if (!ApproachingWall)
            {
                var leanProjection = Vector3.Dot(_slideLeanVector, camera.transform.right);
                SetCameraRoll(leanProjection * 15, CAMERA_ROLL_CORRECT_SPEED);
            }

            _dashTime -= factor;
        }
        if (_dashTime <= 0)
        {
            _dashTime = 0;
            _dashVector = Vector3.zero;
        }
        if (_dashCancelTempSpeed > 0)
        {
            var loss = factor * DASH_CANCEL_TEMP_SPEED_DECAY;
            velocity -= Flatten(velocity).normalized * loss;
            Game.Canvas.speedChangeDisplay.interpolation += loss;
            _dashCancelTempSpeed -= loss;
        }
        if (Game.PostProcessVolume != null && Game.PostProcessVolume.profile.TryGetSettings(out MotionBlur motion))
        {
            motion.enabled.value = IsDashing;
        }

        // Interpolation delta resets to 0 every fixed update tick, its used to measure the time since the last fixed update tick
        _motionInterpolationDelta = 0;
        var movement = (_dashVector.magnitude > 0 ? _dashVector : velocity) * Time.fixedDeltaTime;
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
                if (CanCollide(hit.collider))
                {
                    ContactCollider(hit.normal, hit.collider);
                }
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

        var overlap = Physics.OverlapBox(hitbox.bounds.center, hitbox.bounds.extents);

        foreach (var collider in overlap)
        {
            if (!CanCollide(collider)) continue;
            if (Physics.ComputePenetration(hitbox, hitbox.transform.position, hitbox.transform.rotation, collider, collider.transform.position, collider.transform.rotation, out var direction, out var distance))
            {
                if (collider.isTrigger)
                {
                    ContactTrigger(direction, collider);
                    continue;
                }

                if (CanCollide(collider))
                {
                    ContactCollider(direction, collider);
                }

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
            Game.ReturnToLastCheckpoint();
        }

        var dashProjection = Vector3.Dot(_dashVector, -normal);
        if (dashProjection > 0)
        {
            var impulse = normal * dashProjection;
            _dashVector += impulse;
        }

        var velocityProjection = Vector3.Dot(velocity, -normal);
        if (velocityProjection > 0)
        {
            var impulse = normal * velocityProjection;
            if (_tpThisTick)
            {
                var speed = Flatten(velocity).magnitude;
                velocity += impulse;
                var y = velocity.y;
                velocity = Flatten(velocity).normalized * speed;
                velocity.y = y;
            }
            else
            {
                velocity += impulse;
            }
        }

        var angle = Vector3.Angle(Vector3.up, normal);

        if (angle < GROUND_ANGLE && !collider.CompareTag("Uninteractable") && !collider.CompareTag("Wall"))
        {
            IsOnGround = true;
            _currentGround = collider.gameObject;
        }

        if (!collider.CompareTag("Uninteractable")
            && Mathf.Abs(angle - 90) < WALL_VERTICAL_ANGLE_GIVE
            && !IsOnGround && jumpKitEnabled
            && (collider.gameObject != _lastWall || Vector3.Dot(Flatten(normal).normalized, _wallNormal) < 0.7 || WALL_ALLOW_SAME_FACING))
        {
            if (IsOnWall && Vector3.Angle(_wallNormal, Flatten(normal).normalized) > 10)
            {
                Accelerate(_wallNormal, WALL_END_BOOST_SPEED, WALL_END_BOOST_SPEED);
                IsOnWall = false;
                audioManager.StopAudio(wallRun);
            }
            else
            {
                IsOnWall = true;
                _wallNormal = Flatten(normal).normalized;

                _currentWall = collider.gameObject;
            }
        }
    }

    private void ContactTrigger(Vector3 normal, Collider other)
    {
        if (other.CompareTag("Rail") && (PlayerInput.tickCount - _railTimestamp > RAIL_COOLDOWN_TICKS || other.transform.parent.gameObject != _lastRail))
        {
            _lastRail = other.transform.parent.gameObject;
            SetRail(other.gameObject.transform.parent.gameObject.GetComponent<Rail>());
        }

        if (other.CompareTag("Kill Block"))
        {
            Game.ReturnToLastCheckpoint();
        }
    }

    public void GunShotEvent(RaycastHit hit)
    {
        //Teleport(hit.point - (Game.Player.CrosshairDirection * 0.5f));
    }

    public void Teleport(Vector3 position)
    {
        _teleportToPosition = position;
        _teleportTime = 0.2f;
        _teleportTotalTime = 0.2f;
        _teleportStartPosition = transform.position;
    }

    public void Dash(Vector3 wishdir)
    {
        DashAvailable = false;
        audioManager.PlayOneShot(dash);

        if (velocity.magnitude < BASE_SPEED) velocity = wishdir * BASE_SPEED;

        var x1 = Flatten(velocity).magnitude;
        var x2 = Flatten(wishdir).magnitude;
        var y2 = wishdir.y;

        var y1 = x1 * y2 / x2;

        //velocity = Flatten(wishdir).normalized * x1;

        var _dashUpSpeed = 30;
        if (Mathf.Abs(y1) > _dashUpSpeed)
        {
            y1 = _dashUpSpeed * Mathf.Sign(y1);

            if (Vector3.Angle(wishdir, Vector3.up) < 22.5)
            {
                //velocity = Flatten(velocity).normalized * (y1 * x2 / y2);
            }
        }

        //velocity.y = y1;

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

        var direction = wishdir.normalized;

        var bonusSpeed = Mathf.Max(Vector3.Dot(DASH_SPEED * direction, velocity.normalized), 0);
        _dashVector = (bonusSpeed + velocity.magnitude) * direction;

        var up = Vector3.Dot(_dashVector, Vector3.up);

        var forward = new Vector3(Mathf.Sin(Mathf.Deg2Rad * Yaw), 0, Mathf.Cos(Mathf.Deg2Rad * Yaw));
        velocity = Flatten(velocity).magnitude * forward;

        if (up > MAX_JUMP_HEIGHT) velocity.y = MAX_JUMP_HEIGHT;
        else velocity.y = up;

        if (_dashVector.y <= 0)
        {
            _dashTime = DASH_TIME;
        }
        else
        {
            var maxUpDistance = 25 / Mathf.Abs(_dashVector.y);
            _dashTime = Mathf.Min(DASH_TIME, maxUpDistance);
        }
    }

    public void StopDash()
    {
        if (IsDashing)
        {
            SetCameraRoll(0, CAMERA_ROLL_CORRECT_SPEED);
            _dashTime = 0;
        }
    }

    public void CancelDash(ref float jumpHeight)
    {
        if (IsDashing)
        {
            audioManager.PlayOneShot(dashCancel);
            velocity = Mathf.Max(velocity.magnitude, BASE_SPEED) * velocity.normalized;

            var divide = 1.4f;
            var height = jumpHeight / divide;
            if (velocity.y > height)
            {
                velocity.y *= 1.8f;
            }
            else
            {
                var gain = DASH_CANCEL_TEMP_SPEED - _dashCancelTempSpeed;
                var overflow = DASH_CANCEL_SPEED;
                jumpHeight /= divide;
                velocity += Flatten(velocity).normalized * (gain + overflow);
                _dashCancelTempSpeed += gain;
                Game.Canvas.speedChangeDisplay.interpolation -= gain;
            }
            velocity.y = _dashVector.y;

            StopDash();
        }
    }

    public void SetRail(Rail rail)
    {
        if (IsOnRail) return;
        _currentRail = rail;
        audioManager.PlayOneShot(railLand);
        audioManager.PlayAudio(railDuring, true);
        _railTickCount = 0;
        if (IsDashing) StopDash();
        _railLeanVector = Vector3.up;
        if (rail.railDirection == Rail.RailDirection.BACKWARD)
        {
            Game.lastCheckpoint = rail.smoothedPoints[rail.smoothedPoints.Length - 1];
            Game.checkpointScene = SceneManager.GetActiveScene().name;
            var direction = rail.smoothedPoints[rail.smoothedPoints.Length - 2] - Game.lastCheckpoint;
            var angle = Mathf.Atan2(direction.x, direction.z);
            Game.checkpointYaw = Mathf.Rad2Deg * angle;
            _railDirection = -1;
        }
        else if (rail.railDirection == Rail.RailDirection.FORWARD)
        {
            Game.lastCheckpoint = rail.smoothedPoints[0];
            Game.checkpointScene = SceneManager.GetActiveScene().name;
            var direction = rail.smoothedPoints[1] - Game.lastCheckpoint;
            var angle = Mathf.Atan2(direction.x, direction.z);
            Game.checkpointYaw = Mathf.Rad2Deg * angle;
            _railDirection = 1;
        }
        if (GrappleHooked) DetachGrapple();
    }

    public void EndRail()
    {
        if (!IsOnRail) return;
        velocity.y += MIN_JUMP_HEIGHT;
        SetCameraRoll(0, CAMERA_ROLL_CORRECT_SPEED);
        _currentRail = null;
        audioManager.StopAudio(railDuring);
        audioManager.PlayOneShot(railEnd);
        _railDirection = 0;
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

        var balance = leanVector + Vector3.up * 0.4f;

        return balance.normalized;
    }

    public void RailMove(float f)
    {
        if (!IsOnRail) return;
        DoubleJumpAvailable = true;
        DashAvailable = true;

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

        var previousVector = _railVector;
        _railVector = -(current - next).normalized;

        if ((_railDirection == -1 && closeIndex == 0 || _railDirection == 1 && closeIndex == _currentRail.smoothedPoints.Length - 1) && Vector3.Dot(transform.position - current, _railVector) > 0)
        {
            EndRail();
            return;
        }

        var balanceVector = GetBalanceVector(closeIndex + _railDirection);
        _railLeanVector = Vector3.Lerp(_railLeanVector, balanceVector, f);

        if (_railTickCount == 0)
        {
            var velInRailDirection = Flatten(velocity).magnitude * Flatten(_railVector).normalized;
            velInRailDirection.y = velocity.y;

            var projection = Vector3.Dot(velInRailDirection, _railVector);
            _railSpeed = Mathf.Max(_currentRail.speed, projection);
        }
        var correctionVector = -(transform.position - next).normalized;
        velocity = _railSpeed * Vector3.Lerp(_railVector, correctionVector, f * 20).normalized;

        if (velocity.y < 0) GravityTick(f);

        var totalAngle = Vector3.Angle(Vector3.up, _railLeanVector) / 2f;
        var roll = Vector3.Dot(_railLeanVector.normalized * totalAngle, -transform.right);

        var speed = Mathf.Abs(CameraRoll - roll) / Time.fixedDeltaTime;
        SetCameraRoll(roll, speed);

        if (_railTickCount > 0)
        {
            VectorToYawPitch(previousVector, out var prevVecYaw, out var prevVecPitch);
            VectorToYawPitch(_railVector, out var currVecYaw, out var currVecPitch);

            var yawChange = currVecYaw - prevVecYaw;
            var pitchChange = currVecPitch - prevVecPitch;

            var prevAngle = Vector3.Angle(Flatten(CrosshairDirection), Flatten(previousVector));
            var currAngle = Vector3.Angle(Flatten(CrosshairDirection), Flatten(_railVector));

            var change = Vector3.Angle(Flatten(_railVector), Flatten(previousVector));

            if (yawChange < 0) change *= -1;

            if (prevAngle > currAngle)
            {
                yawChange = change * Mathf.Pow(1 - (Mathf.Clamp(currAngle, 0, 90) / 90f), 2);
            }
            else
            {
                yawChange = change * Mathf.Clamp01(Mathf.Pow(1 + (Mathf.Clamp(currAngle, 0, 90) / 90f), 2) + 0.5f);
            }

            var pointsToEndOfRail = _railDirection == 1 ? _currentRail.smoothedPoints.Length - 1 - closeIndex : closeIndex;
            yawChange *= Mathf.Clamp(pointsToEndOfRail, 0, 5) / 5;
            yawChange *= Mathf.Clamp(_railTickCount - 1, 0, 10) / 10;

            pitchChange /= 2;

            YawFutureInterpolation += yawChange;
            PitchFutureInterpolation += pitchChange;
        }

        PlayerJump();
        _railTickCount++;
    }

    public void AttachGrapple(Vector3 position)
    {
        if (GrappleHooked) return;
        audioManager.PlayOneShot(grappleAttach);
        if (IsOnRail) EndRail();
        _grappleAttachPosition = position;
        GrappleHooked = true;
        _grappleTicks = 0;

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
        SetCameraRoll(0, CAMERA_ROLL_CORRECT_SPEED);

        var y = velocity.y;
        velocity = Flatten(CrosshairDirection).normalized * Flatten(velocity).magnitude;
        velocity.y = y;
        YawFutureInterpolation = 0;
        PitchFutureInterpolation = 0;

        audioManager.PlayOneShot(grappleRelease);
    }


    private void VectorToYawPitch(Vector3 vector, out float yaw, out float pitch)
    {
        yaw = Mathf.Atan2(vector.x, vector.z) * Mathf.Rad2Deg;
        pitch = -Mathf.Asin(vector.y) * Mathf.Rad2Deg;
    }

    private void LookTowardTick(Vector3 vector, float reduction)
    {
        VectorToYawPitch(vector, out var tangyaw, out var tangpitch);

        var yawChange = tangyaw - Yaw;
        if (Mathf.Abs(yawChange) > 200)
        {
            if (yawChange > 200) yawChange -= 360;
            else if (yawChange < 200) yawChange += 360;
        }
        var pitchChange = tangpitch - Pitch;
        YawFutureInterpolation += yawChange / reduction;
        PitchFutureInterpolation += pitchChange / reduction;
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

        var lookAdjust = Vector3.Lerp(tangentVector, towardPoint, 0.3f);
        //LookTowardTick(lookAdjust, 35);

        var swingProjection = Mathf.Max(0, Vector3.Dot(velocity, Vector3.Lerp(towardPoint, tangentVector, 0.5f)));

        if (velocity.magnitude < 0.05f) velocity += towardPoint * f * GRAPPLE_ACCELERATION;
        //var magnitude = velocity.magnitude;
        //velocity += (CrosshairDirection - Vector3.Dot(CrosshairDirection, towardPoint) * towardPoint).normalized * GRAPPLE_CONTROL_ACCELERATION * f;
        //velocity = velocity.normalized * magnitude;

        if (Vector3.Distance(position, transform.position) > GRAPPLE_DISTANCE)
        {
            var target = position + tangentVector * GRAPPLE_DISTANCE;
            var direction = Vector3.Lerp(velocity.normalized, (target - transform.position).normalized, GRAPPLE_CORRECTION_ACCELERATION);
            velocity = velocity.magnitude * direction.normalized;
        }

        if (velocityProjection < 0)
        {
            velocity -= towardPoint * (velocityProjection / 10);
        }

        var gain = Accelerate(velocity.normalized, GRAPPLE_SPEED, GRAPPLE_ACCELERATION, f);

        var absolute = (gain / f) / 6f;
        var projection = Mathf.Sqrt(swingProjection) * Vector3.Dot(-transform.right, towardPoint) * absolute;
        SetCameraRoll(projection, CAMERA_ROLL_CORRECT_SPEED);

        if (_grappleTicks > GRAPPLE_MAX_TICKS)
        {
            //DetachGrapple();
        }
        if (Vector3.Dot(towardPoint, CrosshairDirection) < 0 || _grappleTicks > GRAPPLE_MAX_TICKS)
        {
            //DetachGrapple();
        }
        _grappleTicks++;
        PlayerJump();
    }

    public void WallMove(float f)
    {
        DoubleJumpAvailable = true;
        _lastGround = null;
        if (PlayerJump()) return;

        if (_wallTickCount == WALL_JUMP_FORGIVENESS_TICKS)
        {
            audioManager.PlayAudio(wallRun, true);
        }
        audioManager.SetVolume(wallRun, Mathf.Clamp01(_wallTickCount / 10f));
        _wallTickCount++;

        if (_wallTickCount >= WALL_JUMP_FORGIVENESS_TICKS && _wallTickCount < JUMP_STAMINA_RECOVERY_TICKS)
        {
            ApplyFriction(f * JUMP_STAMINA_RECOVERY_FRICTION, BASE_SPEED);
        }

        var normal = Flatten(_wallNormal);
        var projection = Vector3.Dot(CrosshairDirection, new Vector3(-normal.z, normal.y, normal.x));

        _wallLeanAmount = Mathf.Clamp01(_wallLeanAmount + Time.fixedDeltaTime / 0.1f);
        var roll = WALL_LEAN_DEGREES * -projection * _wallLeanAmount;
        var speed = Mathf.Abs(CameraRoll - roll) / Time.fixedDeltaTime;
        SetCameraRoll(roll, speed);
        if (IsDashing)
        {
            _dashVector.y = 0;
        }

        if (Flatten(velocity).magnitude < BASE_SPEED / 3)
        {
            var alongView = (CrosshairDirection - (Flatten(_wallNormal).normalized * Vector3.Dot(CrosshairDirection, Flatten(_wallNormal).normalized))).normalized;
            Accelerate(alongView, BASE_SPEED, BASE_SPEED, f);
        }
        else
        {
            //Accelerate(Flatten(velocity).normalized, BASE_SPEED, BASE_SPEED, f);
            Accelerate(Flatten(velocity).normalized, WALL_SPEED, WALL_ACCELERATION, f);
        }

        if (velocity.y < 0)
        {
            velocity.y = Mathf.Lerp(velocity.y, 0, f * WALL_CATCH_FRICTION);
        }
        else
        {
            GravityTick(f);
        }

        //Time.timeScale = 0.1f;
        Accelerate(-_wallNormal, 1, Gravity, f);
    }

    private int bonusGravityCooldownTicks;

    public void GravityTick(float f)
    {
        if (bonusGravityCooldownTicks == 0 && rigidbody.SweepTest(Vector3.down, out var hit, 20, QueryTriggerInteraction.Ignore) && !hit.collider.CompareTag("Uninteractable"))
            velocity.y -= Gravity * f * 2;
        else
            velocity.y -= Gravity * f;
    }

    private GameObject _lastRefreshGround;

    public void GroundMove(float f)
    {
        if (_currentGround.CompareTag("Finish"))
        {
            Game.EndTimer();
        }
        DoubleJumpAvailable = true;
        GravityTick(f);
        _lastWall = null;

        if (!DashAvailable && _lastRefreshGround != _currentGround)
        {
            DashAvailable = true;
            _lastRefreshGround = _currentGround;
        }
        _lastGround = _currentGround;
        if ((_crouchAmount < 0.7 && IsSliding) || (_groundTickCount == 0 && IsSliding))
        {
            var speed = Mathf.Lerp(0, BASE_SPEED + 8, Mathf.Clamp01(_crouchAmount * 1.5f));
            Accelerate(Flatten(velocity).normalized, speed, BASE_SPEED);
        }

        if (PlayerJump()) return;

        _groundTickCount++;

        if (_groundTickCount == 1 && !IsSliding) audioManager.PlayAudio(groundLand);

        if (IsSliding)
        {
            if (!audioManager.IsPlaying(slide))
            {
                audioManager.PlayAudio(slide, true);
            }
            var volume = Mathf.Min(_groundTickCount / 10f, _crouchAmount, Flatten(velocity).magnitude / 10f);
            audioManager.SetVolume(slide, volume);
        }
        else
        {
            audioManager.StopAudio(slide);
        }

        if (!IsDashing)
        {
            var leanProjection = Vector3.Dot(_slideLeanVector, camera.transform.right);
            var roll = leanProjection * 15 * _crouchAmount;
            var rollspeed = Mathf.Abs(CameraRoll - roll) / Time.fixedDeltaTime;
            SetCameraRoll(roll, rollspeed);
            if (IsSliding)
            {
                ApplyFriction(f * SLIDE_FRICTION, 0, BASE_SPEED / 2);
                AirAccelerate(ref velocity, f);

                _slideLeanVector = Vector3.Lerp(_slideLeanVector, Flatten(velocity).normalized, f * 7);
            }
            else
            {
                ApplyFriction(f * GROUND_FRICTION, 0, BASE_SPEED / 3);
                Accelerate(Wishdir, BASE_SPEED / 2, GROUND_ACCELERATION, f);
            }
        }
        else
        {
            AirAccelerate(ref velocity, f, SLIDE_MOVEMENT_SCALE);
        }
    }

    public void AirMove(ref Vector3 velocity, float f)
    {
        GravityTick(f);
        _slideLeanVector = Vector3.zero;
        _airTickCount++;

        if (!jumpKitEnabled)
        {
            AirAccelerate(ref velocity, f);
            return;
        }

        if (PlayerInput.SincePressed(PlayerInput.MoveForward) == 1)
        {
            var wishdir = (transform.right * PlayerInput.GetAxisStrafeRight() + transform.forward).normalized;

            var maxTime = 0.5f;
            var minTime = 0.2f;
            var lurchTimer = Mathf.Clamp01(maxTime - ((PlayerInput.tickCount - _jumpTimestamp) * Time.fixedDeltaTime));
            var amt = Mathf.Min(lurchTimer / (maxTime - minTime), 1);
            var strength = 0.7f;
            var max = BASE_SPEED * 0.7f * amt;

            var beforespeed = Flatten(velocity).magnitude;
            var lurchdirection = Vector3.Lerp(Flatten(velocity).normalized, wishdir * 1.5f, strength) - Flatten(velocity).normalized;
            var lurch = Flatten(velocity).normalized * beforespeed + lurchdirection.normalized * max;

            if (lurch.magnitude > beforespeed)
            {
                lurch = lurch.normalized * beforespeed;
            }

            velocity.x = lurch.x;
            velocity.z = lurch.z;
        }

        if (rigidbody.SweepTest(Vector3.down, out var finish, FINISH_HOVER_DISTANCE, QueryTriggerInteraction.Ignore))
        {
            if (finish.collider.CompareTag("Finish"))
            {
                Game.EndTimer();
            }
        }

        // Lean in
        var movement = velocity + (_dashVector * _dashTime);
        var didHit = rigidbody.SweepTest(movement.normalized, out var hit, movement.magnitude * WALL_LEAN_PREDICTION_TIME, QueryTriggerInteraction.Ignore);

        var wallBuffering = IsDashing ? GROUND_JUMP_FORGIVENESS_TICKS : WALL_JUMP_FORGIVENESS_TICKS;

        var eatJump = false;
        var fromWall = 0f;

        if (didHit
            && Mathf.Abs(Vector3.Angle(Vector3.up, hit.normal) - 90) < WALL_VERTICAL_ANGLE_GIVE
            && CanCollide(hit.collider, false)
            && (hit.collider.gameObject != _lastWall || Vector3.Dot(Flatten(hit.normal).normalized, _wallNormal) < 0.7 || WALL_ALLOW_SAME_FACING))
        {
            fromWall = 1 - hit.distance / movement.magnitude / WALL_LEAN_PREDICTION_TIME;

            _wallNormal = Flatten(hit.normal).normalized;

            if (_wallLeanAmount < 1)
            {
                _wallLeanAmount = Mathf.Clamp01(_wallLeanAmount + Time.fixedDeltaTime / WALL_LEAN_PREDICTION_TIME);

                var easeOutSine = Mathf.Sin(_wallLeanAmount * Mathf.PI / 2);

                var normal = Flatten(hit.normal);
                var projection = Vector3.Dot(CrosshairDirection, new Vector3(-normal.z, normal.y, normal.x));

                var roll = WALL_LEAN_DEGREES * easeOutSine * -projection;
                var speed = Mathf.Abs(CameraRoll - roll) / Time.fixedDeltaTime;
                SetCameraRoll(roll, speed);
            }
            if (!ApproachingWall)
            {
                if (_cameraRotation < 0)
                {
                    weaponManager.EquippedGun.LeftWallStart();
                }
                else
                {
                    weaponManager.EquippedGun.RightWallStart();
                }
            }
            ApproachingWall = true;

            // Eat jump inputs if you are < jumpForgiveness away from the wall to not eat double jump
            if (WALL_LEAN_PREDICTION_TIME * (1 - fromWall) / Time.fixedDeltaTime < wallBuffering) eatJump = true;

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
        else
        {
            weaponManager.EquippedGun.WallStop();
            ApproachingWall = false;
            if (_wallLeanAmount > 0)
            {
                _wallLeanAmount = Mathf.Clamp01(_wallLeanAmount - Time.fixedDeltaTime / WALL_LEAN_PREDICTION_TIME);

                var leanOutProjection = Vector3.Dot(CrosshairDirection, new Vector3(-_wallNormal.z, _wallNormal.y, _wallNormal.x));

                var easeOut = _wallLeanCancelled ? Mathf.Sin(_wallLeanAmount * Mathf.PI / 2) : -(Mathf.Cos(Mathf.PI * _wallLeanAmount) - 1) / 2;

                var roll = WALL_LEAN_DEGREES * easeOut * -leanOutProjection;
                var speed = Mathf.Abs(CameraRoll - roll) / Time.fixedDeltaTime;
                SetCameraRoll(roll, speed);
            }
            else
            {
                _wallLeanCancelled = true;
            }
        }

        if (didHit && Vector3.Angle(Vector3.up, hit.normal) < GROUND_ANGLE && CanCollide(hit.collider))
        {
            // Eat jump inputs if you are < jumpForgiveness away from the ground to not eat double jump
            if (hit.distance / movement.magnitude / Time.fixedDeltaTime < GROUND_JUMP_FORGIVENESS_TICKS) eatJump = true;
        }

        var mod = 1 - Mathf.Min(fromWall * 2, 1);

        AirAccelerate(ref velocity, f, mod);

        if (eatJump)
        {
            if (PlayerInput.SincePressed(PlayerInput.Jump) < Mathf.Max(wallBuffering, GROUND_JUMP_FORGIVENESS_TICKS))
            {
                _jumpBuffered = 2 * Mathf.Max(wallBuffering, GROUND_JUMP_FORGIVENESS_TICKS) * Time.fixedDeltaTime;
            }
        }
        else PlayerJump();
    }

    // Returns the amount of speed gained from air strafing
    public float AirAccelerate(ref Vector3 velocity, float f, float accelMod = 1)
    {

        var airStrafeGains = 0f;

        var forward = transform.forward * PlayerInput.GetAxisStrafeForward();
        var accel = FORWARD_AIR_ACCELERATION * accelMod;
        if (Vector3.Dot(Flatten(velocity), forward) < 0)
        {
            accel = BACKWARD_AIR_ACCELERATION * accelMod;
        }
        var speed = Flatten(velocity).magnitude;
        velocity += forward * accel * f;
        if (speed < Flatten(velocity).magnitude)
        {
            var y = velocity.y;
            velocity = Flatten(velocity).normalized * speed;
            velocity.y = y;
        }
        /*var forwardspeed = Vector3.Dot(velocity, forward);
        var forwardaddspeed = Mathf.Abs(AIR_SPEED) - forwardspeed;
        if (forwardaddspeed > 0)
        {

            if (forwardaccel * f > forwardaddspeed)
                forwardaccel = forwardaddspeed / f;

            var addvector = forwardaccel * forward;

            var beforespeed = Flatten(velocity).magnitude;
            velocity += addvector * f;
            var afterspeed = Flatten(velocity).magnitude;
            if (afterspeed > AIR_SPEED && afterspeed > beforespeed)
            {
                var y = velocity.y;
                velocity = Flatten(velocity).normalized * beforespeed;
                velocity.y = y;
            }
        }*/

        if (PlayerInput.GetAxisStrafeRight() != 0)
        {
            var sideaccel = SIDE_AIR_ACCELERATION * accelMod;
            var right = transform.right * PlayerInput.GetAxisStrafeRight();
            var offset = velocity + right * AIR_SPEED;
            var angle = Mathf.Atan2(offset.z, offset.x) - Mathf.Atan2(velocity.z, velocity.x);

            var offsetAngle = Mathf.Atan2(right.z, right.x) - angle;
            right = new Vector3(Mathf.Cos(offsetAngle), 0, Mathf.Sin(offsetAngle));

            var rightspeed = Vector3.Dot(velocity, right);
            var rightaddspeed = Mathf.Abs(AIR_SPEED) - rightspeed;
            if (rightaddspeed > 0)
            {
                if (sideaccel * f > rightaddspeed)
                    sideaccel = rightaddspeed / f;

                var addvector = sideaccel * right;
                airStrafeGains += Mathf.Max(0, (velocity + addvector * f - velocity).magnitude);
                velocity += addvector * f;
            }
        }

        return airStrafeGains;
    }

    public void ApplyFriction(float f, float minimumSpeed = 0, float deceleration = 0)
    {
        /*float speed = Flatten(velocity).magnitude;
        if (speed != 0)
        {
            float drop = speed * f;
            velocity *= Mathf.Max(speed - drop, 0) / speed;
        }*/
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

    // Returns speed gain
    public float Accelerate(Vector3 wishdir, float speed, float acceleration, float f = 1)
    {
        var currentspeed = Vector3.Dot(velocity, wishdir.normalized);
        var addspeed = Mathf.Abs(speed) - currentspeed;

        if (addspeed <= 0)
            return 0f;

        var accelspeed = acceleration * f * speed;//Mathf.Lerp(currentspeed, speed, acceleration * f) - currentspeed;
        if (accelspeed > addspeed)
            accelspeed = addspeed;

        velocity += accelspeed * wishdir;

        return accelspeed;
    }

    public bool PlayerJump()
    {
        int sinceJump = PlayerInput.SincePressed(PlayerInput.Jump);
        if (sinceJump <= Mathf.Min(WALL_JUMP_FORGIVENESS_TICKS, GROUND_JUMP_FORGIVENESS_TICKS) || _jumpBuffered > 0)
        {
            if (_teleportTime > 0)
            {
                _jumpBuffered = Mathf.Min(WALL_JUMP_FORGIVENESS_TICKS, GROUND_JUMP_FORGIVENESS_TICKS) * Time.fixedDeltaTime;
                return false;
            }
            if (GrappleHooked)
            {
                DetachGrapple();
                PlayerInput.ConsumeBuffer(PlayerInput.Jump);
                _jumpBuffered = 0;
                return true;
            }

            if (PlayerInput.tickCount - _railTimestamp < COYOTE_TICKS)
            {
                if (_railTickCount > RAIL_COOLDOWN_TICKS)
                {
                    EndRail();
                    PlayerInput.ConsumeBuffer(PlayerInput.Jump);
                    _jumpBuffered = 0;
                }
                return true;
            }

            var wallJump = PlayerInput.tickCount - _wallTimestamp < COYOTE_TICKS;
            var groundJump = PlayerInput.tickCount - _groundTimestamp < COYOTE_TICKS;
            var coyoteJump = PlayerInput.tickCount - _wallTimestamp != 0 && PlayerInput.tickCount - _groundTimestamp != 0;
            _groundTimestamp = -COYOTE_TICKS;

            if (!groundJump && !wallJump && !DoubleJumpAvailable) return false;

            var jumpStamina = Mathf.Clamp01(Mathf.Max(_groundTickCount, _wallTickCount) / JUMP_STAMINA_RECOVERY_TICKS);
            var jumpHeight = Mathf.Lerp(MIN_JUMP_HEIGHT, MAX_JUMP_HEIGHT, jumpStamina);// * (IsSliding && !groundJump ? 0.6f : 1);

            audioManager.StopAudio(slide);
            audioManager.StopAudio(groundLand);
            if (wallJump)
            {
                // Jumping from wall
                _wallRecovery = WALL_AIR_ACCEL_RECOVERY;
                ApproachingWall = false;
                audioManager.PlayOneShot(jump);
                IsOnWall = false;
                IsSliding = false;
                _wallLeanCancelled = false;

                _wallTimestamp = -COYOTE_TICKS;
                _lastWall = _currentWall;

                var y = velocity.y;
                var velDirection = Flatten(velocity).normalized;
                var normal = _wallNormal;
                var jumpDirection = Flatten(Flatten(velDirection - normal * Vector3.Dot(velDirection, normal)).normalized + normal * WALL_JUMP_ANGLE).normalized;
                velocity = Mathf.Max(velocity.magnitude, BASE_SPEED) * jumpDirection;
                velocity += jumpDirection * WALL_JUMP_SPEED;

                velocity.y = y;
                var h = jumpHeight;
                if (IsDashing)
                {
                    CancelDash(ref h);
                }
                velocity.y = Mathf.Max(IsSliding ? h / 3 : h, velocity.y);

                if (_wallTickCount < WALL_JUMP_FORGIVENESS_TICKS)
                {
                    audioManager.PlayOneShot(viz);
                }
            }
            else
            {
                SetCameraRoll(0, CAMERA_ROLL_CORRECT_SPEED);

                if (!groundJump)
                {
                    DoubleJumpAvailable = false;
                    velocity.y = Mathf.Max(MAX_JUMP_HEIGHT, velocity.y);
                    StopDash();
                    audioManager.PlayOneShot(jumpair);
                    bonusGravityCooldownTicks = 10;
                    IsSliding = false;
                }
                else
                {
                    audioManager.PlayOneShot(jump);
                    bonusGravityCooldownTicks = 100;
                    IsOnGround = false;
                    var height = jumpHeight;
                    if (IsDashing)
                    {
                        CancelDash(ref height);
                    }
                    velocity.y = Mathf.Max(height, velocity.y);
                }
            }

            _jumpTimestamp = PlayerInput.tickCount;
            PlayerInput.ConsumeBuffer(PlayerInput.Jump);
            _jumpBuffered = 0;
            return true;
        }
        return false;
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}