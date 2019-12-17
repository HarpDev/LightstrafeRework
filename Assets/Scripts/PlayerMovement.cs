using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    public new Rigidbody rigidbody;
    public new Camera camera;
    public Collider standingHitbox;
    public Text wallkickDisplay;

    public Vector3 cameraPosition;
    public Vector3 velocity;

    /* Movement Stuff */
    public float deceleration = 10f;
    public float friction = 0.5f;
    public float wallFriction = 0.1f;
    public float groundAcceleration = 8f;
    public float groundTurnAcceleration = 50;
    public float railSpeed = 20f;
    public float airAcceleration = 800f;
    public float dashSpeed = 10f;
    public float dashDistance = 10f;
    public float dashFriction = 1f;
    public float stairHeight = 0.6f;
    public float gravity = 0.3f;
    public float movementSpeed = 11;
    public float jumpHeight = 12f;
    public float jumpCameraThunk = 5f;
    public float fallSpeed = 60f;
    public int wallAngleGive = 10;
    public float wallCatchThreshold = 30f;
    public float wallCatchFriction = 5f;
    public float wallSpeed = 12f;
    public float wallAcceleration = 8f;
    public float wallJumpSpeed = 3f;
    public int jumpForgiveness = 10;
    public float wallKickFriction = 7;
    public float grappleSwingForce = 30f;
    public float grappleDistance = 10f;
    public float slopeAngle = 45;
    public int coyoteTime = 20;
    public int railCooldown = 40;

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
    public int maxGrappleTimeMillis = 5000;
    public bool GrappleHooked { get; set; }

    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public float LookScale { get; set; }

    private bool _approachingWall;
    private int _groundTimestamp = -100000;
    private int _wallTimestamp = -100000;
    private int _railTimestamp = -100000;
    private int _railCooldownTimestamp = -100000;
    private int _wallTickCount;
    private int _wallJumpTimestamp;
    private Vector3 _wallNormal;
    private Vector3 _grappleAttachPosition;
    private Transform _grappleAttachTransform;
    private Vector3 _previousPosition;
    private Collision _previousCollision;
    private Vector3 _previousCollisionLocalPosition;
    private Vector3 _displacePosition;
    private float _crouchAmount;
    private int _sinceJumpCounter;
    private float _lastAirborneSpeed;
    private int _railDirection;
    private Vector3 _railLeanVector;
    private Vector3 _slideLeanVector;
    private GameObject _lastRail;
    private float _cameraRotation;
    private float _cameraRotationSpeed;
    private bool _smoothRotation;
    private float _motionInterpolationDelta;
    private readonly List<Vector3> _momentumBuffer = new List<Vector3>();
    private CurvedLineRenderer _currentRail;
    private float _currentDashSpeed;
    private float _dashStartSpeed;
    private int _cancelLeanTickCount;
    private bool _landed;
    private Vector3 _cameraPosition;
    private float _approachingWallDistance;

    public float CameraRoll { get; set; }

    public static bool DoubleJumpAvailable { get; set; }

    public bool IsGrounded { get; set; }

    public bool IsSliding
    {
        get { return (velocity.magnitude >= movementSpeed - 1 && IsGrounded) || IsOnRail; }
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

    public void SetCameraRotation(float value, float speed, bool smooth)
    {
        _cameraRotation = value;
        _cameraRotationSpeed = speed;
        _smoothRotation = smooth;
    }

    private void Awake()
    {
        LookScale = 1;

        _cameraPosition = camera.transform.position;

        Game.Level.StopTimer();
        Game.Level.ResetTimer();
        Yaw += transform.rotation.eulerAngles.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        wallkickDisplay.gameObject.SetActive(true);
        var c = wallkickDisplay.color;
        c.a = 0;
        wallkickDisplay.color = c;

        grappleTether.useWorldSpace = false;
        grappleTether.enabled = false;
        grappleDuring.volume = 0;
    }

    private void Update()
    {
        if (Cursor.visible) return;

        // Wallkick display fade out
        var c = wallkickDisplay.color;
        if (c.a > 0) c.a -= Time.deltaTime;
        wallkickDisplay.color = c;

        // Mouse motion
        Yaw = (Yaw + Input.GetAxis("Mouse X") * (Game.Sensitivity / 10) * LookScale) % 360f;
        Pitch -= Input.GetAxis("Mouse Y") * (Game.Sensitivity / 10) * LookScale;
        Pitch = Mathf.Max(Pitch, -90);
        Pitch = Mathf.Min(Pitch, 90);

        // This is where orientation is handled, the camera is only adjusted by the pitch, and the entire player is adjusted by yaw
        var cam = camera.transform;

        if (!_smoothRotation)
        {
            var speed = _cameraRotationSpeed * Time.deltaTime;
            if (Mathf.Abs(CameraRoll - _cameraRotation) > speed)
                CameraRoll += CameraRoll < _cameraRotation ? speed : -speed;
            else CameraRoll = _cameraRotation;
        }
        else CameraRoll = Mathf.Lerp(CameraRoll, _cameraRotation, Time.deltaTime * _cameraRotationSpeed);

        camera.transform.localRotation =
            Quaternion.Euler(new Vector3(Pitch + HudMovement.rotationSlamVectorLerp.y, 0, CameraRoll));
        CrosshairDirection = cam.forward;
        transform.rotation = Quaternion.Euler(0, Yaw, 0);

        // This value is used to calcuate the positions in between each fixedupdate tick
        _motionInterpolationDelta += Time.deltaTime;

        // Check for level restart
        if (Input.GetKeyDown(PlayerInput.RestartLevel)) Game.RestartLevel();

        var position = cameraPosition;
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
                    SetCameraRotation(0, 50, false);
                }
            }
        }
        if (camera.fieldOfView > 100 && !IsSliding && !IsDashing)
        {
            if (IsSliding)
                camera.fieldOfView -= Time.deltaTime * 30;
            else
                camera.fieldOfView -= Time.deltaTime * 60;
        }
        else if (camera.fieldOfView < 110 && (IsSliding || IsDashing))
        {
            if (IsSliding)
                camera.fieldOfView += Time.deltaTime * 30;
            else
                camera.fieldOfView += Time.deltaTime * 60;
        }

        position.y -= 0.6f * _crouchAmount;
        //camera.transform.position = InterpolatedPosition + position;
        _cameraPosition = Vector3.Lerp(_cameraPosition, InterpolatedPosition + position, Time.deltaTime * 20);
        camera.transform.position = _cameraPosition;

        if (Input.GetKeyDown(PlayerInput.PrimaryInteract))
        {
            if (!GrappleHooked)
            {
                if (Physics.Raycast(_cameraPosition, CrosshairDirection, out var grapple, 500, 1, QueryTriggerInteraction.Ignore))
                {
                    AttachGrapple(grapple.transform, grapple.point);
                }
            }
        }
        if (!Input.GetKey(PlayerInput.PrimaryInteract))
        {
            if (GrappleHooked) DetachGrapple();
        }
    }

    private void FixedUpdate()
    {
        // Set Wishdir
        Wishdir = (transform.right * PlayerInput.GetAxisStrafeRight() +
                   transform.forward * PlayerInput.GetAxisStrafeForward()).normalized;
        if (PlayerInput.GetAxisStrafeForward() == 1)
        {
            if (PlayerInput.GetAxisStrafeRight() != 0)
            {
                Wishdir = Flatten((transform.forward * (velocity.magnitude / 4)) + (Wishdir * 4)).normalized;
            }
        }

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

        DashMove(factor);

        // Count how many ticks the player has been on a wall (include coyote time)
        if (PlayerInput.tickCount - _wallTimestamp < coyoteTime)
            _wallTickCount++;
        else
            _wallTickCount = 0;

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
        IsOnWall = false;
        IsGrounded = false;
    }

    private void OnCollisionExit(Collision other)
    {
        // momentumBuffer keeps track of the players positional difference over physics ticks
        // essentially what this does is takes any displace position movements and turns them into actual velocity when they exit a collision
        // if a player is being moved on a platform and they jump off, the platforms motion will convert into velocity
        _previousCollision = null;
        if (_momentumBuffer.Count < 2) return;
        if (Mathf.Abs(_momentumBuffer[0].x) > Mathf.Abs(velocity.x)) velocity.x = _momentumBuffer[0].x;
        if (Mathf.Abs(_momentumBuffer[0].z) > Mathf.Abs(velocity.z)) velocity.z = _momentumBuffer[0].z;
        _momentumBuffer.Clear();

        if (IsOnWall)
        {
            rollSound.volume = 0;
            IsOnWall = false;
        }
        SetCameraRotation(0, 50, false);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Rail Grab
        if (other.CompareTag("Rail") && (PlayerInput.tickCount - _railCooldownTimestamp > railCooldown ||
                                         other.transform.parent.gameObject != _lastRail))
        {
            _lastRail = other.transform.parent.gameObject;
            SetRail(other.gameObject.transform.parent.gameObject.GetComponent<CurvedLineRenderer>());
        }
    }

    private void OnCollisionStay(Collision other)
    {
        var moved = (rigidbody.transform.position - _displacePosition - _previousPosition) / Time.fixedDeltaTime;
        _momentumBuffer.Add(moved);

        if (_momentumBuffer.Count > 2) _momentumBuffer.RemoveAt(0);

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

            if (Vector3.Angle(Vector3.up, point.normal) < slopeAngle)
            {
                if (_sinceJumpCounter > jumpForgiveness)
                {
                    if (!_landed)
                    {
                        _landed = true;
                        source.PlayOneShot(groundLand);
                    }
                    IsGrounded = true;
                }

                if (other.collider.CompareTag("Bounce Block"))
                {
                    Accelerate(Vector3.up, 30, 30);
                    return;
                }
                if (other.collider.CompareTag("Kill Block"))
                {
                    Game.RestartLevel();
                    return;
                }
            }
            if (other.collider.CompareTag("Wall") && Mathf.Abs(Vector3.Angle(Vector3.up, point.normal) - 90) < wallAngleGive && !IsGrounded)
            {
                // Wall Grab
                _wallNormal = point.normal;
                if (_sinceJumpCounter > jumpForgiveness)
                {
                    if (!_landed)
                    {
                        _landed = true;
                        source.PlayOneShot(groundLand);
                    }
                    IsOnWall = true;
                }
            }
        }
    }

    public void Dash(Vector3 wishdir, float speed, float distance)
    {
        IsDashing = true;
        source.Play();

        HudMovement.RotationSlamVector += Vector3.up * jumpCameraThunk;

        var x = Flatten(velocity).magnitude;
        var dirChange = velocity.magnitude * wishdir.normalized;
        var adjustedY = (dirChange.y * x) / Flatten(dirChange).magnitude;
        var potentialVariant = new Vector2(x, adjustedY);
        var bonus = Mathf.Min(Mathf.Max(potentialVariant.magnitude - Flatten(velocity).magnitude, 0), Mathf.Abs(velocity.y));
        if (wishdir.y > 0 && velocity.y <= 0 || wishdir.y < 0 && velocity.y >= 0) bonus = 0;

        velocity = Mathf.Max(Mathf.Min(Flatten(velocity).magnitude + bonus, velocity.magnitude), movementSpeed) * wishdir.normalized;

        _dashStartSpeed = velocity.magnitude;
        velocity = (velocity.magnitude + speed) * velocity.normalized;

        _currentDashSpeed = speed;

        var time = distance / velocity.magnitude;

        Invoke("StopDash", time);
    }

    public void DashMove(float f)
    {
        if (velocity.magnitude < _dashStartSpeed || velocity.magnitude < movementSpeed) _currentDashSpeed = 0;
        if (_currentDashSpeed > 0 && !IsDashing)
        {
            var speed = velocity.magnitude;
            var drop = speed * f * dashFriction;

            var newspeed = speed - drop;
            if (newspeed < 0)
                newspeed = 0;
            if (speed > 0)
                newspeed /= speed;

            velocity *= newspeed;

            _currentDashSpeed -= Mathf.Max(0, speed - velocity.magnitude);
        }
    }

    public void CancelDash()
    {
        if (IsDashing)
        {
            var addspeed = Mathf.Min(3, _currentDashSpeed);
            if (_currentDashSpeed > 0)
            {
                var c = wallkickDisplay.color;
                c.g = 1;
                c.r = 0;
                c.b = 0;
                c.a = 1;
                wallkickDisplay.text = "+" + Mathf.RoundToInt(addspeed * 2);
                source.PlayOneShot(wallKick);
                wallkickDisplay.color = c;
            }
            _currentDashSpeed -= addspeed;
            IsDashing = false;
        }
    }

    public void StopDash()
    {
        IsDashing = false;
    }

    public void UndoDash()
    {
        if (IsDashing)
        {
            source.Stop();
            var take = _currentDashSpeed;
            IsDashing = false;
            velocity = velocity.normalized * (velocity.magnitude - take);
        }
    }

    public void SetRail(CurvedLineRenderer rail)
    {
        if (IsOnRail) return;
        UndoDash();
        _currentRail = rail;
        source.PlayOneShot(railLand);
        railSound.Play();
        railSound.volume = 1;
        if (GrappleHooked) DetachGrapple();
    }

    public void EndRail()
    {
        if (!IsOnRail) return;
        SetCameraRotation(0, 80, false);
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

        var bonusSpeed = Vector3.Dot(_railLeanVector, Wishdir) * 5;
        Accelerate(velocity.normalized, railSpeed + bonusSpeed, f);
        if (velocity.y < 0) Gravity(f);

        var totalAngle = Vector3.Angle(Vector3.up, _railLeanVector) / 2f;
        var projection = Vector3.Dot(_railLeanVector.normalized * totalAngle, -transform.right);
        SetCameraRotation(projection, 6, true);

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
        SetCameraRotation(0, 50, false);
        grappleDuring.volume = 0;
        //ApplySpeedPotentialFriction(0.3f);

        source.PlayOneShot(grappleRelease);
    }

    public void GrappleMove(float f)
    {
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

        SetCameraRotation(velocityProjection * value, 6, true);

        var yankProjection = Vector3.Dot(velocity, towardPoint);
        if (yankProjection < 0) velocity -= towardPoint * yankProjection;
        if (Vector3.Distance(position, camTrans.position) > grappleDistance)
        {
            ApplySpeedPotentialFriction(f / 2);
            velocity += towardPoint * f * 30;
        }

        Accelerate(towardPoint, grappleSwingForce, f);
        Accelerate(Wishdir, grappleSwingForce / 4, f);
    }

    public void WallMove(float f)
    {
        if (Jump()) return;
        rollSound.pitch = Mathf.Min(Mathf.Max(velocity.magnitude / 20, 1), 2);
        rollSound.volume = Mathf.Min(velocity.magnitude / 30, 1);

        DoubleJumpAvailable = true;

        var normal = Flatten(_wallNormal);

        var projection = Vector3.Dot(CrosshairDirection, new Vector3(-normal.z, normal.y, normal.x));

        SetCameraRotation(20 * -projection, 5, true);

        source.pitch = 1;

        //if (Flatten(velocity).magnitude + wallJumpSpeed > _lastAirborneSpeed && Flatten(velocity).magnitude > movementSpeed)
        //ApplyFriction(f * wallKickFriction);


        var speed = velocity.magnitude;
        var control = speed < deceleration ? deceleration : speed;
        var drop = control * f * wallCatchFriction;

        var newspeed = speed - drop;
        if (newspeed < 0)
            newspeed = 0;
        if (speed > 0)
            newspeed /= speed;

        velocity.y *= newspeed;

        var direction = new Vector3(_wallNormal.z, 0, -_wallNormal.x);
        if (Vector3.Angle(CrosshairDirection, direction) < 90)
            Accelerate(direction, wallSpeed, wallAcceleration * f);
        else
            Accelerate(-direction, wallSpeed, wallAcceleration * f);

        Accelerate(-_wallNormal, fallSpeed, gravity * f);
    }

    public void WallJump()
    {
        IsOnWall = false;
        _wallTimestamp = -coyoteTime;
        SetCameraRotation(0, 50, false);

        UndoDash();

        var x = velocity.normalized.x + _wallNormal.x / 4;
        var z = velocity.normalized.z + _wallNormal.z / 4;
        var jumpDir = new Vector3(x, 0, z).normalized;

        var newDir = Flatten(velocity).magnitude * jumpDir;
        velocity.x = newDir.x;
        velocity.z = newDir.z;

        if (Flatten(velocity).magnitude < movementSpeed)
        {
            var y = velocity.y;
            velocity = Flatten(velocity).normalized * movementSpeed;
            velocity.y = y;
        }

        DoubleJumpAvailable = true;
        _wallJumpTimestamp = Environment.TickCount;
        if (velocity.y > jumpHeight)
        {
            velocity.y += jumpHeight / 2;
        }
        Accelerate(new Vector3(0, 1, 0), jumpHeight, 40);

        /*if (PlayerInput.SincePressed(PlayerInput.Jump) != 0)
            wallkickDisplay.text = "-" + PlayerInput.SincePressed(PlayerInput.Jump);
        else
            wallkickDisplay.text = "+" + _wallTickCount;*/

        if (_wallTickCount < jumpForgiveness)
        {
            var wallKickSpeed = 3;
            velocity += velocity.normalized * wallKickSpeed;

            var c = wallkickDisplay.color;
            c.g = 1;
            c.r = 0;
            c.b = 0;
            c.a = 1;
            wallkickDisplay.text = "+" + wallKickSpeed * 2;
            source.PlayOneShot(wallKick);
            wallkickDisplay.color = c;
        }
        else
        {
            source.PlayOneShot(wallJump);
        }

        _momentumBuffer.Clear();

        _wallTickCount = 0;
        rollSound.volume = 0;
    }

    public void Gravity(float f)
    {
        if (!IsDashing) Accelerate(Vector3.down, fallSpeed, gravity * f);
    }

    public void GroundMove(float f)
    {
        rollSound.pitch = Mathf.Min(Mathf.Max(velocity.magnitude / 20, 1), 2);
        rollSound.volume = Mathf.Min(velocity.magnitude / 30, 1);

        Gravity(f);
        DoubleJumpAvailable = true;
        if (IsSliding)
        {
            _slideLeanVector = Vector3.Lerp(_slideLeanVector, Flatten(velocity).normalized, f * 4);
            var leanProjection = Vector3.Dot(_slideLeanVector, camera.transform.right);
            AirAccelerate(Wishdir, airAcceleration * f);
            SetCameraRotation(leanProjection * 15, 6, true);
        }
        else
        {
            _slideLeanVector = Flatten(velocity).normalized;
            ApplySpeedPotentialFriction(friction * f);
        }
        if (velocity.magnitude < movementSpeed)
        {
            Accelerate(Wishdir, movementSpeed, groundAcceleration * f);
        }

        Jump();
    }

    public void AirMove(float f)
    {
        _lastAirborneSpeed = Flatten(velocity).magnitude - _currentDashSpeed;
        Gravity(f);
        _slideLeanVector = Flatten(velocity).normalized;

        var time = (Environment.TickCount - _wallJumpTimestamp) / 500f;
        if (time > 1) time = 1;
        AirAccelerate(Wishdir, airAcceleration * f * time);
        rollSound.volume = 0;

        if (Flatten(velocity).magnitude > 25)
        {
            var speed = Flatten(velocity).magnitude;
            var control = speed < deceleration ? deceleration : speed;
            var drop = control * f * 0.04f;

            var newspeed = speed - drop;
            if (newspeed < 0)
                newspeed = 0;
            if (speed > 0)
                newspeed /= speed;

            velocity.x *= newspeed;
            velocity.z *= newspeed;
        }

        var t = 0.3f;

        var pos = InterpolatedPosition;
        var didHit = rigidbody.SweepTest(velocity.normalized, out RaycastHit hit, velocity.magnitude * t,
            QueryTriggerInteraction.Ignore);
        if (didHit && Math.Abs(Vector3.Angle(Vector3.up, hit.normal) - 90) < wallAngleGive && hit.collider.CompareTag("Wall"))
        {
            var close = hit.point;

            var distance = Flatten(close - pos).magnitude - 0.5f;
            if (!_approachingWall)
            {
                _approachingWall = true;
                _approachingWallDistance = distance;
            }

            var rotation = (_approachingWallDistance - distance) / _approachingWallDistance;

            var normal = Flatten(hit.normal);
            var projection = Vector3.Dot(CrosshairDirection, new Vector3(-normal.z, normal.y, normal.x));
            SetCameraRotation(20 * rotation * -projection, 60, false);
            _cancelLeanTickCount = 0;
        }
        else
        {
            if (_approachingWall)
            {
                _cancelLeanTickCount++;
                if (_cancelLeanTickCount >= 5)
                {
                    _approachingWall = false;
                    SetCameraRotation(0, 50, false);
                }
            }
        }

        Jump();
    }

    public void ApplySpeedFriction(float f)
    {
        var speed = velocity.magnitude;
        var control = speed < deceleration ? deceleration : speed;
        var drop = control * f;

        var newspeed = speed - drop;
        if (newspeed < 0)
            newspeed = 0;
        if (speed > 0)
            newspeed /= speed;

        velocity.x *= newspeed;
        velocity.z *= newspeed;
    }

    public void ApplySpeedPotentialFriction(float f)
    {
        var speed = velocity.magnitude;
        var control = speed < deceleration ? deceleration : speed;
        var drop = control * f;

        var newspeed = speed - drop;
        if (newspeed < 0)
            newspeed = 0;
        if (speed > 0)
            newspeed /= speed;

        velocity.x *= newspeed;
        velocity.y *= newspeed;
        velocity.z *= newspeed;
    }

    public void AirAccelerate(Vector3 wishdir, float accel)
    {

        if (wishdir.magnitude > 0 && Vector3.Angle(wishdir, Flatten(velocity)) < 90 && !IsStrafing)
        {
            var newdir = (Flatten(velocity) + wishdir * accel).normalized;
            var y = velocity.y;
            velocity = Flatten(velocity).magnitude * newdir;
            velocity.y = y;
        }

        var currentSpeed = Vector3.Dot(velocity, wishdir);
        var addSpeed = -currentSpeed;
        if (addSpeed > 0)
        {
            var accelSpeed = accel;
            if (accelSpeed > addSpeed)
                accelSpeed = addSpeed;

            var wishspeed = accelSpeed * 0.95f;
            var forwardspeed = accelSpeed * 0.05f;

            velocity += wishspeed * wishdir;
            velocity += forwardspeed * Flatten(velocity).normalized;
        }
    }

    public void Accelerate(Vector3 wishdir, float wishspeed, float accel)
    {
        if (wishspeed < 0) wishdir = -wishdir;
        var currentspeed = Vector3.Dot(velocity, wishdir);
        var addspeed = Mathf.Abs(wishspeed) - currentspeed;

        if (addspeed <= 0)
            return;

        var accelspeed = Mathf.Abs(accel) * Mathf.Abs(wishspeed);
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
            var wallJump = PlayerInput.tickCount - _wallTimestamp < coyoteTime;
            var groundJump = PlayerInput.tickCount - _groundTimestamp < coyoteTime;
            _groundTimestamp = -coyoteTime;
            var railJump = PlayerInput.tickCount - _railTimestamp < coyoteTime;
            _railTimestamp = -coyoteTime;

            if (!groundJump && !railJump && !wallJump && !DoubleJumpAvailable) return false;
            _sinceJumpCounter = 0;
            PlayerInput.ClearSincePressed(PlayerInput.Jump);

            if (wallJump)
            {
                WallJump();
                return true;
            }

            var speed = jumpHeight;
            if (!groundJump && !railJump)
            {
                DoubleJumpAvailable = false;
                Dash(CrosshairDirection, dashSpeed, dashDistance);
                return true;
            }
            CancelDash();

            velocity.y = Mathf.Max(speed, velocity.y + speed);

            SetCameraRotation(0, 50, false);

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