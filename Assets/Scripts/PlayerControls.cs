using System;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PlayerControls : MonoBehaviour
{
    public new Camera camera;

    /* Movement Stuff */
    public bool movementEnabled = true;
    public float deceleration = 10f;
    public float friction = 5f;
    public float runAcceleration = 10f;
    public float airAcceleration = 50f;
    public float gravity = 14f;
    public bool gravityEnabled = true;
    public float movementSpeed = 50;
    public float jumpHeight = 10f;

    public AudioSource source;

    public AudioClip spring;
    public AudioClip jump;
    public AudioClip jumpair;
    public AudioClip land;
    public AudioClip ding;

    public AudioSource grindSound;

    public Grapple grapple;

    public float Yaw { get; set; }
    public float Pitch { get; set; }

    public float LookScale { get; set; }

    public Bow bow;
    public Vector3 bowPosition = new Vector3(0.3f, -0.35f, 0.8f);

    public Vector3 velocity = new Vector3(0, 0, 0);

    public Vector2 startRotation;

    private bool firstMove;

    private void Start()
    {
        LookScale = 1;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Yaw = startRotation.x;
        Pitch = startRotation.y;
    }

    public CharacterController controller;

    private const float Tolerance = 0.05f;

    public float MovementDirectionRadians
    {
        get { return Mathf.Atan2(Input.GetAxis("Right"), Input.GetAxis("Forward")); }
    }

    public bool IsMoving
    {
        get { return Math.Abs(Input.GetAxis("Forward")) > Tolerance || Math.Abs(Input.GetAxis("Right")) > Tolerance; }
    }

    private float groundTimer;

    public float CameraRotation { get; set; }

    public Vector3 Wishdir { get; set; }

    private void Update()
    {
        if (IsMoving && !firstMove)
        {
            firstMove = true;
            Game.StartTimer();
        }

        // Mouse motion
        Yaw = (Yaw + Input.GetAxis("Mouse X") * LookScale) % 360f;
        Pitch -= Input.GetAxis("Mouse Y") * LookScale;
        Pitch = Mathf.Max(Pitch, -90);
        Pitch = Mathf.Min(Pitch, 90);

        camera.transform.rotation =
            Quaternion.Euler(new Vector3(Pitch + HudMovement.rotationSlamVectorLerp.y, Yaw, CameraRotation));

        // Movement
        var t = MovementDirectionRadians;

        t += Mathf.Deg2Rad * Yaw;
        Wishdir = IsMoving ? new Vector3(Mathf.Sin(t), 0, Mathf.Cos(t)) : new Vector3();

        grindSound.pitch = Mathf.Min(Mathf.Max(velocity.magnitude / 16, 1), 2);

        if (isGrounded())
        {
            grindSound.volume = Mathf.Min(velocity.magnitude / 15, 1);
            // On ground movement
            if (groundTimer == 0)
                source.PlayOneShot(land);

            groundTimer += Time.deltaTime;
            var frictionMod = Mathf.Max(0f, Mathf.Min(1f, groundTimer));

            ApplyFriction(frictionMod * Time.deltaTime);

            if (IsMoving && movementEnabled)
            {
                var movementMod = Mathf.Max(0f, Mathf.Min(1f, groundTimer * 3 - 0.1f));
                GroundMove(movementMod);
            }
        }
        else
        {
            if (movementEnabled) grindSound.volume = 0;
            groundTimer = 0;

            // Air movement

            if (IsMoving)
            {
                AirAccelerate(Wishdir, airAcceleration * Time.deltaTime);
            }
        }

        // Handle Jump
        if (JumpLock && Input.GetAxis("Jump") < Tolerance) JumpLock = false;
        if (groundLock && Input.GetAxis("Jump") < Tolerance && isGrounded()) groundLock = false;
        if (movementEnabled)
        {
            if (Input.GetAxis("Jump") > 0)
            {
                if (isGrounded())
                {
                    if (!groundLock)
                    {
                        if (Jump(new Vector3(0, jumpHeight, 0)))
                        {
                            var slam = HudMovement.RotationSlamVector;
                            slam.y += 30;
                            HudMovement.RotationSlamVector = slam;
                        }
                    }
                }
            }
        }

        // Gravity
        if (gravityEnabled)
        {
            if (!isGrounded())
            {
                velocity.y -= gravity * Time.deltaTime;
            }
            else
            {
                if (velocity.y > -0.1)
                    velocity.y -= gravity * Time.deltaTime;
                if (velocity.y < -0.1)
                    velocity.y = -0.1f;
            }
        }

        if (Mathf.Abs(velocity.y) < controller.minMoveDistance) velocity.y = -controller.minMoveDistance * 10;

        grapple.HandleGrapple(true);

        // Movement happens here
        controller.Move(velocity * Time.deltaTime);

        transform.rotation = Quaternion.Euler(0, Yaw, 0);

        if (Input.GetAxis("Fire2") > 0)
        {
            if (grapple.Hooked)
            {
                grapple.Detach();
                JumpLock = true;
            }
        }

        // Handle bow position
        if (bow != null)
        {
            var bowAngle = velocity.y * 1.8f - 10;

            bowAngle -= bow.Drawback * 85;

            bowAngle = Mathf.Max(Mathf.Min(bowAngle, 0), -100);
            bow.transform.localRotation = Quaternion.Lerp(bow.transform.localRotation,
                Quaternion.Euler(new Vector3(90 - bowAngle, -90, -90)), Time.deltaTime * 6);

            var yCalc = velocity.y / bow.yVelocityReduction;

            yCalc -= bow.Drawback / 1.5f;

            yCalc = Mathf.Max(yCalc, -bow.yVelocityLimit);
            yCalc = Mathf.Min(yCalc, bow.yVelocityLimit / 6);

            var xCalc = velocity.x / bow.hVelocityReduction;

            xCalc = Mathf.Max(xCalc, bowPosition.x);
            xCalc = Mathf.Min(xCalc, bow.hVelocityLimit);

            var zCalc = velocity.z / bow.hVelocityReduction;

            zCalc += bow.Drawback / 3;

            zCalc = Mathf.Max(zCalc, -bow.hVelocityLimit);
            zCalc = Mathf.Min(zCalc, bow.hVelocityLimit);

            var finalPosition = bowPosition + new Vector3(xCalc, -yCalc, zCalc);

            if (isGrounded()) finalPosition += CameraBobbing.BobbingVector / 12;

            bow.transform.localPosition = Vector3.Lerp(bow.transform.localPosition, finalPosition, Time.deltaTime * 20);
            if (Input.GetAxis("Fire1") > 0)
            {
                if (bow.Drawback < 0.22f) bow.Drawback += Time.deltaTime / 4;
            }
            else if (bow.Drawback > 0)
            {
                var trans = camera.transform;
                bow.Fire(trans.position, trans.forward);
            }
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider.CompareTag("Skip Block")) Skip();
        else if (hit.collider.CompareTag("Bounce Block"))
        {
            velocity.y = 30;
            DoubleJump.doubleJumpSpent = false;
            source.PlayOneShot(spring);
        }
        else if (hit.collider.CompareTag("Launch Pad")) hit.gameObject.GetComponent<LaunchPad>().Launch();
        else
        {
            var vel = controller.velocity;
            velocity.x = vel.x;
            velocity.z = vel.z;
            if (!isGrounded())
                velocity.y = vel.y;
        }
    }

    public void GroundMove(float f)
    {
        var speed = movementSpeed;

        Accelerate(Wishdir, speed * f, runAcceleration * Time.deltaTime);
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
        float addspeed;
        float accelspeed;
        float currentspeed;

        currentspeed = Vector3.Dot(velocity, wishdir);
        addspeed = wishspeed - currentspeed;
        if (addspeed <= 0)
            return;
        accelspeed = accel * wishspeed;
        if (accelspeed > addspeed)
            accelspeed = addspeed;

        velocity.x += accelspeed * wishdir.x;
        velocity.z += accelspeed * wishdir.z;
    }

    public bool JumpLock { get; set; }
    private bool groundLock;

    public bool Jump(Vector3 force)
    {
        if (JumpLock) return false;
        JumpLock = true;
        groundLock = true;

        if (velocity.y < 0)
            velocity.y = 0;
        velocity.y += force.y;
        velocity.x += force.x;
        velocity.z += force.z;
        grindSound.volume = 0;

        groundTimestamp = 0;
        source.PlayOneShot(jump);
        return true;
    }

    public void Skip()
    {
        var wishdir = Wishdir;
        wishdir.y = 0.4f;
        velocity = wishdir.normalized * velocity.magnitude;
    }

    private int groundTimestamp;

    public bool isGrounded()
    {
        if (controller.isGrounded && velocity.y < 0) groundTimestamp = Environment.TickCount;
        return Environment.TickCount - groundTimestamp < 200;
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}