using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    private const float Tolerance = 0.05f;
    public CharacterController controller;
    public new Camera camera;

    /* Movement Stuff */
    public bool movementEnabled = true;
    public float deceleration = 10f;
    public float friction = 3f;
    public float runAcceleration = 6f;
    public float airAcceleration = 60f;
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

    public Image wallkickDisplay;

    public Vector3 velocity = new Vector3(0, 0, 0);

    /* Audio */
    public AudioSource source;
    public AudioSource grindSound;
    public AudioSource wallApproach;
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
    private Vector3 grappleAttachPosition;
    public int maxGrappleTimeMillis = 5000;
    public int maxGrappleDistance = 120;
    public bool GrappleHooked { get; set; }

    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public float LookScale { get; set; }

    public Vector2 startRotation;

    private bool firstMove;
    private bool jumpLock;
    private bool groundLock;
    private bool wishBounce;
    private bool approachingWall;
    private float approachingWallDistance;
    private float groundTimer;
    private int bounceTimestamp;
    private int groundTimestamp;
    private Collider currentWall;
    private int wallTickCount;
    private int grappleAttachTimestamp;

    public static bool DoubleJumpAvailable { get; set; }

    public static float MovementDirectionRadians
    {
        get { return Mathf.Atan2(Input.GetAxis("Right"), Input.GetAxis("Forward")); }
    }

    public static bool IsMoving
    {
        get { return Math.Abs(Input.GetAxis("Forward")) > Tolerance || Math.Abs(Input.GetAxis("Right")) > Tolerance; }
    }

    public bool IsGrounded
    {
        get
        {
            if (controller.isGrounded && velocity.y < 0) groundTimestamp = Environment.TickCount;
            return Environment.TickCount - groundTimestamp < 200;
        }
    }

    public bool IsOnWall { get; set; }

    public float CameraRotation { get; set; }
    public Vector3 Wishdir { get; set; }
    
    public Vector3 CrosshairDirection { get; set; }

    private void Start()
    {
        LookScale = 1;

        firstMove = false;
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

        if ((IsMoving || Input.GetAxis("Fire1") > 0) && !firstMove)
        {
            firstMove = true;
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
        camera.transform.rotation = Quaternion.Euler(new Vector3(Pitch + HudMovement.rotationSlamVectorLerp.y, Yaw, CameraRotation));
        transform.rotation = Quaternion.Euler(0, Yaw, 0);

        // Movement
        var t = MovementDirectionRadians;

        t += Mathf.Deg2Rad * Yaw;
        Wishdir = IsMoving ? new Vector3(Mathf.Sin(t), 0, Mathf.Cos(t)) : new Vector3();

        Gravity(Time.deltaTime);

        if (GrappleHooked)
            GrappleMove(Time.deltaTime);
        else if (IsOnWall)
            WallMove(Time.deltaTime);
        else if (IsGrounded)
            GroundMove(Time.deltaTime);
        else
            AirMove(Time.deltaTime);

        Jump();
        WallJump();
        Bounce();

        WallLean(0.3f, Time.deltaTime);

        controller.Move(velocity * Time.deltaTime);

        var vel = controller.velocity;
        if (vel.magnitude >= velocity.magnitude) return;
        velocity.x = vel.x;
        velocity.z = vel.z;
        if (!IsGrounded)
            velocity.y = vel.y;
    }

    private void FixedUpdate()
    {
        if (IsOnWall)
        {
            wallTickCount++;
            if (wallTickCount == 4)
            {
                grindSound.volume = 1;
                source.PlayOneShot(wallLand);
            }
        }
    }

    public void Bounce()
    {
        if (!wishBounce) return;
        wishBounce = false;
        if (Environment.TickCount - bounceTimestamp <= 1000) return;
        bounceTimestamp = Environment.TickCount;
        Accelerate(new Vector3(0, 1, 0), 26, 26);
        DoubleJumpAvailable = true;
        source.PlayOneShot(spring);
    }


    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider.CompareTag("Bounce Block"))
        {
            wishBounce = true;
        }
        else if (hit.collider.CompareTag("Kill Block"))
        {
            Game.RestartLevel();
        }

        //Wall Grab
        var position = transform.position;
        var point = hit.collider.ClosestPoint(position);
        var compare = point.y - position.y;
        if (compare < -0.9f || compare > 0) return;
        if (IsOnWall || IsGrounded) return;
        currentWall = hit.collider;
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
        grappleAttachPosition = point;
        GrappleHooked = true;
        gravityEnabled = false;
        DoubleJumpAvailable = true;
        grappleTether.enabled = true;
        grappleAttachTimestamp = Environment.TickCount;
        grappleDuring.volume = 1;

        var towardPoint = (grappleAttachPosition - transform.position).normalized;
        var yankProjection = Vector3.Dot(Game.I.Player.velocity, towardPoint);
        Game.I.Player.velocity -= towardPoint * yankProjection;
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

        var list = new List<Vector3>
            {new Vector3(0, grappleYOffset, 0), transform.InverseTransformPoint(grappleAttachPosition)};

        grappleTether.positionCount = list.Count;
        grappleTether.SetPositions(list.ToArray());

        if (Environment.TickCount - grappleAttachTimestamp > maxGrappleTimeMillis) DetachGrapple();

        var trans = transform.position;
        var camTrans = camera.transform;
        camTrans.localPosition = new Vector3();

        var towardPoint = (grappleAttachPosition - trans).normalized;

        var forward = camTrans.forward;
        var projection = Vector3.Dot(forward, towardPoint);
        if (projection < 0) DetachGrapple();

        var velocityProjection = Mathf.Abs(Vector3.Dot(velocity, camTrans.right));

        var relativePoint = transform.InverseTransformPoint(grappleAttachPosition);

        var value = Mathf.Atan2(relativePoint.z, relativePoint.x) * Mathf.Rad2Deg;
        value = Mathf.Abs(value);
        value -= 90;
        value /= 90;

        grappleDuring.pitch = velocity.magnitude / 30f;

        CameraRotation = Mathf.Lerp(CameraRotation, velocityProjection * value, f * 10);

        var yankProjection = Vector3.Dot(Game.I.Player.velocity, towardPoint);
        if (yankProjection < 0) Game.I.Player.velocity -= towardPoint * yankProjection;

        Accelerate(towardPoint, grappleSwingForce, 1 * f);
        Accelerate(velocity.normalized, grappleSwingForce / 4, 1 * f);
        Accelerate(Wishdir, grappleSwingForce, 0.2f * f);
    }

    public void WallLean(float t, float f)
    {
        if (IsOnWall)
        {
            try
            {
                var point = currentWall.ClosestPoint(transform.position);

                var relativePoint = transform.InverseTransformPoint(point);

                var value = Mathf.Atan2(relativePoint.z, relativePoint.x) * Mathf.Rad2Deg;
                value = Mathf.Abs(value);
                value -= 90;
                value /= 90;

                CameraRotation = Mathf.Lerp(CameraRotation, 18 * -value, f * 2);
            }
            catch (Exception)
            {
                IsOnWall = false;
                gravityEnabled = true;
                movementEnabled = true;
            }
        }
        else
        {
            if (!approachingWall) approachingWallDistance = 100000f;

            CameraRotation = Mathf.Lerp(CameraRotation, 0, f * 6);
        }

        RaycastHit hit;
        var layermask = ~(1 << 9);

        var pos = transform.position;
        var didHit = Physics.CapsuleCast(pos - new Vector3(0, 2f, 0), pos + new Vector3(0, 1f, 0),
            controller.radius, Flatten(velocity).normalized, out hit, velocity.magnitude * t,
            layermask);
        if (didHit && !IsGrounded && !IsOnWall)
        {
            var close = hit.point;

            var distance = Flatten(close - pos).magnitude - controller.radius;
            if (!(distance <= approachingWallDistance)) return;
            if (!approachingWall)
            {
                approachingWall = true;
                approachingWallDistance = distance;
                wallApproach.volume = 0;
                wallApproach.Play();
            }

            var rotation = (approachingWallDistance - distance) / approachingWallDistance;

            wallApproach.volume = rotation;
            wallApproach.pitch = 1 + rotation;

            var relativePoint = transform.InverseTransformPoint(hit.collider.ClosestPoint(pos));

            var value = Mathf.Atan2(relativePoint.z, relativePoint.x) * Mathf.Rad2Deg;
            value = Mathf.Abs(value);
            value -= 90;
            value /= 90;

            CameraRotation = Mathf.Lerp(CameraRotation, 30 * rotation * -value, f * 8);
        }
        else
        {
            approachingWall = false;
            wallApproach.volume = 0;
            if (wallApproach.isPlaying) wallApproach.Stop();
        }
    }

    public void WallMove(float f)
    {
        var position = transform.position;
        var point = currentWall.ClosestPoint(position);
        var ycompare = point.y - position.y;
        var distance = (Flatten(point) - Flatten(position)).magnitude;

        if (ycompare < -0.9f || ycompare > 0 || distance > controller.radius * 2)
        {
            IsOnWall = false;
            gravityEnabled = true;

            grindSound.volume = 0;
            wallTickCount = -1;
            return;
        }

        if (currentWall.CompareTag("Launch Wall"))
        {
            Accelerate(new Vector3(0, 1, 0), 25, 1 * f);
        }
        else
        {
            if (wallTickCount > wallNoFrictionTicks) ApplyFriction(wallFriction * f);
            var towardWall = Flatten(point - position).normalized;

            if (wallTickCount == 1) Accelerate(new Vector3(0, 1, 0), 2, 4);
            Accelerate(towardWall, 1, 1 * f);
            Accelerate(Flatten(velocity).normalized, wallSpeed, runAcceleration * f);
        }

        DoubleJumpAvailable = true;
    }

    public void WallJump()
    {
        if (!IsOnWall) return;
        if (jumpLock && Input.GetAxis("Jump") < Tolerance) jumpLock = false;
        if (jumpLock || !movementEnabled) return;

        if (!(Input.GetAxis("Jump") > 0)) return;
        jumpLock = true;

        if (wallTickCount < 0) return;

        var position = transform.position;
        var point = currentWall.ClosestPoint(position);
        var jumpDir = Flatten(position - point).normalized;
        IsOnWall = false;
        gravityEnabled = true;

        var c = wallkickDisplay.color;
        Accelerate(new Vector3(0, 1, 0), jumpHeight, 2);
        if (currentWall.CompareTag("Launch Wall")) Accelerate(new Vector3(0, 1, 0), 40, 1);
        
        Accelerate(jumpDir, wallJumpSpeed, 0.2f);

        switch (wallTickCount)
        {
            case 1:
                Accelerate(Flatten(velocity).normalized, wallJumpSpeed * 2f, 0.07f);
                source.PlayOneShot(wallKick);
                c.a = 1;
                c.r = 0;
                c.b = 0;
                c.g = 1;
                break;
            case 2:
                Accelerate(Flatten(velocity).normalized, wallJumpSpeed * 1.8f, 0.07f);
                source.PlayOneShot(wallKick);
                c.a = 1;
                c.r = 1;
                c.b = 0;
                c.g = 1;
                break;
            case 3:
                Accelerate(Flatten(velocity).normalized, wallJumpSpeed * 1.6f, 0.08f);
                source.PlayOneShot(wallKick);
                c.a = 1;
                c.r = 1;
                c.b = 0;
                c.g = 0;
                break;
            default:
                source.PlayOneShot(wallJump);
                break;
        }

        wallkickDisplay.color = c;
        wallTickCount = -1;
        grindSound.volume = 0;
    }

    public void Gravity(float f)
    {
        if (!gravityEnabled) return;
        if (!IsGrounded)
        {
            Accelerate(new Vector3(0, -1, 0), fallSpeed, gravity * f);
        }
        else
        {
            if (velocity.y > -0.1)
                velocity.y -= gravity * f;
            if (velocity.y < -0.1)
                velocity.y = -0.1f;
        }
    }

    public void GroundMove(float f)
    {
        grindSound.pitch = Mathf.Min(Mathf.Max(velocity.magnitude / 16, 1), 2);
        grindSound.volume = Mathf.Min(velocity.magnitude / 15, 1);
        if (groundTimer <= 0)
            source.PlayOneShot(land);

        groundTimer += f;
        var frictionMod = Mathf.Max(0f, Mathf.Min(1f, groundTimer));

        ApplyFriction(frictionMod * f);
        if (!IsMoving || !movementEnabled) return;

        var movementMod = Mathf.Max(0f, Mathf.Min(1f, groundTimer * 3 - 0.1f));
        Accelerate(Wishdir, movementSpeed, runAcceleration * movementMod * f);
    }

    public void AirMove(float f)
    {
        if (movementEnabled) grindSound.volume = 0;
        groundTimer = 0;
        if (!IsMoving) return;
        AirAccelerate(Wishdir, airAcceleration * f);
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
            if (movementEnabled) velocity += accelSpeed * Wishdir;
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
        if (jumpLock && Input.GetAxis("Jump") < Tolerance) jumpLock = false;
        if (groundLock && Input.GetAxis("Jump") < Tolerance && IsGrounded) groundLock = false;
        if (jumpLock || !movementEnabled) return;

        if (!(Input.GetAxis("Jump") > 0)) return;
        jumpLock = true;
        if (IsGrounded && groundLock) return;
        if (!IsGrounded && !DoubleJumpAvailable) return;

        Accelerate(new Vector3(0, 1, 0), jumpHeight, jumpHeight);

        if (IsGrounded)
        {
            groundLock = true;
            source.PlayOneShot(jump);
        }
        else
        {
            DoubleJumpAvailable = false;
            source.PlayOneShot(jumpair);
            AirAccelerate(Wishdir, 5);
        }

        grindSound.volume = 0;
        groundTimestamp = 0;

        var slam = HudMovement.RotationSlamVector;
        slam.y += 20;
        HudMovement.RotationSlamVector = slam;
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}