using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    private const float Tolerance = 0.05f;
    public new Rigidbody rigidbody;
    public new Camera camera;
    public Text multiplierText;

    public Vector3 cameraPosition;
    public Vector3 velocity;

    /* Movement Stuff */
    public float deceleration = 10f;
    public float friction = 5f;
    public float runAcceleration = 8f;
    public float airFriction = 1f;
    public float strafeAcceleration = 75f;
    public float stairHeight = 1.2f;
    public float surfAcceleration = 900f;
    public float gravity = 0.3f;
    public float movementSpeed = 11;
    public float jumpHeight = 12f;
    public float fallSpeed = 60f;
    public float wallSpeed = 12f;
    public float wallFriction = 0.7f;
    public float wallJumpSpeed = 14f;
    public float greenKickSpeed = 20f;
    public int greenKickTicks = 6;
    public float wallKickFlingThreshold = 5;
    public float wallBumpThreshold = 8;
    public float wallBumpSpeed = 10;
    public float grappleSwingForce = 40f;
    public float grappleDetachFrictionScale = 0.12f;
    public float slopeAngle = 45;

    public Image wallkickDisplay;

    /* Audio */
    public AudioSource source;
    public AudioSource grindSound;
    public AudioSource grappleDuring;
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
    private bool _approachingWall;
    private float _approachingWallDistance;
    private int _groundTimestamp;
    private int _wallTimestamp;
    private int _bounceTimestamp;
    private float _lastJumpBeforeYVelocity;
    private Collider _currentWall;
    private int _wallTickCount;
    private int _wallKickCounter;
    private int _wallJumpTimestamp;
    private Vector3 _wallNormal;
    private int _grappleAttachTimestamp;
    private float _targetSpeed;
    private Transform _grappleAttachPosition;
    private Vector3 _previousPosition;
    private Collision _previousCollision;
    private Vector3 _previousCollisionLocalPosition;
    private Vector3 _slideLeanVector;
    private Vector3 _displacePosition;
    private float _motionInterpolationDelta;
    private float _crouchAmount;
    private bool _isSurfing;
    private int _sinceJumpCounter;
    private Vector3 _lastAirborneVelocity;
    private float _cameraRoll;
    private float _removeInvertY;
    private readonly List<Vector3> _momentumBuffer = new List<Vector3>();

    public static bool DoubleJumpAvailable { get; set; }

    public static bool Inverted { get; set; }

    public static float MovementDirectionRadians
    {
        get { return Mathf.Atan2(Input.GetAxisRaw("Right"), Input.GetAxisRaw("Forward")); }
    }

    public static bool IsMoving
    {
        get { return Math.Abs(Input.GetAxis("Forward")) > Tolerance || Math.Abs(Input.GetAxis("Right")) > Tolerance; }
    }

    public static bool IsSliding
    {
        get { return Input.GetAxis("Slide") > 0; }
    }

    public float GetJumpHeight(float distance)
    {
        var vx = Flatten(velocity).magnitude;
        var t = distance / vx;
        var y = gravity * fallSpeed * Mathf.Pow(t, 2) / t;
        if (vx < Tolerance || y > jumpHeight) y = jumpHeight;
        return Inverted ? -y : y;
    }

    public float GetJumpHeightUncapped(float distance)
    {
        var vx = Flatten(velocity).magnitude;
        var t = distance / vx;
        var y = gravity * fallSpeed * Mathf.Pow(t, 2) / t;
        return Inverted ? -y : y;
    }

    public bool IsGrounded
    {
        get { return Environment.TickCount - _groundTimestamp < 150; }
    }

    public bool IsOnWall
    {
        get { return Environment.TickCount - _wallTimestamp < 150; }
    }

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

    private void Awake()
    {
        LookScale = 1;

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

        if (Input.GetAxis("Jump") > 0)
        {
            if (!_jumpLock)
            {
                _sinceJumpCounter = 0;
                _jumpLock = true;
                if (IsOnWall)
                    WallJump();
                else
                    Jump();
            }
        }
        else if (_jumpLock) _jumpLock = false;
        
        if (Time.timeScale < 1 && Time.timeScale > 0)
        {
            Time.timeScale += Time.unscaledDeltaTime;
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

        var cam = camera.transform;
        _cameraRoll = Mathf.Lerp(_cameraRoll, CameraRotation, Time.deltaTime * 10);
        camera.transform.rotation =
            Quaternion.Euler(new Vector3(Pitch + HudMovement.rotationSlamVectorLerp.y, Yaw, _cameraRoll));
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
        else if (_crouchAmount > 0)
        {
            _crouchAmount -= Time.deltaTime * 6;
            CameraRotation = 0;
        }

        position.y -= 0.6f * _crouchAmount;
        camera.transform.position = InterpolatedPosition + position;

        WallLean(0.3f, Time.deltaTime);

        if (_wallKickCounter == 0)
            multiplierText.text = "";
        else
            multiplierText.text = "x" + _wallKickCounter;
        multiplierText.fontSize = _wallKickCounter * 2 + 50;
    }

    private void FixedUpdate()
    {
        _sinceJumpCounter++;
        rigidbody.velocity = new Vector3();

        var factor = Time.fixedDeltaTime;

        Gravity(factor);

        GrappleMove(factor);
        WallMove(factor);
        SlideMove(factor);
        GroundMove(factor);
        AirMove(factor);

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
                var angle = Vector3.Angle(Vector3.up, hit.normal);
                if (angle < slopeAngle) _groundTimestamp = Environment.TickCount;
                else
                {
                    RaycastHit stair;
                    if (Math.Abs(angle - 90) < 0.05f && Physics.Raycast(hit.point - hit.normal * 0.2f + new Vector3(0, 3, 0), new Vector3(0, -1, 0), out stair, 6) &&
                        stair.point.y - (nextPosition.y - 1) < (IsSliding ? stairHeight + 0.7f : stairHeight))
                    {
                        _displacePosition += new Vector3(0, stair.point.y - (nextPosition.y - 1), 0);

                        var projection = Vector3.Dot(velocity, new Vector3(0, 1, 0));
                        if (projection > 0)
                        {
                            var impulse = new Vector3(0, -1, 0) * projection;
                            velocity += impulse;
                        }
                    }
                    else
                    {
                        var projection = Vector3.Dot(velocity, -hit.normal);
                        if (projection > 0)
                        {
                            var impulse = hit.normal * (projection - 4f);
                            velocity += impulse;
                        }

                        nextPosition = Vector3.Lerp(_previousPosition + platformMotion + velocity * factor,
                            nextPosition,
                            hit.distance / difference.magnitude);
                    }
                }
            }
        }

        _previousCollision = null;
        _previousPosition -= _displacePosition;
        rigidbody.MovePosition(nextPosition + _displacePosition);
        _displacePosition = new Vector3();
    }

    private void OnCollisionExit(Collision other)
    {
        _previousCollision = null;
        if (_momentumBuffer.Count < 2) return;
        if (Mathf.Abs(_momentumBuffer[0].x) > Mathf.Abs(velocity.x)) velocity.x = _momentumBuffer[0].x;
        if (Mathf.Abs(_momentumBuffer[0].y) > Mathf.Abs(velocity.y)) velocity.y = _momentumBuffer[0].y;
        if (Mathf.Abs(_momentumBuffer[0].z) > Mathf.Abs(velocity.z)) velocity.z = _momentumBuffer[0].z;
        _momentumBuffer.Clear();
    }

    private void OnCollisionStay(Collision other)
    {
        var moved = (rigidbody.transform.position - _displacePosition - _previousPosition) / Time.fixedDeltaTime;
        if (other.collider.CompareTag("Launch Block"))
        {
            var launch = other.collider.gameObject.GetComponent<LaunchBlock>();
            if (launch.IsAtApex)
                _momentumBuffer.Add(launch.maxSpeed * launch.Direction.normalized + moved);
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

        if (other.collider.CompareTag("Kill Block"))
        {
            Game.RestartLevel();
        }
        else if (other.collider.CompareTag("Launch Block"))
        {
            other.gameObject.GetComponent<LaunchBlock>().ActivateLaunch();
        }

        // Wall Grab
        var position = transform.position;
        var close = other.collider.ClosestPoint(position);
        var compare = close.y - position.y;
        if (compare < -0.9f || compare > 0.2f ||
            Math.Abs(Vector3.Angle(Vector3.up, other.contacts[0].normal) - 90) > Tolerance || IsGrounded)
        {
            if (!IsGrounded)
                _isSurfing = true;
            return;
        }

        _wallNormal = other.contacts[0].normal;
        _currentWall = other.collider;
        _wallTimestamp = Environment.TickCount;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Arrow"))
        {
            other.GetComponent<Arrow>().Explode();
        }
    }

    public void Teleport(Vector3 position)
    {
        _displacePosition += position - transform.position;
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

                CameraRotation = 20 * -value;
            }
            catch (Exception)
            {
                _wallTimestamp = 0;
                CameraRotation = 0;
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

            CameraRotation = 20 * rotation * -value;
        }
        else
        {
            if (_approachingWall)
            {
                _approachingWall = false;
                CameraRotation = 0;
            }
        }
    }

    public void WallMove(float f)
    {
        if (!IsOnWall) return;
        if (_sinceJumpCounter < greenKickTicks / 2)
        {
            velocity.y = _lastJumpBeforeYVelocity;
            _lastAirborneVelocity.y = _lastJumpBeforeYVelocity;
            WallJump();
        }

        var point = _currentWall.ClosestPoint(InterpolatedPosition);
        var ycompare = point.y - InterpolatedPosition.y;
        var distance = (Flatten(point) - Flatten(InterpolatedPosition)).magnitude;

        _wallTickCount++;

        DoubleJumpAvailable = true;

        if (ycompare < -0.9f || ycompare > 0.2f || distance > 0.5f * 2)
        {
            grindSound.volume = 0;
            CameraRotation = 0;
            return;
        }

        if (_currentWall.CompareTag("Launch Wall"))
        {
            Accelerate(new Vector3(0, 1, 0), 25, 1 * f);
        }
        else
        {
            var speed = Mathf.Abs(velocity.y);
            var control = speed < deceleration ? deceleration : speed;
            var drop = control * friction * f;

            var newspeed = speed - drop;
            if (newspeed < 0)
                newspeed = 0;
            if (speed > 0)
                newspeed /= speed;

            var towardWall = Flatten(point - InterpolatedPosition).normalized;
            if (_wallTickCount > greenKickTicks / 2)
            {
                source.pitch = 1;
                _wallKickCounter = 0;

                if (velocity.y < 0) velocity.y *= newspeed;
                ApplyFriction(wallFriction * f);
            }
            if (_wallTickCount == 1)
            {
                Accelerate(Vector3.up, 1, 10);
            }

            var direction = new Vector3(-towardWall.z, 0, towardWall.x);
            if (Vector3.Angle(CrosshairDirection, direction) < 90)
            {
                Accelerate(direction, wallSpeed, runAcceleration * f);
            }
            else
            {
                Accelerate(-direction, wallSpeed, runAcceleration * f);
            }

            Accelerate(towardWall, 4, 10 * f);
        }
    }

    public void WallJump()
    {
        if (!IsOnWall) return;

        _wallTimestamp = 0;
        CameraRotation = 0;
        if (Inverted) Inverted = false;

        var c = wallkickDisplay.color;
        if (_currentWall.CompareTag("Launch Wall")) Accelerate(new Vector3(0, 1, 0), 40, 0.2f);

        var x = velocity.x + 0.2f * 15 * _wallNormal.x;
        var z = velocity.z + 0.2f * 15 * _wallNormal.z;
        var jumpDir = new Vector3(x, 0, z).normalized;

        var newDir = Flatten(velocity).magnitude * jumpDir;
        velocity.x = newDir.x;
        velocity.z = newDir.z;

        DoubleJumpAvailable = true;
        _wallJumpTimestamp = Environment.TickCount;

        Accelerate(jumpDir, wallJumpSpeed, 0.15f);

        var up = _lastAirborneVelocity.y;

        if (_wallTickCount <= greenKickTicks / 2)
        {
            if (up < -wallKickFlingThreshold)
            {
                Inverted = true;
                velocity.y = 0;
                _removeInvertY = transform.position.y;
            }
            if (up > wallBumpThreshold)
            {
                Accelerate(Vector3.up, wallBumpSpeed, 1);
            }
            
            _wallKickCounter++;
            velocity += jumpDir * greenKickSpeed;
            source.pitch = 1 + (_wallKickCounter - 1) / 10f;
            source.PlayOneShot(wallKick);
            Accelerate(new Vector3(0, 1, 0), GetJumpHeight(8), 40);
            c.a = 1;
            c.r = 0;
            c.b = 0;
            c.g = 1;
        }
        else
        {
            Accelerate(new Vector3(0, 1, 0), GetJumpHeight(7), 40);
        }

        _momentumBuffer.Clear();

        source.PlayOneShot(wallJump);

        wallkickDisplay.color = c;
        _wallTickCount = 0;
        grindSound.volume = 0;
    }

    public void Gravity(float f)
    {
        if (IsOnWall) return;
        if (GrappleHooked) return;
        var direction = Inverted ? Vector3.up : Vector3.down;
        Accelerate(direction, fallSpeed, gravity * f);
    }

    public void GroundMove(float f)
    {
        if (!IsGrounded) return;
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

        if (grindSound.volume <= 0)
        {
            source.PlayOneShot(wallLand);
            var slideBoost = -_lastAirborneVelocity.y / 10;
            velocity += Flatten(velocity).normalized * slideBoost;
        }
        grindSound.pitch = Mathf.Min(Mathf.Max(velocity.magnitude / 10, 1), 2);
        grindSound.volume = Mathf.Min(velocity.magnitude / 10, 1);
        DoubleJumpAvailable = true;

        var projection = Vector3.Dot(Flatten(_slideLeanVector).normalized, camera.transform.right);
        CameraRotation = projection * 15 * _crouchAmount;

        ApplyFriction(f / 8);
        _slideLeanVector = Vector3.Lerp(_slideLeanVector, velocity, f * 4);
        AirAccelerate(Wishdir, surfAcceleration * f);
        Accelerate(Flatten(velocity).normalized, movementSpeed, runAcceleration * f);
    }

    public void AirMove(float f)
    {
        if (_targetSpeed > 0)
        {
            if (Flatten(velocity).magnitude > _targetSpeed)
                ApplyFriction(airFriction * f);
            else
                _targetSpeed = -1;
        }

        if (transform.position.y > _removeInvertY)
        {
            Inverted = false;
        }

        if (IsGrounded) return;
        if (GrappleHooked) return;
        if (IsOnWall) return;
        _wallTickCount = 0;
        _lastAirborneVelocity = velocity;

        if (Flatten(velocity).magnitude < movementSpeed / 1.5f)
        {
            Accelerate(Wishdir, movementSpeed / 1.5f, runAcceleration / 3 * f);
        }
        else
        {
            if (_isSurfing)
            {
                Accelerate(Wishdir, 1, surfAcceleration);
            }
            else
            {
                var time = (Environment.TickCount - _wallJumpTimestamp) / 500f;
                if (time > 1) time = 1;
                AirAccelerate(Wishdir, strafeAcceleration * f * time);
            }
        }
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
        const float wishSpeed = 0.4f;

        var currentSpeed = Vector3.Dot(velocity, wishdir);
        var addSpeed = wishSpeed - currentSpeed;
        if (addSpeed > 0)
        {
            var accelSpeed = accel;
            if (accelSpeed > addSpeed)
                accelSpeed = addSpeed;
            velocity += accelSpeed * Wishdir;
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

    public void Jump()
    {
        if (IsOnWall) return;

        if (!IsGrounded && !DoubleJumpAvailable) return;
        if (Inverted) Inverted = false;

        var speed = GetJumpHeight(7);
        if (_momentumBuffer.Count > 0 && _momentumBuffer[0].y > speed) speed += _momentumBuffer[0].y;

        _lastJumpBeforeYVelocity = velocity.y;
        if (velocity.y < speed) velocity.y = speed;

        if (IsGrounded)
        {
            source.PlayOneShot(jump);
        }
        else
        {
            DoubleJumpAvailable = false;
            source.PlayOneShot(jumpair);
        }

        CameraRotation = 0;

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