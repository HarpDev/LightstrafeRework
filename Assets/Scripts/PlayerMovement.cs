using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    private const float Tolerance = 0.05f;
    public new Rigidbody rigidbody;
    public new Collider collider;
    public new Camera camera;

    public Vector3 cameraPosition;

    /* Movement Stuff */
    public float deceleration = 10f;
    public float friction = 3f;
    public float runAcceleration = 6f;
    public float airAcceleration = 60f;
    public float surfAcceleration = 900f;
    public float gravity = 0.5f;
    public bool gravityEnabled = true;
    public float movementSpeed = 11;
    public float jumpHeight = 11f;
    public float fallSpeed = 40f;
    public float wallFriction = 2f;
    public float wallSpeed = 20f;
    public float wallJumpSpeed = 30f;
    public int wallNoFrictionTicks = 1;
    public float grappleSwingForce = 25f;
    public float grappleDetachFrictionScale = 0.12f;
    public float slopeAngle = 45;

    public Vector3 velocity;

    public Image wallkickDisplay;

    /* Audio */
    public AudioSource source;
    public AudioSource grindSound;
    public AudioSource grappleDuring;
    public AudioClip spring;
    public AudioClip jump;
    public AudioClip jumpair;
    public AudioClip land;
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

    public Vector2 startRotation;

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
    private Vector3 _grappleAttachPosition;
    private Vector3 _previousPosition;
    private Collision _previousCollision;
    private Vector3 _previousCollisionLocalPosition;
    private Vector3 _slideLeanVector;
    private float _motionInterpolationDelta;
    private float _crouchAmount;
    private bool _isSurfing;
    private int _wallJumpTimestamp;
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
        Yaw = startRotation.x;
        Pitch = startRotation.y;

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
        CrosshairDirection = cam.forward;
        camera.transform.rotation =
            Quaternion.Euler(new Vector3(Pitch + HudMovement.rotationSlamVectorLerp.y, Yaw, CameraRotation));
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

        if ((!IsSliding || !IsGrounded) && !IsOnWall)
            CameraRotation = Mathf.Lerp(CameraRotation, 0, Time.deltaTime * 6);
    }

    private void FixedUpdate()
    {
        rigidbody.velocity = new Vector3();
        if (IsOnWall)
        {
            _wallTickCount++;
            if (_wallTickCount == 4)
            {
                grindSound.volume = 1;
                source.PlayOneShot(wallLand);
            }
        }

        var factor = Time.fixedDeltaTime;

        Gravity(factor);

        GrappleMove(factor);
        WallMove(factor);
        SlideMove(factor);
        GroundMove(factor);
        AirMove(factor);

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
            if (blockAction.IsAtApex) _momentumBuffer.Add(blockAction.maxSpeed * blockAction.direction.normalized);
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
        }

        if (!validCollision) return;

        DoubleJumpAvailable = true;

        if (other.collider.CompareTag("Bounce Block"))
        {
            if (Environment.TickCount - _bounceTimestamp <= 1000) return;
            _bounceTimestamp = Environment.TickCount;
            Accelerate(new Vector3(0, 1, 0), 26, 26);
            DoubleJumpAvailable = true;
            source.PlayOneShot(spring);
        }
        else if (other.collider.CompareTag("Kill Block"))
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
        gravityEnabled = false;
    }

    public void AttachGrapple(Vector3 point)
    {
        if (!enabled) return;
        source.PlayOneShot(grappleAttach);
        Game.I.Hitmarker.Display();
        if (GrappleHooked) return;
        if (Vector3.Distance(point, transform.position) > maxGrappleDistance) return;
        _grappleAttachPosition = point;
        GrappleHooked = true;
        gravityEnabled = false;
        DoubleJumpAvailable = true;
        grappleTether.enabled = true;
        _grappleAttachTimestamp = Environment.TickCount;
        grappleDuring.volume = 1;

        var towardPoint = (_grappleAttachPosition - transform.position).normalized;
        var yankProjection = Vector3.Dot(velocity, towardPoint);
        velocity -= towardPoint * yankProjection;
    }

    public void DetachGrapple()
    {
        gravityEnabled = true;
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
            {new Vector3(0, grappleYOffset, 0), camTrans.InverseTransformPoint(_grappleAttachPosition)};

        grappleTether.positionCount = list.Count;
        grappleTether.SetPositions(list.ToArray());

        if (Environment.TickCount - _grappleAttachTimestamp > maxGrappleTimeMillis) DetachGrapple();

        camTrans.localPosition = new Vector3();

        var towardPoint = (_grappleAttachPosition - camTrans.position).normalized;

        var forward = camTrans.forward;
        var projection = Vector3.Dot(forward, towardPoint);
        if (projection < 0) DetachGrapple();

        var velocityProjection = Mathf.Abs(Vector3.Dot(velocity, camTrans.right));

        var relativePoint = transform.InverseTransformPoint(_grappleAttachPosition);

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
                gravityEnabled = true;
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
        var point = _currentWall.ClosestPoint(InterpolatedPosition);
        var ycompare = point.y - InterpolatedPosition.y;
        var distance = (Flatten(point) - Flatten(InterpolatedPosition)).magnitude;

        if (ycompare < -0.9f || ycompare > 0 || distance > 0.5f * 2)
        {
            IsOnWall = false;
            gravityEnabled = true;

            grindSound.volume = 0;
            _wallTickCount = -1;
            return;
        }

        if (_currentWall.CompareTag("Launch Wall"))
        {
            Accelerate(new Vector3(0, 1, 0), 25, 1 * f);
        }
        else
        {
            if (_wallTickCount > wallNoFrictionTicks) ApplyFriction(wallFriction * f);
            var towardWall = Flatten(point - InterpolatedPosition).normalized;

            if (_wallTickCount == 1) Accelerate(new Vector3(0, 1, 0), 2, 4);
            Accelerate(towardWall, 1, 1 * f);
            Accelerate(Flatten(velocity).normalized, wallSpeed, runAcceleration * f);
        }

        DoubleJumpAvailable = true;
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
        var jumpDir = Flatten(position - point).normalized;
        IsOnWall = false;
        gravityEnabled = true;
        _wallJumpTimestamp = Environment.TickCount;

        var c = wallkickDisplay.color;
        Accelerate(new Vector3(0, 1, 0), IsSliding ? jumpHeight / 1.5f : jumpHeight, 2);
        if (_currentWall.CompareTag("Launch Wall")) Accelerate(new Vector3(0, 1, 0), 40, 0.2f);

        Accelerate(jumpDir, wallJumpSpeed, 0.2f);
        DoubleJumpAvailable = true;

        if (_wallTickCount <= 1)
        {
            Accelerate(Flatten(velocity).normalized, wallJumpSpeed * 2f, 0.07f);
            source.PlayOneShot(wallKick);
            c.a = 1;
            c.r = 0;
            c.b = 0;
            c.g = 1;
        }
        else if (_wallTickCount == 2)
        {
            Accelerate(Flatten(velocity).normalized, wallJumpSpeed * 1.8f, 0.07f);
            source.PlayOneShot(wallKick);
            c.a = 1;
            c.r = 1;
            c.b = 0;
            c.g = 1;
        }
        else if (_wallTickCount == 3)
        {
            Accelerate(Flatten(velocity).normalized, wallJumpSpeed * 1.6f, 0.08f);
            source.PlayOneShot(wallKick);
            c.a = 1;
            c.r = 1;
            c.b = 0;
            c.g = 0;
        }

        _momentumBuffer.Clear();

        source.PlayOneShot(wallJump);

        wallkickDisplay.color = c;
        _wallTickCount = -1;
        grindSound.volume = 0;
    }

    public void Gravity(float f)
    {
        if (!gravityEnabled) return;
        Accelerate(new Vector3(0, -1, 0), fallSpeed, gravity * f);
    }

    public void GroundMove(float f)
    {
        if (!IsGrounded) return;
        if (IsSliding) return;
        ApplyFriction(f);
        if (!IsMoving) return;

        Accelerate(Wishdir, movementSpeed, runAcceleration * f);
    }

    public void SlideMove(float f)
    {
        if (!IsSliding)
        {
            _slideLeanVector = velocity;
            grindSound.volume = 0;
            return;
        }
        if (!IsGrounded) return;
        if (GrappleHooked) return;
        if (IsOnWall) return;
        if (grindSound.volume <= 0)
        {
            source.PlayOneShot(wallLand);
        }
        grindSound.pitch = Mathf.Min(Mathf.Max(velocity.magnitude / 10, 1), 2);
        grindSound.volume = Mathf.Min(velocity.magnitude / 10, 1);

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
        if (!IsMoving) return;
        var accel = _isSurfing ? surfAcceleration : airAcceleration;
        accel *= Mathf.Min((Environment.TickCount - _wallJumpTimestamp) / 1000f, 1);
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
        if (IsGrounded) DoubleJumpAvailable = true;
        if (_jumpLock && Input.GetAxis("Jump") < Tolerance) _jumpLock = false;
        if (_groundLock && Input.GetAxis("Jump") < Tolerance && IsGrounded) _groundLock = false;
        if (_jumpLock) return;

        if (!(Input.GetAxis("Jump") > 0)) return;
        _jumpLock = true;
        if (IsGrounded && _groundLock) return;
        if (!IsGrounded && !DoubleJumpAvailable) return;

        var speed = jumpHeight;
        if (_momentumBuffer.Count > 0 && _momentumBuffer[0].y > speed) speed += _momentumBuffer[0].y;

        Accelerate(new Vector3(0, 1, 0), speed, jumpHeight);

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