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

    public Vector3 cameraPosition;
    public Vector3 velocity;

    public bool jumpKitEnabled = true;

    public enum Ability
    {
        GRAPPLE,
        DASH
    }

    private const float wallLandFriction = 10f;
    private const float wallCatchFriction = 10f;
    private const float wallSpeed = 16f;
    private const float wallAcceleration = 80f;
    private const float wallJumpSpeed = 10f;
    private const float wallJumpTrueSpeed = 3f;
    private const float wallAngleGive = 10f;

    private const float wallLeanDegrees = 20f;
    private const float wallLeanPreTime = 0.3f;

    private const float cameraRotationCorrectSpeed = 4f;

    private const float movementSpeed = 7f;
    private const float groundAcceleration = 80f;
    private const float groundFriction = 5f;
    private const float slideFriction = 0.5f;
    private const float landBoostSpeed = 16f;

    private const float airAcceleration = 40f;
    private const float airSpeed = 2f;
    private const float airCorrectionForce = 10f;
    private const float airLowSpeedGroundMultiplier = 0.65f;

    private const float groundAngle = 45;

    private const float railSpeed = 20f;
    private const float railAcceleration = 24f;
    private const int railCooldownTicks = 40;

    private const float dashSpeed = 15f;
    private const float dashDistance = 15f;

    private const float dashCancelTempSpeed = 27f;
    private const float dashCancelSpeed = 8f;
    private const float dashCancelPotentialMultiplier = 1.65f;

    private const float excededFriction = 4;

    private const float gravity = 0.2f;
    private const float fallSpeed = 100f;

    private const float jumpHeight = 12f;
    private const int coyoteTicks = 20;
    private const int jumpForgiveness = 10;
    private const float jumpGracePeriod = 0.5f;
    private const float jumpGraceBonusAccel = 40;

    private const float grappleControlAcceleration = 6f;
    private const float grappleDistance = 30f;
    private const float grappleAcceleration = 25f;
    private const float grappleTopSpeed = 80f;

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

    public Vector3 CollisionImpulse { get; set; }

    public bool approachingWall;

    private List<AbilityContainer> currentAbilities = new List<AbilityContainer>();

    struct AbilityContainer
    {
        public Ability ability;
        public GameObject dot;
    }

    private Vector3 _wallNormal;
    private int _wallTimestamp = -100000;
    private int _wallTickCount;
    private int _cancelLeanTickCount;
    private float _currentLean;

    public Vector3 GroundNormal { get; set; }

    private int _railTimestamp = -100000;
    private int _railCooldownTimestamp = -100000;
    private int _railDirection;
    private Vector3 _railLeanVector;
    private GameObject _lastRail;
    private Rail _currentRail;

    private Vector3 _grappleAttachPosition;

    private float _dashAddPotential;
    private float _dashAddSpeed;
    private float _excededSpeed;
    private int _dashTicks;

    private int _groundTimestamp = -100000;
    private bool _landed;
    private Vector3 _previousPosition;
    private float _crouchAmount;
    private int _sinceJumpCounter;
    private Vector3 _slideLeanVector;
    private float _cameraRotation;
    private float _cameraRotationSpeed;
    private float _motionInterpolationDelta;
    private float _crosshairRotation;

    private bool _wasSliding;

    public float YawIncrease { get; set; }

    public float CameraRoll { get; set; }

    public static bool DoubleJumpAvailable { get; set; }

    public bool IsGrounded { get; set; }

    public bool IsSliding
    {
        get { return (Flatten(velocity).magnitude > movementSpeed + 1 || (!IsGrounded && Flatten(velocity).magnitude >= movementSpeed - 1) || IsOnWall || IsOnRail || GrappleHooked) && jumpKitEnabled; }
    }

    public bool IsOnWall { get; set; }

    public MeshCollider CurrentCollider { get; set; }

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
            return Vector3.Lerp(_previousPosition, transform.position,
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

        Yaw += transform.rotation.eulerAngles.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        grappleTether.useWorldSpace = false;
        grappleTether.enabled = false;
        grappleDuring.volume = 0;

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
            CurrentCollider = crouchingCollider;
            if (_crouchAmount < 1) _crouchAmount += Time.deltaTime * 6;
        }
        else
        {
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
        if (!IsGrounded) transform.position -= Vector3.up * crouchChange;

        position.y -= 0.6f * _crouchAmount;
        camera.transform.position = InterpolatedPosition + position;

        var targetFOV = Flatten(velocity).magnitude + (100 - movementSpeed);
        targetFOV = Mathf.Max(targetFOV, 100);
        targetFOV = Mathf.Min(targetFOV, 120);

        camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, targetFOV, Time.deltaTime * 5);

        if (DoubleJumpAvailable)
        {
            _crosshairRotation = Mathf.Lerp(_crosshairRotation, 0, Time.deltaTime * 20);
        }
        else
        {
            _crosshairRotation = Mathf.Lerp(_crosshairRotation, 45, Time.deltaTime * 20);
        }
        crosshair.transform.rotation = Quaternion.Euler(new Vector3(0, 0, _crosshairRotation));

        if (Input.GetKeyDown(PlayerInput.SecondaryInteract) && Time.timeScale > 0 && currentAbilities.Count > 0)
        {
            var container = currentAbilities[currentAbilities.Count - 1];
            if (container.ability == Ability.GRAPPLE)
            {
                if (Physics.SphereCast(camera.transform.position, 2, CrosshairDirection, out var sphere, 150, 1, QueryTriggerInteraction.Ignore) && !sphere.collider.CompareTag("Uninteractable") && !sphere.collider.CompareTag("Kill Block"))
                {
                    AttachGrapple(sphere.point);
                    SpendAbility();
                }
            }
            if (container.ability == Ability.DASH)
            {
                Dash(CrosshairDirection);
                SpendAbility();
            }
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

        if (!IsOnWall) _wallTickCount = 0;

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

        if (!IsOnWall && !IsGrounded) _landed = false;

        var movement = velocity * Time.fixedDeltaTime;
        _previousPosition = transform.position;

        float movementSqrMagnitude = movement.sqrMagnitude;

        if (movementSqrMagnitude > Mathf.Pow(Mathf.Min(Mathf.Min(CurrentCollider.bounds.extents.x, CurrentCollider.bounds.extents.y), CurrentCollider.bounds.extents.z), 2))
        {
            float movementMagnitude = Mathf.Sqrt(movementSqrMagnitude);

            var center = CurrentCollider.bounds.center;
            var radius = Mathf.Max(CurrentCollider.bounds.extents.x, CurrentCollider.bounds.extents.z);
            var start = center + Vector3.up * (CurrentCollider.bounds.extents.y - radius);
            var end = center + Vector3.down * (CurrentCollider.bounds.extents.y - radius);

            if (Physics.CapsuleCast(start, end, radius, movement, out RaycastHit hit, movementMagnitude, 1, QueryTriggerInteraction.Ignore))
            {
                movement -= movement.normalized * (movementMagnitude - hit.distance - radius);
            }
        }

        transform.position += movement;

        var hold = 0.05f;
        if (IsGrounded)
        {
            transform.position += Vector3.down * hold;
        }
        if (IsOnWall)
        {
            transform.position -= _wallNormal * hold;
        }

        var wasOnWall = IsOnWall;
        IsGrounded = false;
        IsOnWall = false;
        CollisionImpulse = new Vector3();

        var overlap = Physics.OverlapBox(CurrentCollider.bounds.center, CurrentCollider.bounds.extents);
        foreach (var collider in overlap)
        {
            if (collider.gameObject == gameObject) continue;
            if (collider.CompareTag("Player")) continue;
            if (Physics.ComputePenetration(CurrentCollider, CurrentCollider.transform.position, CurrentCollider.transform.rotation, collider, collider.transform.position, collider.transform.rotation, out var direction, out var distance))
            {
                if (collider.isTrigger)
                {
                    OnTrigger(collider);
                    continue;
                }
                var normal = direction.normalized;

                var projection = Vector3.Dot(velocity, -normal);
                var angle = Vector3.Angle(Vector3.up, normal);

                if (angle < groundAngle)
                {
                    IsGrounded = true;
                    GroundNormal = normal;
                }

                if (collider.CompareTag("Instant Kill Block") && !Game.Level.LevelCompleted)
                {
                    Game.RestartLevel();
                }

                if (collider.CompareTag("Kill Block") && IsGrounded && !Game.Level.LevelCompleted)
                {
                    Game.RestartLevel();
                }

                if (!collider.CompareTag("Uninteractable") && Mathf.Abs(angle - 90) < wallAngleGive && !IsGrounded && jumpKitEnabled)
                {
                    if (wasOnWall || Vector3.Dot(Flatten(velocity).normalized, Flatten(normal).normalized) < 0)
                    {
                        // Wall Grab
                        _wallNormal = Flatten(normal).normalized;
                        IsOnWall = true;
                    }
                }

                if (!_landed && (IsOnWall || IsGrounded))
                {
                    _landed = true;
                    if (IsGrounded) source.PlayOneShot(groundLand);
                    DoubleJumpAvailable = true;
                    if (jumpKitEnabled && Flatten(velocity).magnitude >= movementSpeed - 1 && Vector3.Dot(Wishdir, Flatten(velocity).normalized) > -0.5)
                    {
                        Accelerate(Flatten(velocity).normalized, landBoostSpeed, landBoostSpeed);
                    }
                }

                var speed = Flatten(velocity);

                if (IsOnWall)
                {
                    var flat = Vector3.Dot(velocity, -Flatten(normal));
                    if (flat > 0) velocity += normal * flat;
                }
                else if (projection > 0)
                {
                    CollisionImpulse = normal * projection;
                    velocity += normal * projection;
                }

                if (IsGrounded)
                {
                    transform.position += Vector3.up * (Vector3.Dot(direction, Vector3.up) * distance);
                    if (Flatten(velocity).magnitude < speed.magnitude)
                    {
                        velocity.x = speed.x;
                        velocity.z = speed.z;
                    }
                }
                else transform.position += direction * distance;

            }
        }
        _wasSliding = IsSliding;
    }

    private void OnTrigger(Collider other)
    {
        // Rail Grab
        if (other.CompareTag("Rail") && (PlayerInput.tickCount - _railCooldownTimestamp > railCooldownTicks ||
                                         other.transform.parent.gameObject != _lastRail))
        {
            _lastRail = other.transform.parent.gameObject;
            SetRail(other.gameObject.transform.parent.gameObject.GetComponent<Rail>());
        }

        if (other.CompareTag("Finish"))
        {
            Game.Level.EndTimer();
        }
    }

    public void AddAbility(Ability ability)
    {
        AbilityContainer container;
        container.ability = ability;

        var dot = Instantiate(abilityDot, Game.Canvas.transform);
        if (ability == Ability.GRAPPLE)
        {
            dot.GetComponent<Image>().color = Color.blue;
        }
        if (ability == Ability.DASH)
        {
            dot.GetComponent<Image>().color = Color.red;
        }
        if (currentAbilities.Count > 0)
        {
            currentAbilities[currentAbilities.Count - 1].dot.transform.SetParent(dot.transform);
            currentAbilities[currentAbilities.Count - 1].dot.transform.localPosition = new Vector3(0, -5, 0);
        }

        container.dot = dot;
        currentAbilities.Add(container);
    }

    private void SpendAbility()
    {
        if (currentAbilities.Count == 0) return;
        var container = currentAbilities[currentAbilities.Count - 1];
        if (currentAbilities.Count > 1)
        {
            var child = container.dot.transform.GetChild(0);
            child.SetParent(Game.Canvas.transform);
            child.transform.position += new Vector3(0, 5, 0);
        }
        Destroy(container.dot);
        currentAbilities.RemoveAt(currentAbilities.Count - 1);
    }

    public void Dash(Vector3 wishdir)
    {
        StopDash();
        IsDashing = true;
        source.Play();

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
            velocity += Flatten(velocity).normalized * dashCancelSpeed;
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

    public void AttachGrapple(Vector3 position)
    {
        if (!enabled) return;
        source.PlayOneShot(grappleAttach);
        if (GrappleHooked) return;
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
        SetCameraRotation(projection, 6);

        grappleDuring.pitch = velocity.magnitude / 30f;

        var yankProjection = Vector3.Dot(velocity, towardPoint);
        if (yankProjection < 0) velocity -= towardPoint * yankProjection;

        Accelerate(towardPoint, grappleTopSpeed, f * grappleAcceleration);
        var magnitude = velocity.magnitude;
        velocity += Wishdir * grappleControlAcceleration * f;
        velocity += CrosshairDirection * grappleControlAcceleration * f;
        velocity = velocity.normalized * magnitude;

        if (Vector3.Dot(towardPoint, CrosshairDirection) < 0)
        {
            DetachGrapple();
        }

        if (Vector3.Distance(position, transform.position) > grappleDistance)
        {
            var mag = velocity.magnitude;
            velocity = Vector3.Lerp(velocity, towardPoint * velocity.magnitude, f / 5);
            velocity = velocity.normalized * mag;
        }
        Jump();
    }

    public void WallMove(float f)
    {
        if (Jump()) return;

        if (_wallTickCount == 0)
        {
            source.PlayOneShot(groundLand);
            ApplyFriction(wallLandFriction * f);
            if (jumpKitEnabled && Flatten(velocity).magnitude >= movementSpeed - 1 && Vector3.Dot(Wishdir, Flatten(velocity).normalized) > -0.5)
            {
                Accelerate(Flatten(velocity).normalized, landBoostSpeed, landBoostSpeed);
            }
        }

        _wallTickCount++;

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

        Accelerate(-_wallNormal, fallSpeed, gravity * f);
    }

    public void Gravity(float f)
    {
        if (!IsDashing) velocity.y = Mathf.Lerp(velocity.y, -fallSpeed, gravity * f);
    }

    public void GroundMove(float f)
    {
        if (Jump()) return;

        rollSound.pitch = Mathf.Min(Mathf.Max(velocity.magnitude / 20, 1), 2);
        rollSound.volume = Mathf.Min(velocity.magnitude / 30, 1);

        Gravity(f);
        DoubleJumpAvailable = true;
        if (IsSliding)
        {
            _slideLeanVector = Vector3.Lerp(_slideLeanVector, Flatten(velocity).normalized, f * 4);
            var leanProjection = Vector3.Dot(_slideLeanVector, camera.transform.right);
            SetCameraRotation(leanProjection * 15, 6);

            Accelerate(Wishdir, 0, airAcceleration * f);
            ApplyFriction(f * slideFriction, 0, movementSpeed);
        }
        else
        {
            _slideLeanVector = Flatten(velocity).normalized;
            GroundAccelerate(f);
        }

        if (!_wasSliding && IsSliding)
        {
            Accelerate(Flatten(velocity).normalized, landBoostSpeed, landBoostSpeed);
        }
    }

    public void GroundAccelerate(float f)
    {
        ApplyFriction(f * groundFriction, 0, movementSpeed);
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
        Gravity(f);
        _slideLeanVector = Flatten(velocity).normalized;

        AirAccelerate(f);

        rollSound.volume = 0;

        Jump();

        if (!jumpKitEnabled) return;

        // Lean in
        var center = CurrentCollider.bounds.center;
        var radius = Mathf.Max(CurrentCollider.bounds.extents.x, CurrentCollider.bounds.extents.z);
        var start = center + Vector3.up * (CurrentCollider.bounds.extents.y - radius);
        var end = center + Vector3.down * (CurrentCollider.bounds.extents.y - radius);

        var didHit = Physics.CapsuleCast(start, end, radius, velocity.normalized, out RaycastHit hit, velocity.magnitude * wallLeanPreTime, 1, QueryTriggerInteraction.Ignore);

        if (didHit && Math.Abs(Vector3.Angle(Vector3.up, hit.normal) - 90) < wallAngleGive && !hit.collider.CompareTag("Uninteractable"))
        {
            if (!approachingWall) approachingWall = true;

            var rotation = 1 - (hit.distance / velocity.magnitude) / wallLeanPreTime;
            _currentLean = rotation;

            rotation *= 2 - rotation;

            var normal = Flatten(hit.normal);
            var projection = Vector3.Dot(CrosshairDirection, new Vector3(-normal.z, normal.y, normal.x));
            SetCameraRotation(wallLeanDegrees * rotation * -projection, 15);
            _cancelLeanTickCount = 0;
        }
        else
        {
            if (approachingWall)
            {
                _cancelLeanTickCount++;
            }
            if (_cancelLeanTickCount >= 5)
            {
                approachingWall = false;
                SetCameraRotation(0, cameraRotationCorrectSpeed);
                _currentLean = 0;
            }
        }
    }

    public void AirAccelerate(float f)
    {
        var groundMod = 1 - Mathf.Min(Flatten(velocity).magnitude / (movementSpeed - 1), 1);
        GroundAccelerate(f * groundMod * airLowSpeedGroundMultiplier);
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

        accel *= 1 - _currentLean;

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
                velocity += Flatten(velocity).normalized * wallJumpTrueSpeed;

                CancelDash();

                DoubleJumpAvailable = true;

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
                if (!jumpKitEnabled || IsDashing) return false;
                DoubleJumpAvailable = false;

                Accelerate(Flatten(velocity).normalized, movementSpeed, movementSpeed);
            }

            if (groundJump || railJump)
            {
                DoubleJumpAvailable = true;
                CancelDash();
            }

            if (groundJump)
            {
                source.PlayOneShot(jump);
            }

            velocity.y = Mathf.Max(jumpHeight, velocity.y + (railJump || groundJump ? jumpHeight : 0));

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