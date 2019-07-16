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
    public float sprintMovementScale = 1.6f;
    public float jumpHeight = 10f;
    public float bHopForgiveness = 0.1f;

    public AudioSource jump;
    public AudioSource doubleJump;
    public AudioSource land;
    public AudioSource ding;

    public Grapple grapple;

    public float Yaw { get; set; }
    public float Pitch { get; set; }

    public bool Sprinting { get; set; }

    public Bow bow;
    public Vector3 bowPosition = new Vector3(0.3f, -0.35f, 0.8f);

    public Vector3 velocity = new Vector3(0, 0, 0);

    public Vector2 startRotation;

    private void Start()
    {
        Game.I.Player = this;
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

    private Vector3 wishdir;

    private void Update()
    {
        // Mouse motion
        Yaw = (Yaw + Input.GetAxis("Mouse X")) % 360f;
        Pitch -= Input.GetAxis("Mouse Y");
        Pitch = Mathf.Max(Pitch, -90);
        Pitch = Mathf.Min(Pitch, 90);

        camera.transform.rotation =
            Quaternion.Euler(new Vector3(Pitch + HudMovement.rotationSlamVectorLerp.y, Yaw, CameraRotation));

        // Movement
        var t = MovementDirectionRadians;

        // Determine sprinting
        if (Input.GetAxis("Sprint") > 0) Sprinting = true;
        if (Mathf.Rad2Deg * t > 90 || Mathf.Rad2Deg * t < -90 || !IsMoving) Sprinting = false;

        t += Mathf.Deg2Rad * Yaw;
        wishdir = IsMoving ? new Vector3(Mathf.Sin(t), 0, Mathf.Cos(t)) : new Vector3();

        if (isGrounded())
        {
            // On ground movement
            RefreshDoubleJump();
            if (groundTimer == 0)
                land.Play();

            groundTimer += Time.deltaTime;
            var movementMod = Mathf.Min(1f, groundTimer);

            ApplyFriction(movementMod);

            if (IsMoving && movementEnabled)
            {
                GroundMove(movementMod);
            }
        }
        else
        {
            groundTimer = 0;

            // Air movement

            if (IsMoving)
            {
                var wishSpeed = wishdir.magnitude;

                var currentSpeed = Vector3.Dot(velocity, wishdir);
                var addSpeed = wishSpeed - currentSpeed;
                if (addSpeed > 0)
                {
                    var accelSpeed = airAcceleration * Time.deltaTime * wishSpeed;
                    if (accelSpeed > addSpeed)
                        accelSpeed = addSpeed;
                    if (movementEnabled) velocity += accelSpeed * wishdir;
                }
            }
        }

        // Handle Jump
        if (jumpLock && Input.GetAxis("Jump") < Tolerance) jumpLock = false;
        if (groundLock && Input.GetAxis("Jump") < Tolerance && isGrounded()) groundLock = false;
        if (movementEnabled)
        {
            if (Input.GetAxis("Jump") > 0)
            {
                if (isGrounded())
                {
                    if (!groundLock)
                    {
                        if (Jump(new Vector3(0, jumpHeight, 0), 1000, 100))
                        {
                            var slam = HudMovement.RotationSlamVector;
                            slam.y += 30;
                            HudMovement.RotationSlamVector = slam;
                        }
                    }
                }
                else if (!doubleJumpSpent)
                {
                    if (Jump(new Vector3(0, jumpHeight, 0), 1000, 100))
                    {
                        var slam = HudMovement.RotationSlamVector;
                        slam.y += 30;
                        HudMovement.RotationSlamVector = slam;
                        doubleJumpSpent = true;
                    }
                }
            }
        }

        // Gravity
        if (gravityEnabled) velocity.y -= gravity * Time.deltaTime;
        if (Mathf.Abs(velocity.y) < controller.minMoveDistance) velocity.y = -controller.minMoveDistance * 10;

        grapple.HandleGrapple();

        // Movement happens here
        controller.Move(velocity * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0, Yaw, 0);

        if (controller.velocity.magnitude > Tolerance && controller.velocity.magnitude < velocity.magnitude ||
            isGrounded())
            velocity = controller.velocity;

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

    public void GroundMove(float f)
    {
        var speed = movementSpeed;
        if (Sprinting) speed *= sprintMovementScale;

        Accelerate(wishdir, speed * f, runAcceleration);
    }

    public void ApplyFriction(float f)
    {
        var speed = velocity.magnitude;
        var control = speed < deceleration ? deceleration : speed;
        var drop = control * friction * Time.deltaTime * f;

        var newspeed = speed - drop;
        if (newspeed < 0)
            newspeed = 0;
        if (speed > 0)
            newspeed /= speed;

        velocity.x *= newspeed;
        velocity.z *= newspeed;
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
        accelspeed = accel * Time.deltaTime * wishspeed;
        if (accelspeed > addspeed)
            accelspeed = addspeed;

        velocity.x += accelspeed * wishdir.x;
        velocity.z += accelspeed * wishdir.z;
    }


    private bool doubleJumpSpent;
    private bool jumpLock;
    private bool groundLock;

    public bool Jump(Vector3 force, float downfriction, float upfriction)
    {
        if (jumpLock) return false;
        jumpLock = true;
        groundLock = true;

        if (velocity.y < force.y)
            velocity.y = force.y;
        velocity.x += force.x;
        velocity.z += force.z;

        jump.Play();
        return true;
    }

    public void RefreshDoubleJump()
    {
        doubleJumpSpent = false;
    }

    public bool isGrounded()
    {
        var layermask = ~(1 << 9);
        return controller.isGrounded || Physics.Raycast(transform.position, Vector3.down, 2.1f, layermask);
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}