using System;
using System.Collections.Generic;
using UnityEngine;
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

    private const float wallCatchFriction = 10f;
    private const float wallSpeed = 16f;
    private const float wallAcceleration = 80f;
    private const float wallJumpSpeed = 8f;
    private const float wallJumpTrueSpeed = 5f;
    private const float wallAngleGive = 10f;
    private const float wallStamina = 60f;
    private const float wallEndBoostSpeed = 1f;

    private const float wallLeanDegrees = 20f;
    private const float wallLeanPreTime = 0.3f;

    private const float landFriction = 10f;
    private const float slideFriction = 0.5f;

    private const float cameraRotationCorrectSpeed = 4f;

    private const float movementSpeed = 16f;
    private const float groundAcceleration = 80f;
    private const float groundFriction = 3f;
    private const float stepHeight = 0.8f;

    private const float airAcceleration = 80f;
    private const float airSpeed = 2f;
    private const float airCorrectionForce = 10f;
    private const float airLowSpeedGroundMultiplier = 0.65f;

    private const float surfAirAccelMultiplier = 50f;

    private const float groundAngle = 45;

    private const float railSpeed = 40f;
    private const float railAcceleration = 20f;
    private const int railCooldownTicks = 40;

    private const float dashSpeed = 30f;
    private const float dashDownLimit = 0.5f;
    private const float dashUpLimit = 0.1f;
    private const int dashCooldown = 400;

    private const float dashCancelTempSpeed = 30f;
    private const float dashCancelSpeed = 3f;
    private const float dashCancelPotentialMultiplier = 1.65f;
    private const float dashCancelJumpReduction = 2f;

    private const float excededFriction = 4;

    private const float gravity = 22f;

    private const float jumpHeight = 12f;
    private const int coyoteTicks = 20;
    private const int jumpForgiveness = 10;
    private const float jumpGracePeriod = 0.5f;
    private const float jumpGraceBonusAccel = 0;

    private const float grappleControlAcceleration = 30f;
    private const float grappleDistance = 25f;
    private const float grappleAcceleration = 20f;
    private const float grappleTopSpeed = 60f;
    private const float grappleCorrectionAcceleration = 0.02f;

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

    public LineRenderer grappleTether;
    public float grappleYOffset;
    public bool GrappleHooked { get; set; }

    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public float LookScale { get; set; }

    public Vector3 CollisionImpulse { get; set; }

    public Collider CurrentCollider { get; private set; }

    public bool approachingWall;

    private Vector3 _wallNormal;
    private int _wallTimestamp = -100000;
    private int _wallTickCount;
    private float _wallStamina;
    private int _groundTickCount;
    private int _cancelLeanTickCount;

    private int _railTimestamp = -100000;
    private int _railCooldownTimestamp = -100000;
    private int _railDirection;
    private Vector3 _railLeanVector;
    private GameObject _lastRail;
    private Rail _currentRail;

    private int _dashTimestamp = -100000;

    private Vector3 _grappleAttachPosition;

    private float _dashBeforeSpeed;
    private float _excededSpeed;

    private int _groundTimestamp = -100000;
    private Vector3 _previousPosition;
    private float _crouchAmount;
    private int _sinceJumpCounter;
    private Vector3 _slideLeanVector;
    private float _cameraRotation;
    private float _cameraRotationSpeed;
    private float _motionInterpolationDelta;
    private float _crosshairRotation;
    private bool _isDownColliding;

    public float YawIncrease { get; set; }

    public float CameraRoll { get; set; }

    public static bool DoubleJumpAvailable { get; set; }
    public static bool DashAvailable { get; set; }

    public float Gravity { get { return gravity * (1 - Mathf.Min(1, _excededSpeed * 2 / dashSpeed)); } }

    public bool IsGrounded { get; set; }

    public bool IsSliding
    {
        get { return (Flatten(velocity).magnitude > movementSpeed + 1 || (!IsGrounded && Flatten(velocity).magnitude >= movementSpeed - 1) || IsOnWall || IsOnRail || GrappleHooked || (_isDownColliding && !IsGrounded)) && jumpKitEnabled; }
    }

    public bool IsOnWall { get; set; }

    public bool IsDashing { get { return _excededSpeed > 0; } }

    public bool IsStrafing { get { return PlayerInput.GetAxisStrafeRight() != 0 && PlayerInput.GetAxisStrafeForward() == 0; } }

    public bool IsOnRail { get { return _currentRail != null; } }

    public Vector3 Wishdir { get; set; }

    public Vector3 CrosshairDirection { get; set; }

    public Vector3 InterpolatedPosition { get { return Vector3.Lerp(_previousPosition, transform.position, _motionInterpolationDelta / Time.fixedDeltaTime); } }

    public void SetCameraRotation(float value, float speed)
    {
        _cameraRotation = value;
        _cameraRotationSpeed = speed;
    }

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
    }

    private void Update()
    {
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

        camera.transform.localRotation = Quaternion.Euler(new Vector3(Pitch + (HudMovement.rotationSlamVectorLerp.y / 2), 0, CameraRoll));
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
        if (!_isDownColliding) transform.position -= Vector3.up * crouchChange;

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

        if (PlayerInput.SincePressed(PlayerInput.SecondaryInteract) < jumpForgiveness && Time.timeScale > 0 && DashAvailable)
        {
            if (Dash(CrosshairDirection)) PlayerInput.ConsumeBuffer(PlayerInput.SecondaryInteract);
        }
    }

    private void FixedUpdate()
    {
        // Set Wishdir
        Wishdir = (transform.right * PlayerInput.GetAxisStrafeRight() + transform.forward * PlayerInput.GetAxisStrafeForward()).normalized;
        if (Wishdir.magnitude <= 0)
        {
            Wishdir = (transform.right * Input.GetAxis("Joy 1 X") + transform.forward * -Input.GetAxis("Joy 1 Y")).normalized;
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

        if (approachingWall)
        {
            _cancelLeanTickCount++;
        }
        if (_cancelLeanTickCount >= 5 && !IsOnWall)
        {
            approachingWall = false;
            SetCameraRotation(0, cameraRotationCorrectSpeed);
        }

        if (!IsOnWall) _wallTickCount = 0;
        if (!IsGrounded) _groundTickCount = 0;

        if (IsDashing)
        {
            var speed = Flatten(velocity).magnitude;
            var y = velocity.y;

            var newspeed = Mathf.Lerp(speed, speed - _excededSpeed, factor * excededFriction);

            _excededSpeed -= speed - newspeed;
            velocity = Flatten(velocity).normalized * newspeed;
            velocity.y = y;
            if (_excededSpeed < 1f) _excededSpeed = 0;
        }

        // Interpolation delta resets to 0 every fixed update tick, its used to measure the time since the last fixed update tick
        _motionInterpolationDelta = 0;

        var movement = velocity * Time.fixedDeltaTime;
        _previousPosition = transform.position;

        IsGrounded = false;
        IsOnWall = false;
        _isDownColliding = false;
        CollisionImpulse = new Vector3();
        var iterations = 0;

        var hold = 0.1f;
        if (IsGrounded)
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

                var stepCheck = StepCheck(hit.normal, transform.position, hit.collider);
                if (stepCheck > 0)
                {
                    transform.position += Vector3.up * (stepHeight - stepCheck);
                } else
                {
                    var movementProjection = Vector3.Dot(movement, -hit.normal);
                    if (movementProjection > 0) movement += hit.normal * movementProjection;
                    OnCollision(hit.normal, hit.collider);
                }
            }
            else
            {
                if (Physics.Raycast(transform.position, movement.normalized, out var ray, movement.magnitude, 1, QueryTriggerInteraction.Ignore))
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

                var stepCheck = StepCheck(direction, transform.position, collider);
                if (stepCheck > 0)
                {
                    direction = Vector3.up;
                    distance = stepHeight - stepCheck;
                } else
                {
                    OnCollision(direction, collider);
                }
                transform.position += direction * distance;
            }
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

    private float StepCheck(Vector3 normal, Vector3 origin, Collider collider = null)
    {
        var stepCheck = origin + (-Flatten(normal).normalized * 0.51f) + (Vector3.down * (1 - stepHeight));
        if (Flatten(normal).normalized.magnitude > 0.05f
            && Physics.Raycast(stepCheck, Vector3.down, out var rayHit, stepHeight, 1, QueryTriggerInteraction.Ignore)
            && Vector3.Angle(rayHit.normal, Vector3.up) < groundAngle
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
        if (collider.CompareTag("Kill Block") && IsGrounded)
        {
            Game.RestartLevel();
        }

        var velocityProjection = Vector3.Dot(velocity, -normal);
        if (velocityProjection > 0)
        {
            velocity += normal * velocityProjection;
            CollisionImpulse = normal * velocityProjection;
        }

        var angle = Vector3.Angle(Vector3.up, normal);

        if (angle < groundAngle)
        {
            IsGrounded = true;
        }

        if (!collider.CompareTag("Uninteractable") && Mathf.Abs(angle - 90) < wallAngleGive && !IsGrounded && jumpKitEnabled)
        {
            _wallNormal = Flatten(normal).normalized;
            IsOnWall = true;
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
            Game.Level.EndTimer();
        }

        if (other.CompareTag("Shockwave"))
        {
            var towardCenter = (other.transform.position - transform.position).normalized;
            var projection = Vector3.Dot(velocity, towardCenter);
            if (projection > 0) velocity -= towardCenter * projection;

            float power = 55f;

            Game.Level.player.Accelerate(-towardCenter, power, power);
            Destroy(other.gameObject);
        }
    }

    public bool Dash(Vector3 wishdir)
    {
        if (Environment.TickCount - _dashTimestamp < dashCooldown) return false;
        _dashTimestamp = Environment.TickCount;
        var wishY = wishdir.y;
        wishdir = Flatten(wishdir).normalized;
        StopDash();
        source.Play();
        velocity.y = 0;
        var projection = Vector3.Dot(velocity, wishdir);
        velocity = wishdir.normalized * Mathf.Max(projection, 0);
        if (velocity.magnitude < movementSpeed) velocity = wishdir.normalized * movementSpeed;
        DashAvailable = false;

        _dashBeforeSpeed = Flatten(velocity).magnitude;

        wishdir.y = Mathf.Max(-Mathf.Abs(dashDownLimit), Mathf.Min(Mathf.Abs(dashUpLimit), wishY));
        velocity = wishdir.normalized * (velocity.magnitude + dashSpeed);
        Gun.forwardChange += 2;

        _excededSpeed += Mathf.Max(Flatten(velocity).magnitude - _dashBeforeSpeed, 0);
        return true;
    }

    public bool CancelDash()
    {
        if (StopDash())
        {
            velocity += Flatten(velocity).normalized * dashCancelSpeed;
            var beforeSpeed = Flatten(velocity).magnitude;
            velocity += Flatten(velocity).normalized * dashCancelTempSpeed;
            _excededSpeed += Flatten(velocity).magnitude - beforeSpeed;
            velocity.y *= dashCancelPotentialMultiplier;
            source.PlayOneShot(wallKick);
            _dashTimestamp = Environment.TickCount;
            return true;
        }
        return false;
    }

    public bool StopDash()
    {
        if (IsDashing)
        {
            var y = velocity.y;
            velocity = Flatten(velocity).normalized * (Flatten(velocity).magnitude - _excededSpeed);
            velocity.y = y;
            _excededSpeed = 0;
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

        DoubleJumpAvailable = true;
        DashAvailable = true;

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
        velocity = velocity.magnitude * Vector3.Lerp(railVector, correctionVector, f * 20).normalized;

        Accelerate(velocity.normalized, railSpeed, f * railAcceleration);
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
        DoubleJumpAvailable = true;
    }

    public void GrappleMove(float f)
    {
        var position = _grappleAttachPosition;

        if (!grappleTether.enabled) grappleTether.enabled = true;

        rollSound.volume = 0;

        if (DoubleJumpAvailable) DoubleJumpAvailable = false;

        var list = new List<Vector3> { new Vector3(0, grappleYOffset, 0), camera.transform.InverseTransformPoint(position) };

        grappleTether.positionCount = list.Count;
        grappleTether.SetPositions(list.ToArray());

        var towardPoint = (position - transform.position).normalized;
        var velocityProjection = Vector3.Dot(velocity, towardPoint);
        var tangentVector = (velocity + towardPoint * -velocityProjection).normalized;

        var swingProjection = Mathf.Max(0, Vector3.Dot(velocity, Vector3.Lerp(towardPoint, tangentVector, 0.5f)));

        var projection = Mathf.Sqrt(swingProjection) * Vector3.Dot(-transform.right, towardPoint) * 6;
        SetCameraRotation(projection, 2);

        grappleDuring.pitch = velocity.magnitude / 30f;

        if (velocityProjection < 0) velocity -= towardPoint * velocityProjection;

        if (velocity.magnitude < 0.05f) Accelerate(towardPoint, grappleTopSpeed, f * grappleAcceleration);
        Accelerate(velocity.normalized, grappleTopSpeed, f * grappleAcceleration);
        var magnitude = velocity.magnitude;
        velocity += CrosshairDirection * grappleControlAcceleration * f;
        velocity += Wishdir * grappleControlAcceleration * f;
        velocity = velocity.normalized * magnitude;

        var target = position + tangentVector * grappleDistance;
        var direction = Vector3.Lerp(velocity.normalized, (target - transform.position).normalized, grappleCorrectionAcceleration);
        velocity = velocity.magnitude * direction.normalized;

        if (Vector3.Dot(towardPoint, CrosshairDirection) < 0)
        {
            DetachGrapple();
        }
        Jump();
    }

    public void WallMove(float f)
    {
        DoubleJumpAvailable = true;
        if (Jump()) return;

        if (_wallTickCount == 0)
        {
            source.PlayOneShot(groundLand);
            _wallStamina = wallStamina;
            ApplyFriction(landFriction * f);
        }
        _wallTickCount++;

        rollSound.pitch = Mathf.Min(Mathf.Max(velocity.magnitude / 20, 1), 2);
        rollSound.volume = Mathf.Min(velocity.magnitude / 30, 1);

        var normal = Flatten(_wallNormal);

        var projection = Vector3.Dot(CrosshairDirection, new Vector3(-normal.z, normal.y, normal.x));

        SetCameraRotation(wallLeanDegrees * -projection * (_wallStamina / wallStamina), 8);

        if (_wallStamina <= 0)
        {
            _wallStamina = 0;
            IsOnWall = false;
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

        var direction = new Vector3(_wallNormal.z, 0, -_wallNormal.x);
        if (Mathf.Abs(Vector3.Dot(direction, Wishdir)) > 0.5)
        {
            if (Vector3.Angle(Wishdir, direction) < 90)
                Accelerate(direction, wallSpeed, wallAcceleration * f);
            else
                Accelerate(-direction, wallSpeed, wallAcceleration * f);
        }

        if (Flatten(velocity).magnitude < wallSpeed)
        {
            ApplyFriction(f * groundFriction);
        }

        Accelerate(-_wallNormal, 10, Gravity * f);
    }

    public void GravityTick(float f)
    {
        velocity.y -= Gravity * f;
    }

    public void GroundMove(float f)
    {
        DoubleJumpAvailable = true;
        DashAvailable = true;
        if (Jump()) return;

        if (_groundTickCount == 0)
        {
            source.PlayOneShot(groundLand);
        }
        _groundTickCount++;

        rollSound.pitch = Mathf.Min(Mathf.Max(velocity.magnitude / 20, 1), 2);
        rollSound.volume = Mathf.Min(velocity.magnitude / 30, 1);

        GravityTick(f);
        if (IsSliding)
        {
            _slideLeanVector = Vector3.Lerp(_slideLeanVector, Flatten(velocity).normalized, f * 4);
            var leanProjection = Vector3.Dot(_slideLeanVector, camera.transform.right);
            SetCameraRotation(leanProjection * 15, 6);

            Accelerate(Wishdir, airSpeed, airAcceleration * f);
            ApplyFriction(slideFriction * f);
        }
        else
        {
            _slideLeanVector = Flatten(velocity).normalized;
            GroundAccelerate(f);
        }
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

        // Lean in
        var didHit = rigidbody.SweepTest(velocity.normalized, out var hit, velocity.magnitude * wallLeanPreTime, QueryTriggerInteraction.Ignore);

        var eatJump = false;
        var currentLean = 0f;

        if (didHit
            && Math.Abs(Vector3.Angle(Vector3.up, hit.normal) - 90) < wallAngleGive
            && StepCheck(hit.normal, transform.position + (velocity.normalized * hit.distance)) == 0
            && CanCollide(hit.collider, false))
        {
            if (!approachingWall) approachingWall = true;

            currentLean = 1 - hit.distance / velocity.magnitude / wallLeanPreTime;

            // Eat jump inputs if you are < jumpForgiveness away from the wall to not eat double jump
            if (wallLeanPreTime * (1 - currentLean) / Time.fixedDeltaTime < jumpForgiveness) eatJump = true;

            var curve = currentLean * (2 - currentLean);

            var normal = Flatten(hit.normal);
            var projection = Vector3.Dot(CrosshairDirection, new Vector3(-normal.z, normal.y, normal.x));
            SetCameraRotation(wallLeanDegrees * curve * -projection, 15);
            _cancelLeanTickCount = 0;
        }
        else if (didHit && Vector3.Angle(Vector3.up, hit.normal) < groundAngle && CanCollide(hit.collider))
        {
            // Eat jump inputs if you are < jumpForgiveness away from the ground to not eat double jump
            if (hit.distance / velocity.magnitude / Time.fixedDeltaTime < jumpForgiveness) eatJump = true;
        }

        AirAccelerate(f, 1 - currentLean);

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
        var groundMod = 1 - Mathf.Min(Flatten(velocity).magnitude / (movementSpeed - 1), 1);
        if (!_isDownColliding) GroundAccelerate(f * groundMod * airLowSpeedGroundMultiplier, 0);
        f *= 1 - groundMod;

        var speed = Flatten(velocity).magnitude;

        if (groundMod == 0)
        {
            var magnitude = velocity.magnitude;
            var push = Flatten(velocity.normalized).normalized * Flatten(CrosshairDirection).magnitude;
            push.y = CrosshairDirection.y;
            velocity += push * airCorrectionForce * f;
            velocity = velocity.normalized * magnitude;
            if (Flatten(velocity).magnitude > speed && velocity.y < 0)
            {
                velocity = velocity.normalized * (magnitude * speed / Flatten(velocity).magnitude);
            }
        }

        var ticksPerSecond = 1 / Time.fixedDeltaTime;
        var sinceJump = 1 - Mathf.Min(_sinceJumpCounter / (ticksPerSecond * jumpGracePeriod), 1);

        var accel = airAcceleration + (sinceJump * jumpGraceBonusAccel);

        if (_isDownColliding) accel *= surfAirAccelMultiplier;
        accel *= accelMod;

        var currentspeed = Vector3.Dot(velocity - (Flatten(velocity).normalized * _excededSpeed), Wishdir);
        var addspeed = Mathf.Abs(airSpeed) - currentspeed;

        if (addspeed > 0)
        {
            if (accel * f > addspeed)
                accel = addspeed / f;

            var addvector = accel * Wishdir;
            var backspeed = Vector3.Dot(addvector, -Flatten(velocity).normalized);
            if (backspeed > airAcceleration)
            {
                var x1 = backspeed;
                var x2 = airAcceleration;
                var y1 = addvector.magnitude;
                var y2 = (x2 * y1) / x1;

                addvector = addvector.normalized * y2;
            }
            velocity += addvector * f;
        }

        if (!IsStrafing && Vector3.Angle(Wishdir, velocity) < 90)
        {
            speed = Flatten(velocity).magnitude;
            var y = velocity.y;
            velocity += Wishdir * f * accel;
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
        var currentspeed = Vector3.Dot(velocity - (Flatten(velocity).normalized * _excededSpeed), wishdir.normalized);
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

            if (!groundJump && !railJump && !wallJump && !DoubleJumpAvailable) return false;
            _sinceJumpCounter = 0;

            SetCameraRotation(0, cameraRotationCorrectSpeed);

            if (wallJump)
            {
                IsOnWall = false;
                _wallTimestamp = -coyoteTicks;

                velocity += _wallNormal * wallJumpSpeed;
                velocity += Flatten(velocity).normalized * wallJumpTrueSpeed;

                Accelerate(Vector3.up, 0, jumpHeight);
                velocity.y = Mathf.Max(jumpHeight, velocity.y);

                source.PlayOneShot(jump);
                rollSound.volume = 0;
                PlayerInput.ConsumeBuffer(PlayerInput.Jump);
                return true;
            }
            PlayerInput.ConsumeBuffer(PlayerInput.Jump);

            if (!groundJump && !railJump)
            {
                if (!jumpKitEnabled) return false;
                DoubleJumpAvailable = false;
            }

            var force = jumpHeight;
            if (groundJump || railJump)
            {
                if (CancelDash())
                {
                    force /= dashCancelJumpReduction;
                    if (railJump) source.PlayOneShot(trick);
                }
            }

            if (groundJump)
            {
                source.PlayOneShot(jump);
            }

            velocity.y = Mathf.Max(force, velocity.y + (railJump || groundJump ? force : 0));

            rollSound.volume = 0;
            IsGrounded = false;

            if (IsOnRail) EndRail();
            return true;
        }
        return false;
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}