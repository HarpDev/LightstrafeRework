using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    private const float Tolerance = 0.05f;
    public new Rigidbody rigidbody;
    public new Camera camera;

    public Vector3 cameraPosition;
    public Vector3 velocity;

    /* Movement Stuff */
    public float deceleration = 10f;
    public float friction = 3f;
    public float runAcceleration = 6f;
    public float airAcceleration = 60f;
    public float surfAcceleration = 900f;
    public float gravity = 0.5f;
    public float movementSpeed = 11;
    public float jumpHeight = 11f;
    public float fallSpeed = 40f;
    public float wallSpeed = 20f;
    public float wallFriction = 0.5f;
    public float wallJumpSpeed = 15f;
    public float greenKickSpeed = 25f;
    public int greenKickTicks = 2;
    public float dashSpeed = 25f;
    public float grappleSwingForce = 25f;
    public float grappleDetachFrictionScale = 0.12f;
    public float slopeAngle = 45;

    public Image wallkickDisplay;

    /* Audio */
    public AudioSource source;
    public AudioSource grindSound;
    public AudioSource grappleDuring;
    public AudioClip spring;
    public AudioClip jump;
    public AudioClip jumpair;
    public AudioClip ding;
    public AudioClip wallLand;
    public AudioClip wallKick;
    public AudioClip wallJump;
    public AudioClip grappleAttach;
    public AudioClip grappleRelease;

    public LineRenderer grappleTether;
    public float grappleYOffset;
    public int maxGrappleTimeMillis = 5000;
    public int maxGrappleDistance = 120;
    public bool GrappleHooked { get; set; }

    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public float LookScale { get; set; }

    private bool _firstMove;
    private bool _jumpLock;
    private bool _groundLock;
    private bool _approachingWall;
    private float _approachingWallDistance;
    private int _groundTimestamp;
    private int _bounceTimestamp;
    private Collider _currentWall;
    private int _wallTickCount;
    private int _grappleAttachTimestamp;
    private Transform _grappleAttachPosition;
    private Vector3 _previousPosition;
    private Collision _previousCollision;
    private Vector3 _previousCollisionLocalPosition;
    private Vector3 _slideLeanVector;
    private Vector3 _dashVector;
    private float _motionInterpolationDelta;
    private float _crouchAmount;
    private bool _isSurfing;
    private float _dashTimer;
    private readonly List<Vector3> _momentumBuffer = new List<Vector3>();

    public static bool DoubleJumpAvailable { get; set; }

    public static float MovementDirectionRadians
    {
        get { return Mathf.Atan2(Input.GetAxis("Right"), Input.GetAxis("Forward")); }
    }

    public static bool IsMoving
    {
        get { return Math.Abs(Input.GetAxis("Forward")) > Tolerance || Math.Abs(Input.GetAxis("Right")) > Tolerance; }
    }

    public static bool IsSliding
    {
        get { return Input.GetAxis("Slide") > 0; }
    }

    public bool IsDashing
    {
        get { return _dashVector.magnitude > 0; }
    }

    public bool IsGrounded
    {
        get { return Environment.TickCount - _groundTimestamp < 100; }
    }

    public bool IsOnWall { get; set; }

    public float CameraRotation { get; set; }

    public Vector3 InterpolatedPosition
    {
        get
        {
            return Vector3.Lerp(_previousPosition, rigidbody.transform.position,
                _motionInterpolationDelta / Time.fixedDeltaTime);
        }
    }

    public Vector3 Wishdir { get; set; }

    public Vector3 CrosshairDirection { get; set; }

    private void Start()
    {
        LookScale = 1;
        //Time.timeScale = 0.2f;

        _firstMove = false;
        Game.StopTimer();
        Game.ResetTimer();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Yaw = transform.rotation.eulerAngles.y;

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
        if (Input.GetAxis("Reset") > 0) Game.RestartLevel();

        if (Cursor.visible) return;

        if ((IsMoving || Input.GetAxis("Fire1") > 0) && !_firstMove)
        {
            _firstMove = true;
            Game.StartTimer();
        }

        // Wallkick display fade out
        var c = wallkickDisplay.color;
        if (c.a > 0) c.a -= Time.deltaTime;
        wallkickDisplay.color = c;

        // Mouse motion
        Yaw = (Yaw + Input.GetAxis("Mouse X") * (Game.Sensitivity / 10) * LookScale) % 360f;
        Pitch -= Input.GetAxis("Mouse Y") * (Game.Sensitivity / 10) * LookScale;
        Pitch = Mathf.Max(Pitch, -90);
        Pitch = Mathf.Min(Pitch, 90);

        Transform cam;
        (cam = camera.transform).rotation = Quaternion.Euler(new Vector3(Pitch, Yaw, CameraRotation));
        camera.transform.rotation =
            Quaternion.Euler(new Vector3(Pitch + HudMovement.rotationSlamVectorLerp.y, Yaw, CameraRotation));
        CrosshairDirection = cam.forward;
        rigidbody.transform.rotation = Quaternion.Euler(new Vector3(0, Yaw, 0));

        // Movement
        var t = MovementDirectionRadians;

        t += Mathf.Deg2Rad * Yaw;
        Wishdir = IsMoving ? new Vector3(Mathf.Sin(t), 0, Mathf.Cos(t)) : new Vector3();

        _motionInterpolationDelta += Time.deltaTime;

        var position = cameraPosition;
        if (IsSliding)
        {
            if (_crouchAmount < 1) _crouchAmount += Time.deltaTime * 6;
        }
        else if (_crouchAmount > 0) _crouchAmount -= Time.deltaTime * 6;

        position.y -= 0.6f * _crouchAmount;
        camera.transform.position = InterpolatedPosition + position;

        WallLean(0.3f, Time.deltaTime);

        if ((!IsSliding || !IsGrounded) && !IsOnWall && !IsDashing)
            CameraRotation = Mathf.Lerp(CameraRotation, 0, Time.deltaTime * 6);
    }

    private void FixedUpdate()
    {
        rigidbody.velocity = new Vector3();
        if (IsOnWall)
        {
            _wallTickCount++;
        }

        var factor = Time.fixedDeltaTime;

        Gravity(factor);

        GrappleMove(factor);
        WallMove(factor);
        SlideMove(factor);
        GroundMove(factor);
        AirMove(factor);
        DashMove(factor);

        Jump();
        WallJump();

        _motionInterpolationDelta = 0;
        _isSurfing = false;
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

            _previousCollisionLocalPosition =
                _previousCollision.transform.InverseTransformPoint(rigidbody.transform.position);
        }
        else _previousCollisionLocalPosition = new Vector3();

        _previousPosition = rigidbody.transform.position;
        var nextPosition = _previousPosition + platformMotion + velocity * factor;

        RaycastHit hit;
        var difference = nextPosition - _previousPosition;
        if (rigidbody.SweepTest(difference.normalized, out hit, difference.magnitude))
        {
            if (!hit.collider.isTrigger)
            {
                if (Vector3.Angle(Vector3.up, hit.normal) < slopeAngle) _groundTimestamp = Environment.TickCount;
                var projection = Vector3.Dot(velocity, -hit.normal);
                if (projection > 0)
                {
                    var impulse = hit.normal * (projection - 4f);
                    velocity += impulse;
                }

                nextPosition = Vector3.Lerp(_previousPosition + platformMotion + velocity * factor, nextPosition,
                    hit.distance / difference.magnitude);
            }
        }

        _previousCollision = null;
        rigidbody.MovePosition(nextPosition);
    }

    private void OnCollisionExit(Collision other)
    {
        _previousCollision = null;
        if (_momentumBuffer.Count == 0) return;
        if (Mathf.Abs(_momentumBuffer[0].x) > Mathf.Abs(velocity.x)) velocity.x = _momentumBuffer[0].x;
        if (Mathf.Abs(_momentumBuffer[0].y) > Mathf.Abs(velocity.y)) velocity.y = _momentumBuffer[0].y;
        if (Mathf.Abs(_momentumBuffer[0].z) > Mathf.Abs(velocity.z)) velocity.z = _momentumBuffer[0].z;
        _momentumBuffer.Clear();
    }

    private void OnCollisionStay(Collision other)
    {
        var moved = (rigidbody.transform.position - _previousPosition) / Time.fixedDeltaTime;
        if (other.collider.CompareTag("Launch Block"))
        {
            var blockAction = other.collider.gameObject.GetComponent<BlockAction>();
            if (blockAction.IsAtApex)
                _momentumBuffer.Add(blockAction.maxSpeed * blockAction.direction.normalized + moved);
            else _momentumBuffer.Add(moved);
        }
        else _momentumBuffer.Add(moved);

        if (_momentumBuffer.Count > 2) _momentumBuffer.RemoveAt(0);

        _previousCollision = other;
        _previousCollision.transform.hasChanged = false;

        var validCollision = false;
        foreach (var point in other.contacts)
        {
            if (Vector3.Angle(Vector3.up, point.normal) < slopeAngle) _groundTimestamp = Environment.TickCount;
            var projection = Vector3.Dot(velocity, -point.normal);
            if (projection <= 0) continue;
            validCollision = true;
            var impulse = point.normal * projection;
            velocity += impulse;
            if (IsDashing) _dashVector = velocity.normalized;
        }

        if (!validCollision) return;

        if (other.collider.CompareTag("Kill Block"))
        {
            Game.RestartLevel();
        }
        else if (other.collider.CompareTag("Launch Block"))
        {
            other.gameObject.GetComponent<BlockAction>().ActivateLaunch();
        }

        // Wall Grab
        var close = other.collider.ClosestPoint(InterpolatedPosition);
        var compare = close.y - InterpolatedPosition.y;
        if (compare < -0.9f || compare > 0 ||
            Math.Abs(Vector3.Angle(Vector3.up, other.contacts[0].normal) - 90) > Tolerance ||
            IsOnWall || IsGrounded)
        {
            if (!IsGrounded)
                _isSurfing = true;
            return;
        }

        _currentWall = other.collider;
        IsOnWall = true;
    }

    public void Dash(Vector3 direction)
    {
        _dashVector = Flatten(direction).normalized;
    }

    public void DashMove(float f)
    {
        const float dashTime = 0.5f;
        if (!IsDashing)
        {
            _dashTimer = 0;
            return;
        }

        var factor = 1 - Mathf.Min(_dashTimer / dashTime, 1) + 1;

        velocity = factor * dashSpeed * _dashVector;

        var projection = Vector3.Dot(Flatten(velocity), camera.transform.right) / 2f;
        CameraRotation = Mathf.Lerp(CameraRotation, projection, f * 12);

        if (_dashTimer > dashTime) _dashVector = new Vector3();
        _dashTimer += f;
    }

    public void CancelDash(Vector3 direction)
    {
        velocity = dashSpeed * direction.normalized;
        _dashVector = new Vector3();
    }

    public void AttachGrapple(Transform position)
    {
        if (!enabled) return;
        source.PlayOneShot(grappleAttach);
        Game.I.Hitmarker.Display();
        if (GrappleHooked) return;
        if (Vector3.Distance(position.position, transform.position) > maxGrappleDistance) return;
        _grappleAttachPosition = position;
        GrappleHooked = true;
        grappleTether.enabled = true;
        _grappleAttachTimestamp = Environment.TickCount;
        grappleDuring.volume = 1;

        var towardPoint = (_grappleAttachPosition.position - transform.position).normalized;
        var yankProjection = Vector3.Dot(velocity, towardPoint);
        velocity -= towardPoint * yankProjection;
    }

    public void DetachGrapple()
    {
        if (GrappleHooked) GrappleHooked = false;
        if (grappleTether.enabled) grappleTether.enabled = false;
        ApplyFriction(grappleDetachFrictionScale);
        grappleDuring.volume = 0;
        source.PlayOneShot(grappleRelease);
    }

    public void GrappleMove(float f)
    {
        if (!GrappleHooked) return;
        var camTrans = camera.transform;

        var list = new List<Vector3>
            {new Vector3(0, grappleYOffset, 0), camTrans.InverseTransformPoint(_grappleAttachPosition.position)};

        grappleTether.positionCount = list.Count;
        grappleTether.SetPositions(list.ToArray());

        if (Environment.TickCount - _grappleAttachTimestamp > maxGrappleTimeMillis) DetachGrapple();

        camTrans.localPosition = new Vector3();

        var towardPoint = (_grappleAttachPosition.position - camTrans.position).normalized;

        var forward = camTrans.forward;
        var projection = Vector3.Dot(forward, towardPoint);
        if (projection < 0) DetachGrapple();

        var velocityProjection = Mathf.Abs(Vector3.Dot(velocity, camTrans.right));

        var relativePoint = transform.InverseTransformPoint(_grappleAttachPosition.position);

        var value = Mathf.Atan2(relativePoint.z, relativePoint.x) * Mathf.Rad2Deg;
        value = Mathf.Abs(value);
        value -= 90;
        value /= 90;

        grappleDuring.pitch = velocity.magnitude / 30f;

        CameraRotation = Mathf.Lerp(CameraRotation, velocityProjection * value, f * 10);

        var yankProjection = Vector3.Dot(velocity, towardPoint);
        if (yankProjection < 0) velocity -= towardPoint * yankProjection;

        Accelerate(towardPoint, grappleSwingForce, 1 * f);
        Accelerate(velocity.normalized, grappleSwingForce, 0.4f * f);
        Accelerate(Wishdir, grappleSwingForce / 4, 1 * f);
    }

    public void WallLean(float t, float f)
    {
        if (IsOnWall)
        {
            try
            {
                var point = _currentWall.ClosestPoint(InterpolatedPosition);

                var relativePoint = camera.transform.InverseTransformPoint(point);

                var value = Mathf.Atan2(relativePoint.z, relativePoint.x) * Mathf.Rad2Deg;
                value = Mathf.Abs(value);
                value -= 90;
                value /= 90;

                CameraRotation = Mathf.Lerp(CameraRotation, 20 * -value, f * 25);
            }
            catch (Exception)
            {
                IsOnWall = false;
            }
        }
        else
        {
            if (!_approachingWall) _approachingWallDistance = 100000f;
        }

        RaycastHit hit;
        var layermask = ~(1 << 9);

        var pos = InterpolatedPosition;
        var didHit = Physics.CapsuleCast(pos - new Vector3(0, 2f, 0), pos + new Vector3(0, 1f, 0),
            0.5f, Flatten(velocity).normalized, out hit, velocity.magnitude * t,
            layermask);
        if (didHit && !IsGrounded && !IsOnWall &&
            Math.Abs(Vector3.Angle(Vector3.up, hit.normal) - 90) < Tolerance &&
            !hit.collider.CompareTag("Kill Block"))
        {
            var close = hit.point;

            var distance = Flatten(close - pos).magnitude - 0.5f;
            if (!(distance <= _approachingWallDistance)) return;
            if (!_approachingWall)
            {
                _approachingWall = true;
                _approachingWallDistance = distance;
            }

            var rotation = (_approachingWallDistance - distance) / _approachingWallDistance;

            var relativePoint = camera.transform.InverseTransformPoint(hit.collider.ClosestPoint(pos));

            var value = Mathf.Atan2(relativePoint.z, relativePoint.x) * Mathf.Rad2Deg;
            value = Mathf.Abs(value);
            value -= 90;
            value /= 90;

            CameraRotation = Mathf.Lerp(CameraRotation, 20 * rotation * -value, f * 35);
        }
        else
        {
            _approachingWall = false;
        }
    }

    public void WallMove(float f)
    {
        if (!IsOnWall) return;
        if (IsDashing) return;
        var point = _currentWall.ClosestPoint(InterpolatedPosition);
        var ycompare = point.y - InterpolatedPosition.y;
        var distance = (Flatten(point) - Flatten(InterpolatedPosition)).magnitude;

        if (ycompare < -0.9f || ycompare > 0 || distance > 0.5f * 2)
        {
            IsOnWall = false;

            grindSound.volume = 0;
            _wallTickCount = -1;
            return;
        }
        
        DoubleJumpAvailable = true;

        if (_currentWall.CompareTag("Launch Wall"))
        {
            Accelerate(new Vector3(0, 1, 0), 25, 1 * f);
        }
        else
        {
            var speed = velocity.magnitude;
            var control = speed < deceleration ? deceleration : speed;
            var drop = control * friction * f;

            var newspeed = speed - drop;
            if (newspeed < 0)
                newspeed = 0;
            if (speed > 0)
                newspeed /= speed;

            velocity.y *= newspeed;

            ApplyFriction(wallFriction * f);

            var towardWall = Flatten(point - InterpolatedPosition).normalized;

            if (_wallTickCount == 1) Accelerate(new Vector3(0, 1, 0), 2, 4);
            Accelerate(towardWall, 4, 10 * f);
            Accelerate(Flatten(velocity).normalized, wallSpeed, runAcceleration * f);
        }
    }

    public void WallJump()
    {
        if (!IsOnWall) return;
        if (_jumpLock && Input.GetAxis("Jump") < Tolerance) _jumpLock = false;
        if (_jumpLock) return;

        if (!(Input.GetAxis("Jump") > 0)) return;
        _jumpLock = true;

        var position = transform.position;
        var point = _currentWall.ClosestPoint(position);
        var awayDir = Flatten(position - point).normalized;
        IsOnWall = false;

        var c = wallkickDisplay.color;
        Accelerate(new Vector3(0, 1, 0), IsSliding ? jumpHeight / 1.5f : jumpHeight, 2);
        if (_currentWall.CompareTag("Launch Wall")) Accelerate(new Vector3(0, 1, 0), 40, 0.2f);

        var x = velocity.x + 0.2f * 15 * awayDir.x;
        var z = velocity.z + 0.2f * 15 * awayDir.z;
        var jumpDir = new Vector3(x, 0, z).normalized;

        var newDir = Flatten(velocity).magnitude * jumpDir;
        velocity.x = newDir.x;
        velocity.z = newDir.z;

        if (IsDashing)
        {
            CancelDash(velocity);
        }

        if (_wallTickCount <= greenKickTicks)
        {
            Accelerate(jumpDir, greenKickSpeed, 1);
            source.PlayOneShot(wallKick);
            c.a = 1;
            c.r = 0;
            c.b = 0;
            c.g = 1;
        }
        else
        {
            Accelerate(jumpDir, wallJumpSpeed, 1);
        }

        _momentumBuffer.Clear();

        source.PlayOneShot(wallJump);

        wallkickDisplay.color = c;
        _wallTickCount = -1;
        grindSound.volume = 0;
    }

    public void Gravity(float f)
    {
        if (IsOnWall) return;
        if (GrappleHooked) return;
        if (IsDashing) return;
        Accelerate(new Vector3(0, -1, 0), fallSpeed, gravity * f);
    }

    public void GroundMove(float f)
    {
        if (!IsGrounded) return;
        if (IsDashing) return;
        if (IsSliding) return;
        ApplyFriction(f);
        DoubleJumpAvailable = true;
        if (!IsMoving) return;

        Accelerate(Wishdir, movementSpeed, runAcceleration * f);
    }

    public void SlideMove(float f)
    {
        if (!IsGrounded)
        {
            _slideLeanVector = velocity;
            grindSound.volume = 0;
            return;
        }

        if (!IsSliding) return;
        if (GrappleHooked) return;
        if (IsOnWall) return;

        if (grindSound.volume <= 0) source.PlayOneShot(wallLand);
        grindSound.pitch = Mathf.Min(Mathf.Max(velocity.magnitude / 10, 1), 2);
        grindSound.volume = Mathf.Min(velocity.magnitude / 10, 1);
        DoubleJumpAvailable = true;

        ApplyFriction(f / 12);
        _slideLeanVector = Vector3.Lerp(_slideLeanVector, velocity, f * 4);
        var projection = Vector3.Dot(Flatten(_slideLeanVector), camera.transform.right);
        CameraRotation = Mathf.Lerp(CameraRotation, projection * _crouchAmount, f * 12);
        AirAccelerate(Wishdir, airAcceleration * f);
    }

    public void AirMove(float f)
    {
        if (IsGrounded) return;
        if (GrappleHooked) return;
        if (IsOnWall) return;
        if (!IsMoving) return;
        var accel = _isSurfing ? surfAcceleration : airAcceleration;
        AirAccelerate(Wishdir, accel * f);
    }

    public void ApplyFriction(float f)
    {
        var speed = velocity.magnitude;
        var control = speed < deceleration ? deceleration : speed;
        var drop = control * friction * f;

        var newspeed = speed - drop;
        if (newspeed < 0)
            newspeed = 0;
        if (speed > 0)
            newspeed /= speed;

        velocity.x *= newspeed;
        if (!IsGrounded)
            velocity.y *= newspeed;
        velocity.z *= newspeed;
    }

    public void AirAccelerate(Vector3 wishdir, float accel)
    {
        var wishSpeed = wishdir.magnitude;

        var currentSpeed = Vector3.Dot(velocity, wishdir);
        var addSpeed = wishSpeed - currentSpeed;
        if (addSpeed > 0)
        {
            var accelSpeed = accel * wishSpeed;
            if (accelSpeed > addSpeed)
                accelSpeed = addSpeed;
            velocity += accelSpeed * Wishdir;
        }
    }

    public void Accelerate(Vector3 wishdir, float wishspeed, float accel)
    {
        var currentspeed = Vector3.Dot(velocity, wishdir);
        var addspeed = wishspeed - currentspeed;

        if (addspeed <= 0)
            return;

        var accelspeed = accel * wishspeed;
        if (accelspeed > addspeed)
            accelspeed = addspeed;

        velocity.x += accelspeed * wishdir.x;
        velocity.y += accelspeed * wishdir.y;
        velocity.z += accelspeed * wishdir.z;
    }

    public void Jump()
    {
        if (IsOnWall) return;
        if (_jumpLock && Input.GetAxis("Jump") < Tolerance) _jumpLock = false;
        if (_groundLock && Input.GetAxis("Jump") < Tolerance && IsGrounded) _groundLock = false;
        if (_jumpLock) return;

        if (!(Input.GetAxis("Jump") > 0)) return;
        _jumpLock = true;
        if (IsGrounded && _groundLock) return;
        if (!IsGrounded && !DoubleJumpAvailable) return;

        var speed = jumpHeight;
        if (_momentumBuffer.Count > 0 && _momentumBuffer[0].y > speed) speed += _momentumBuffer[0].y;


        if (IsDashing) _dashVector.y += 0.5f;
        else Accelerate(new Vector3(0, 1, 0), speed, jumpHeight);

        if (IsGrounded)
        {
            _groundLock = true;
            source.PlayOneShot(jump);
        }
        else
        {
            DoubleJumpAvailable = false;
            source.PlayOneShot(jumpair);
            AirAccelerate(Wishdir, 5);
        }

        grindSound.volume = 0;
        _groundTimestamp = 0;

        var slam = HudMovement.RotationSlamVector;
        slam.y += 20;
        HudMovement.RotationSlamVector = slam;
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}