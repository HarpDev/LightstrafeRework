using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    private const float Tolerance = 0.05f;
    public new Rigidbody rigidbody;
    public new Camera camera;
    public Collider standingHitbox;
    public Collider slidingHitbox;
    public Text wallkickDisplay;

    public Vector3 cameraPosition;
    public Vector3 velocity;

    /* Movement Stuff */
    public int wadeTime = 40;
    public float deceleration = 10f;
    public float friction = 5f;
    public float groundAcceleration = 8f;
    public float groundTurnAcceleration = 50;
    public float railSpeed = 20f;
    public float strafeAcceleration = 200f;
    public float stairHeight = 0.6f;
    public float surfAcceleration = 900f;
    public float gravity = 0.3f;
    public float movementSpeed = 11;
    public float jumpHeight = 12f;
    public float fallSpeed = 60f;
    public float wallSpeed = 12f;
    public float wallAcceleration = 8f;
    public float wallFriction = 0.5f;
    public float wallJumpSpeed = 3f;
    public int wallJumpForgiveness = 10;
    public float wallKickFlingThreshold = 15;
    public float wallKickFriction = 7;
    public float wallBumpThreshold = 8;
    public float wallBumpSpeed = 10;
    public float grappleSwingForce = 30f;
    public float grappleDetachFriction = 0.3f;
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
    private float _approachingWallDistance;
    private int _groundTimestamp = -100000;
    private int _wallTimestamp = -100000;
    private int _railTimestamp = -100000;
    private int _railCooldownTimestamp = -100000;
    private float _lastJumpBeforeYVelocity;
    private Collider _currentWall;
    private int _wallTickCount;
    private int _wallJumpTimestamp;
    private Vector3 _wallNormal;
    private int _grappleAttachTimestamp;
    private Transform _grappleAttachPosition;
    private Vector3 _previousPosition;
    private Collision _previousCollision;
    private Vector3 _previousCollisionLocalPosition;
    private Vector3 _displacePosition;
    private float _motionInterpolationDelta;
    private float _crouchAmount;
    private bool _isSurfing;
    private int _sinceJumpCounter;
    private Vector3 _lastAirborneVelocity;
    private float _removeInvertY;
    private int _railDirection;
    private Vector3 _railLeanVector;
    private GameObject _lastRail;
    private bool _wishJump;
    private int _wadeTicks;
    private float _cameraRotation;
    private float _cameraRotationSpeed;
    private bool _smoothRotation;
    private readonly List<Vector3> _momentumBuffer = new List<Vector3>();
    private CurvedLineRenderer _currentRail;

    public float CameraRoll { get; set; }

    public static bool DoubleJumpAvailable { get; set; }

    public static bool Inverted { get; set; }

    public float GetJumpHeight(float distance)
    {
        var vx = Flatten(velocity).magnitude;
        var t = distance / vx;
        var y = gravity * fallSpeed * Mathf.Pow(t, 2) / t;
        if (vx < Tolerance || y > jumpHeight) y = jumpHeight;
        return Inverted ? -y : y;
    }

    public bool IsGrounded { get; set; }

    public bool IsSliding
    {
        get { return Input.GetKey((KeyCode) PlayerInput.Key.Slide); }
    }

    public bool IsOnWall { get; set; }

    public bool IsOnRail
    {
        get { return _currentRail != null; }
    }

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

    public void SetCameraRotation(float value, float speed, bool smooth)
    {
        _cameraRotation = value;
        _cameraRotationSpeed = speed;
        _smoothRotation = smooth;
    }

    private void Awake()
    {
        LookScale = 1;

        Game.StopTimer();
        Game.ResetTimer();
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
        if (Input.GetKeyDown((KeyCode) PlayerInput.Key.Jump)) _wishJump = true;

        if (Input.GetKeyDown((KeyCode) PlayerInput.Key.StrafeRight) ||
            Input.GetKeyDown((KeyCode) PlayerInput.Key.StrafeLeft)) _wadeTicks = wadeTime;

        // Check for level restart
        if (Input.GetKeyDown((KeyCode) PlayerInput.Key.RestartLevel)) Game.RestartLevel();

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
                if (!IsOnRail)
                {
                    SetCameraRotation(0, 50, false);
                }
            }
        }

        position.y -= 0.6f * _crouchAmount;
        camera.transform.position = InterpolatedPosition + position;
    }

    private void FixedUpdate()
    {
        // Set Wishdir
        Wishdir = (transform.right * PlayerInput.GetAxisStrafeRight() +
                   transform.forward * PlayerInput.GetAxisStrafeForward()).normalized;

        // Start the timer when the player moves
        if ((Wishdir.magnitude > 0 || PlayerInput.SincePressed(PlayerInput.Key.FireBow) <= 1) && !Game.TimerRunning)
        {
            Game.StartTimer();
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

        // Ensure the rigidbody will stay stationary unless acted upon by this script
        rigidbody.velocity = new Vector3();

        // Remove inversion
        if (transform.position.y > _removeInvertY && Inverted) Inverted = false;

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

        // Count how many ticks the player has been on a wall (include coyote time)
        if (PlayerInput.tickCount - _wallTimestamp < coyoteTime)
            _wallTickCount++;
        else
            _wallTickCount = 0;

        // Wish jump should only be on for 1 tick when the input is sent
        _wishJump = false;

        // Interpolation delta resets to 0 every fixed update tick, its used to measure the time since the last fixed update tick
        _motionInterpolationDelta = 0;

        // Set is surfing to false, if the player is surfing it will be set to true again on the next collision
        _isSurfing = false;

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

            _previousCollisionLocalPosition =
                _previousCollision.transform.InverseTransformPoint(rigidbody.transform.position);
        }
        else _previousCollisionLocalPosition = new Vector3();

        // Calculate the players position for the next tick
        _previousPosition = rigidbody.transform.position;
        var movement = platformMotion + velocity * factor;

        // Here we check if the next position will collide with a surface, and if needed, adjust the next position so that they either:
        // A: collide with it instead of going through it
        // B: if the top of the surface is close enough, adjust the next position to be on top of the surface (step)
        RaycastHit hit;
        if (rigidbody.SweepTest(movement.normalized, out hit, movement.magnitude, QueryTriggerInteraction.Ignore))
        {
            
            var angle = Vector3.Angle(Vector3.up, hit.normal);
            if (angle < slopeAngle)
            {
                IsGrounded = true;
            }
            else
            {
                RaycastHit stair;
                var didHit = Physics.Raycast(hit.point - hit.normal * 0.3f + new Vector3(0, 3, 0), Vector3.down,
                    out stair, 6, 1, QueryTriggerInteraction.Ignore);

                var stepHeight = stair.point.y - (_previousPosition.y + movement.y - 1);
                
                if (Vector3.Angle(Vector3.up, hit.normal) < slopeAngle && didHit && stepHeight < stairHeight)
                {
                    Debug.Log("step");
                    _displacePosition += new Vector3(0, stepHeight, 0);

                    var projection = Vector3.Dot(velocity, new Vector3(0, 1, 0));
                    if (projection > 0)
                    {
                        var impulse = new Vector3(0, -1, 0) * projection;
                        velocity += impulse;
                    }
                }
                else
                {
                    var penetration = movement.magnitude - hit.distance;
                    Debug.Log(penetration);
                    //nextPosition = Vector3.Lerp(_previousPosition + platformMotion + velocity * factor,
                      //  nextPosition,
                        //hit.distance / difference.magnitude);
                }
            }
        }

        // The previous collision is set in oncollision, executed after fixedupdate
        _previousCollision = null;
        _previousPosition -= _displacePosition;
        rigidbody.MovePosition(_previousPosition + movement);
        _displacePosition = new Vector3();
    }

    private void OnCollisionExit(Collision other)
    {
        _previousCollision = null;
        if (_momentumBuffer.Count < 2) return;
        if (Mathf.Abs(_momentumBuffer[0].x) > Mathf.Abs(velocity.x)) velocity.x = _momentumBuffer[0].x;
        if (_momentumBuffer[0].y > velocity.y) velocity.y = _momentumBuffer[0].y;
        if (Mathf.Abs(_momentumBuffer[0].z) > Mathf.Abs(velocity.z)) velocity.z = _momentumBuffer[0].z;
        _momentumBuffer.Clear();
        IsGrounded = false;
        IsOnWall = false;
        SetCameraRotation(0, 50, false);
    }

    private void OnTriggerEnter(Collider other)
    {
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
            if (Vector3.Angle(Vector3.up, point.normal) < slopeAngle)
            {
                IsGrounded = true;
            }

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
        var meshCollider = other.collider as MeshCollider;
        if (meshCollider != null && !meshCollider.convex) return;

        var position = transform.position;
        var close = other.collider.ClosestPoint(position);
        var compare = close.y - position.y;
        if (compare < -0.9f || compare > 0.2f ||
            Math.Abs(Vector3.Angle(Vector3.up, other.contacts[0].normal) - 90) > Tolerance || IsGrounded)
        {
            if (!IsGrounded) _isSurfing = true;
            return;
        }

        _wallNormal = other.contacts[0].normal;
        _currentWall = other.collider;
        IsOnWall = true;
    }

    public void SetRail(CurvedLineRenderer rail)
    {
        if (IsOnRail) return;
        _currentRail = rail;
        source.PlayOneShot(railLand);
        railSound.Play();
        railSound.volume = 1;
        if (GrappleHooked) DetachGrapple(0);
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
        var range = Mathf.Min((_currentRail.smoothedPoints.Length - 1) / 2, 8);
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

        if (_wishJump)
        {
            Jump();
            return;
        }

        if (_railDirection == 1 && closeIndex >= _currentRail.smoothedPoints.Length - 1 ||
            _railDirection == -1 && closeIndex <= 0)
        {
            EndRail();
            return;
        }

        _railLeanVector = Vector3.Lerp(_railLeanVector, GetBalanceVector(closeIndex + _railDirection), f * 20);

        var current = _currentRail.smoothedPoints[closeIndex] + _railLeanVector;
        var next = _currentRail.smoothedPoints[closeIndex + _railDirection] + _railLeanVector;
        var railVector = -(current - next).normalized;
        var correctionVector = -(transform.position - next).normalized;
        velocity = velocity.magnitude * Vector3.Lerp(railVector, correctionVector, f * 20).normalized;

        var bonusSpeed = Vector3.Dot(_railLeanVector, Wishdir) * 5;
        Accelerate(velocity.normalized, railSpeed + bonusSpeed, f);
        Gravity(f);

        var totalAngle = Vector3.Angle(Vector3.up, _railLeanVector) / 2f;
        var projection = Vector3.Dot(_railLeanVector.normalized * totalAngle, -transform.right);
        SetCameraRotation(projection, 6, true);

        railSound.pitch = Mathf.Min(Mathf.Max(velocity.magnitude / 10, 1), 2);
    }

    public void Teleport(Vector3 position)
    {
        _displacePosition += position - _displacePosition - transform.position;
    }

    public void AttachGrapple(Transform position)
    {
        if (!enabled) return;
        source.PlayOneShot(grappleAttach);
        Game.I.Hitmarker.Display();
        if (GrappleHooked) return;
        if (IsOnRail) EndRail();
        _grappleAttachPosition = position;
        GrappleHooked = true;
        grappleTether.enabled = true;
        _grappleAttachTimestamp = Environment.TickCount;
        grappleDuring.volume = 1;

        var towardPoint = (_grappleAttachPosition.position - transform.position).normalized;
        var yankProjection = Vector3.Dot(velocity, towardPoint);
        velocity -= towardPoint * yankProjection;

        velocity.y = 0;
    }

    public void DetachGrapple(float f)
    {
        if (GrappleHooked) GrappleHooked = false;
        if (grappleTether.enabled) grappleTether.enabled = false;
        SetCameraRotation(0, 50, false);
        grappleDuring.volume = 0;
        var speed = velocity.magnitude;
        var control = speed < deceleration ? deceleration : speed;
        var drop = control * grappleDetachFriction * f;

        var newspeed = speed - drop;
        if (newspeed < 0)
            newspeed = 0;
        if (speed > 0)
            newspeed /= speed;

        velocity.x *= newspeed;
        if (!IsGrounded)
            velocity.y *= newspeed;
        velocity.z *= newspeed;
        source.PlayOneShot(grappleRelease);
    }

    public void GrappleMove(float f)
    {
        var camTrans = camera.transform;

        var list = new List<Vector3>
            {new Vector3(0, grappleYOffset, 0), camTrans.InverseTransformPoint(_grappleAttachPosition.position)};

        grappleTether.positionCount = list.Count;
        grappleTether.SetPositions(list.ToArray());

        if (Environment.TickCount - _grappleAttachTimestamp > maxGrappleTimeMillis) DetachGrapple(1);

        camTrans.localPosition = new Vector3();

        var towardPoint = (_grappleAttachPosition.position - camTrans.position).normalized;

        var forward = camTrans.forward;
        var projection = Vector3.Dot(forward, towardPoint);
        if (projection < 0) DetachGrapple(1);

        var velocityProjection = Mathf.Abs(Vector3.Dot(velocity, camTrans.right));

        var relativePoint = transform.InverseTransformPoint(_grappleAttachPosition.position);

        var value = Mathf.Atan2(relativePoint.z, relativePoint.x) * Mathf.Rad2Deg;
        value = Mathf.Abs(value);
        value -= 90;
        value /= 90;

        grappleDuring.pitch = velocity.magnitude / 30f;

        SetCameraRotation(velocityProjection * value, 6, true);

        var yankProjection = Vector3.Dot(velocity, towardPoint);
        if (yankProjection < 0) velocity -= towardPoint * yankProjection;

        var direction = Wishdir;
        if (Input.GetKey((KeyCode) PlayerInput.Key.Slide)) direction.y = -1;
        if (Input.GetKey((KeyCode) PlayerInput.Key.Jump)) direction.y = 1;

        Accelerate(towardPoint, grappleSwingForce, f * 2);
        Accelerate(direction.normalized, grappleSwingForce / 4, f * 2);
    }

    public void WallMove(float f)
    {
        if (PlayerInput.SincePressed(PlayerInput.Key.Jump) < wallJumpForgiveness)
        {
            if (_sinceJumpCounter < wallJumpForgiveness)
            {
                velocity.y = _lastJumpBeforeYVelocity;
                _lastAirborneVelocity.y = _lastJumpBeforeYVelocity;

                var slam = HudMovement.RotationSlamVector;
                slam.y -= 20;
                HudMovement.RotationSlamVector = slam;
            }

            if (_wallTickCount == 0)
            {
                for (var i = 0; i < PlayerInput.SincePressed(PlayerInput.Key.Jump); i++)
                {
                    if (Flatten(velocity).magnitude + wallJumpSpeed > Flatten(_lastAirborneVelocity).magnitude)
                        ApplyFriction(wallFriction * f * wallKickFriction);
                    else
                        ApplyFriction(wallFriction * f);
                }
            }

            Jump();
            return;
        }

        var point = _currentWall.ClosestPoint(InterpolatedPosition);
        var ycompare = point.y - InterpolatedPosition.y;
        var distance = (Flatten(point) - Flatten(InterpolatedPosition)).magnitude;

        DoubleJumpAvailable = true;

        if (ycompare < -0.9f || ycompare > 0.2f || distance > 0.5f * 2)
        {
            rollSound.volume = 0;
            SetCameraRotation(0, 50, false);
            return;
        }

        var relativePoint = camera.transform.InverseTransformPoint(point);

        var value = Mathf.Atan2(relativePoint.z, relativePoint.x) * Mathf.Rad2Deg;
        value = Mathf.Abs(value);
        value -= 90;
        value /= 90;

        SetCameraRotation(20 * -value, 10, true);

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
            source.pitch = 1;

            velocity.y *= newspeed;
            if (Flatten(velocity).magnitude + wallJumpSpeed > Flatten(_lastAirborneVelocity).magnitude &&
                _wallTickCount < wallJumpForgiveness)
                ApplyFriction(wallFriction * f * wallKickFriction);
            else
                ApplyFriction(wallFriction * f);

            var direction = new Vector3(-towardWall.z, 0, towardWall.x);
            if (Vector3.Angle(CrosshairDirection, direction) < 90)
                Accelerate(direction, wallSpeed, wallAcceleration * f);
            else
                Accelerate(-direction, wallSpeed, wallAcceleration * f);

            Accelerate(towardWall, 4, 10 * f);
        }
    }

    public void WallJump()
    {
        IsOnWall = false;
        _wallTimestamp = -coyoteTime;
        SetCameraRotation(0, 50, false);
        if (Inverted) Inverted = false;

        var x = velocity.x + 0.2f * 15 * _wallNormal.x;
        var z = velocity.z + 0.2f * 15 * _wallNormal.z;
        var jumpDir = new Vector3(x, 0, z).normalized;

        var newDir = Flatten(velocity).magnitude * jumpDir;
        velocity.x = newDir.x;
        velocity.z = newDir.z;
        velocity += jumpDir * wallJumpSpeed;

        DoubleJumpAvailable = true;
        _wallJumpTimestamp = Environment.TickCount;

        var up = _lastAirborneVelocity.y;

        if (_wallTickCount < wallJumpForgiveness)
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
        }

        if (PlayerInput.SincePressed(PlayerInput.Key.Jump) != 0)
            wallkickDisplay.text = "-" + PlayerInput.SincePressed(PlayerInput.Key.Jump);
        else
            wallkickDisplay.text = "+" + _wallTickCount;

        if (_wallTickCount < wallJumpForgiveness)
        {
            Accelerate(new Vector3(0, 1, 0), GetJumpHeight(8), 40);

            var c = wallkickDisplay.color;
            if (Flatten(velocity).magnitude > Flatten(_lastAirborneVelocity).magnitude)
            {
                c.g = 1;
                c.r = 0;
                c.b = 0;
                c.a = 1;
                source.PlayOneShot(wallKick);
            }
            else
            {
                c.g = 0;
                c.r = 1;
                c.b = 0;
                c.a = 1;
                source.PlayOneShot(wallJump);
            }

            wallkickDisplay.color = c;
        }
        else
        {
            Accelerate(new Vector3(0, 1, 0), GetJumpHeight(7), 40);
            source.PlayOneShot(wallJump);
        }

        _momentumBuffer.Clear();

        _wallTickCount = 0;
        rollSound.volume = 0;
    }

    public void Gravity(float f)
    {
        var direction = Inverted ? Vector3.up : Vector3.down;
        Accelerate(direction, fallSpeed, gravity * f);
    }

    public void GroundMove(float f)
    {
        if (rollSound.volume <= 0)
        {
            source.PlayOneShot(groundLand);
        }

        Gravity(f);
        DoubleJumpAvailable = true;
        var leanProjection = Vector3.Dot(Flatten(velocity).normalized, camera.transform.right);
        if (IsSliding)
        {
            AirAccelerate(Wishdir, surfAcceleration * f);
            SetCameraRotation(leanProjection * 15, 6, true);
        }
        else
        {
            //ApplyFriction(f / 4);

            AirAccelerate(Wishdir, groundTurnAcceleration * f);
            var right = transform.right;
            var frictionProjection = Vector3.Dot(velocity, right);

            var drop = frictionProjection * f;

            velocity -= right * drop;

            if (Math.Abs(PlayerInput.GetAxisStrafeRight()) > 0.05f && _wadeTicks > 0)
            {
                if (_wadeTicks == wadeTime) source.PlayOneShot(groundLand);
                _wadeTicks--;
                var scale = 1 - _wadeTicks / (wadeTime - 1f);
                //var a = 2;
                //var ease = Mathf.Pow(scale, a) / (Mathf.Pow(scale, a) + Mathf.Pow(1 - scale, a));
                var ease = Mathf.Sqrt(scale);

                var wishspeed = movementSpeed;
                var accel = groundAcceleration * f * ease;
                var wishdir = transform.forward;
                if (wishspeed < 0) wishdir = -wishdir;
                var currentspeed = Vector3.Dot(velocity, wishdir);
                var addspeed = Mathf.Abs(wishspeed) - currentspeed;

                SetCameraRotation(_wadeTicks / 4f * Vector3.Dot(-right, PlayerInput.GetAxisStrafeRight() * right), 6,
                    true);
                if (addspeed > 0)
                {
                    var accelspeed = Mathf.Abs(accel) * Mathf.Abs(wishspeed);
                    if (accelspeed > addspeed)
                        accelspeed = addspeed;

                    var add = wishdir * accelspeed;
                    var wade = PlayerInput.GetAxisStrafeRight() * add.magnitude * right;

                    velocity += add;
                    velocity += wade;
                }
            }
            else
            {
                SetCameraRotation(0, 6, true);
            }

            rollSound.pitch = Mathf.Min(Mathf.Max(velocity.magnitude / 20, 1), 2);
            rollSound.volume = Mathf.Min(velocity.magnitude / 30, 1);
        }

        if (_wishJump) Jump();
    }

    public void AirMove(float f)
    {
        _lastAirborneVelocity = velocity;
        Gravity(f);

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
        rollSound.volume = 0;

        var t = 0.25f;

        RaycastHit hit;

        var didHit = rigidbody.SweepTest(velocity.normalized, out hit, velocity.magnitude * t,
            QueryTriggerInteraction.Ignore);
        if (didHit && Math.Abs(Vector3.Angle(Vector3.up, hit.normal) - 90) < Tolerance &&
            !hit.collider.CompareTag("Kill Block"))
        {
            if (!_approachingWall)
            {
                _approachingWall = true;
            }

            var pos = InterpolatedPosition;
            var relativePoint = camera.transform.InverseTransformPoint(hit.collider.ClosestPoint(pos));

            var value = Mathf.Atan2(relativePoint.z, relativePoint.x) * Mathf.Rad2Deg;
            value = Mathf.Abs(value);
            value -= 90;
            value /= 90;

            SetCameraRotation(20 * -value, 50, false);
        }
        else
        {
            if (_approachingWall)
            {
                _approachingWall = false;
                SetCameraRotation(0, 50, false);
            }
        }

        if (_wishJump) Jump();
    }

    public void ApplyFriction(float f)
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
        if (Inverted) Inverted = false;

        if (PlayerInput.tickCount - _wallTimestamp < coyoteTime)
        {
            WallJump();
            return;
        }

        var groundJump = PlayerInput.tickCount - _groundTimestamp < coyoteTime;
        _groundTimestamp = -coyoteTime;
        var railJump = PlayerInput.tickCount - _railTimestamp < coyoteTime;
        _railTimestamp = -coyoteTime;

        if (!groundJump && !railJump && !DoubleJumpAvailable) return;
        var speed = GetJumpHeight(7);
        if (railJump) speed += velocity.y;
        if (_momentumBuffer.Count > 1 && _momentumBuffer.Count > 0 && _momentumBuffer[0].y > speed)
            speed += _momentumBuffer[0].y;

        _lastJumpBeforeYVelocity = velocity.y;
        if (velocity.y < speed) velocity.y = speed;

        if (groundJump)
        {
            source.PlayOneShot(jump);
        }
        else if (!railJump)
        {
            DoubleJumpAvailable = false;
            source.PlayOneShot(jumpair);
        }

        SetCameraRotation(0, 50, false);
        _sinceJumpCounter = 0;

        rollSound.volume = 0;
        IsGrounded = false;

        if (IsOnRail) EndRail();

        var slam = HudMovement.RotationSlamVector;
        slam.y += 20;
        HudMovement.RotationSlamVector = slam;
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}