using System;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PlayerControls : MonoBehaviour
{
    public new Camera camera;
    public GameObject hud;
    
    /* Movement Stuff */
    public float maxSpeed = 15;
    public float airAcceleration = 8f;
    public float gravity = 14f;
    public float movementSpeed = 50;
    public float sprintMovementScale = 1.6f;
    public float jumpHeight = 10f;
    public float bHopForgiveness = 0.1f;

    public Grapple grapple;

    public float Yaw { get; set; }
    public float Pitch { get; set; }

    private bool sprinting;

    private const float BobbingSpeed = 0.18f;
    private const float BobbingWidth = 0.2f;
    private const float BobbingHeight = 0.2f;
    private float bobbingPos;

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

    private Vector3 slamVector;
    private Vector3 slamVectorLerp;

    private float prevYaw;
    private float prevPitch;

    private float hudYawOffset;
    private float hudPitchOffset;
    public float hudMovementReduction = 6;

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

    private Vector3 prevVelocity;

    private float groundTimer;

    private void Update()
    {
        // Mouse motion
        Yaw = (Yaw + Input.GetAxis("Mouse X")) % 360f;
        Pitch -= Input.GetAxis("Mouse Y");
        Pitch = Mathf.Max(Pitch, -90);
        Pitch = Mathf.Min(Pitch, 90);

        // Camera bobbing
        var bobbingVector = new Vector3();
        if (Math.Abs(velocity.magnitude) > Tolerance && isGrounded())
        {
            bobbingPos += Flatten(velocity).magnitude * BobbingSpeed * Time.deltaTime * 2;
            while (bobbingPos > Mathf.PI * 2) bobbingPos -= Mathf.PI * 2;

            var y = BobbingHeight * Mathf.Sin(bobbingPos * 2);
            var x = BobbingWidth * Mathf.Sin(bobbingPos + 1.8f);
            bobbingVector = new Vector3(x, y, 0);
            camera.transform.localPosition = Vector3.Lerp(camera.transform.localPosition,
                sprinting ? bobbingVector * 3 : new Vector3(), Time.deltaTime);
        }

        // Movement
        var t = MovementDirectionRadians;

        // Determine sprinting
        if (Input.GetAxis("Sprint") > 0) sprinting = true;
        if (Mathf.Rad2Deg * t > 90 || Mathf.Rad2Deg * t < -90 || !IsMoving) sprinting = false;

        t += Mathf.Deg2Rad * Yaw;
        var direction = new Vector3(Mathf.Sin(t), 0, Mathf.Cos(t));

        if (isGrounded())
        {
            // On ground movement

            groundTimer += Time.deltaTime;
            if (groundTimer > bHopForgiveness)
            {
                var reduction = 8f;
                reduction -= Math.Max(velocity.magnitude - maxSpeed, 0);
                reduction = Mathf.Max(1, reduction);
                var reduced = Vector3.Lerp(velocity, new Vector3(),
                    Time.deltaTime * reduction);
                velocity.x = reduced.x;
                velocity.z = reduced.z;

                if (IsMoving && velocity.magnitude < maxSpeed)
                {
                    var speed = movementSpeed;
                    if (sprinting) speed *= sprintMovementScale;
                    velocity += speed * Time.deltaTime * direction;
                }
            }
        }
        else
        {
            groundTimer = 0;
            
            // Air movement

            if (IsMoving)
            {
                var accelDir = direction;
                
                var projVel = Vector3.Dot(velocity, accelDir); // Vector projection of Current velocity onto accelDir.
                var accelVel = airAcceleration * Time.deltaTime; // Accelerated velocity in direction of movment

                // If necessary, truncate the accelerated velocity so the vector projection does not exceed max_velocity
                if(projVel + accelVel < maxSpeed / 4)
                    velocity += accelDir * accelVel;
            }
        }

        // Handle Jump
        if (Input.GetAxis("Jump") > 0) Jump();
        if (jumpLock && Input.GetAxis("Jump") < Tolerance) jumpLock = false;
        if (groundLock && Input.GetAxis("Jump") < Tolerance && isGrounded()) groundLock = false;

        // Gravity
        velocity.y -= gravity * Time.deltaTime;
        if (Mathf.Abs(velocity.y) < controller.minMoveDistance) velocity.y = -controller.minMoveDistance * 10;

        grapple.HandleGrapple();

        // Movement happens here
        controller.Move(velocity * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0, Yaw, 0);
        camera.transform.rotation = Quaternion.Euler(new Vector3(Pitch + slamVectorLerp.y, Yaw, 0));

        // Collision momentum
        var collideVel = velocity - prevVelocity;
        slamVector += collideVel;
        slamVector = Vector3.Lerp(slamVector, new Vector3(), Time.deltaTime * 8);
        slamVectorLerp = Vector3.Lerp(slamVectorLerp, slamVector, Time.deltaTime * 8);
        prevVelocity = velocity;

        if (controller.velocity.magnitude > Tolerance && controller.velocity.magnitude < velocity.magnitude || isGrounded())
            velocity = controller.velocity;

        // Handle HUD momentum
        var yawMovement = prevYaw - Yaw;
        var pitchMovement = prevPitch - Pitch;
        if (yawMovement > 200) yawMovement = 0;
        hudYawOffset += yawMovement / hudMovementReduction;
        hudPitchOffset += pitchMovement / hudMovementReduction;
        hudYawOffset = Mathf.Lerp(hudYawOffset, 0, 0.05f);
        hudPitchOffset = Mathf.Lerp(hudPitchOffset, 0, 0.05f);
        hud.transform.localRotation = Quaternion.Euler(-hudPitchOffset, -hudYawOffset, 0);

        prevYaw = Yaw;
        prevPitch = Pitch;

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

            if (isGrounded()) finalPosition += bobbingVector / 12;

            bow.transform.localPosition = Vector3.Lerp(bow.transform.localPosition, finalPosition, Time.deltaTime * 20);
            if (Input.GetAxis("Fire1") > 0)
            {
                if (bow.Drawback < 0.22f) bow.Drawback += Time.deltaTime / 4;
            }
            else if (bow.Drawback > 0)
            {
                var trans = camera.transform;
                bow.Fire(trans.position, trans.forward, velocity);
            }
        }
    }


    private bool doubleJumpSpent;
    private bool jumpLock;
    private bool groundLock;

    public void Jump()
    {
        if (jumpLock) return;
        if (isGrounded() && !groundLock)
        {
            jumpLock = true;
            groundLock = true;
            velocity.y = jumpHeight;
            doubleJumpSpent = false;
        }

        if (!isGrounded() && !doubleJumpSpent)
        {
            jumpLock = true;
            doubleJumpSpent = true;
            var flat = Flatten(velocity);
            var magnitude = flat.magnitude;
            var target = new Vector3(magnitude * Mathf.Sin(MovementDirectionRadians + Yaw * Mathf.Deg2Rad), 0,
                magnitude * Mathf.Cos(MovementDirectionRadians + Yaw * Mathf.Deg2Rad));

            var factor = magnitude / 30;
            factor = Mathf.Min(factor, 0.8f);
            factor = Mathf.Max(factor, 0.4f);

            var lerp = Vector3.Lerp(target, flat, 0.6f);

            if (IsMoving)
            {
                velocity.x = lerp.x;
                velocity.z = lerp.z;
            }

            if (velocity.y < 0) velocity.y = jumpHeight;
            else velocity.y += jumpHeight;
        }
    }

    public void RefreshDoubleJump()
    {
        doubleJumpSpent = false;
    }

    public bool isGrounded()
    {
        return controller.isGrounded || Physics.Raycast(transform.position, Vector3.down, 2.1f);
    }

    private Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}