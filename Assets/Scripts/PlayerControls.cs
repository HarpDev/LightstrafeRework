using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerControls : MonoBehaviour
{
    public new Camera camera;
    public GameObject hud;
    public float movementSpeed = 50;
    public float sprintMovementScale = 1.6f;
    public float jumpHeight = 10f;
    public float fallSpeed = 14f;

    public float bobbingSpeed = 0.18f;
    public float bobbingWidth = 0.2f;
    public float bobbingHeight = 0.2f;
    private float bobbingPos;

    public Grapple grapple;

    public float Yaw { get; set; }
    public float Pitch { get; set; }

    private bool sprinting;

    public Bow bow;
    public Vector3 bowPosition = new Vector3(0.3f, -0.35f, 0.8f);

    public Vector3 velocity = new Vector3(0, 0, 0);

    public static GameObject Player { get; set; }

    private void Start()
    {
        Player = gameObject;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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

    public Text speedText;

    private bool jumpLock;

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
            bobbingPos += Flatten(velocity).magnitude / 100 * bobbingSpeed;
            while (bobbingPos > Mathf.PI * 2) bobbingPos -= Mathf.PI * 2;

            var y = bobbingHeight * Mathf.Sin(bobbingPos * 2);
            var x = bobbingWidth * Mathf.Sin(bobbingPos + 1.8f);
            bobbingVector = new Vector3(x, y, 0);
            camera.transform.localPosition = Vector3.Lerp(camera.transform.localPosition,
                sprinting ? bobbingVector : new Vector3(), Time.deltaTime);
        }

        // Movement
        speedText.text = velocity.magnitude + "";

        var forward = Input.GetAxis("Forward");
        var right = Input.GetAxis("Right");

        var t = Mathf.Atan2(right, forward);

        // Determine sprinting
        if (Input.GetAxis("Sprint") > 0) sprinting = true;
        if (Mathf.Rad2Deg * t > 90 || Mathf.Rad2Deg * t < -90 ||
            Math.Abs(forward) < Tolerance && Math.Abs(right) < Tolerance) sprinting = false;

        t += Mathf.Deg2Rad * Yaw;
        var direction = new Vector3(Mathf.Sin(t), 0, Mathf.Cos(t));

        if (isGrounded())
        {
            // On ground movement

                var reduction = 8f;
                reduction -= (velocity.magnitude + 1) / 5;
                reduction = Mathf.Max(2, reduction);
                var reduced = Vector3.Lerp(velocity, new Vector3(),
                    Time.deltaTime * reduction);
                velocity.x = reduced.x;
                velocity.z = reduced.z;

                if ((Math.Abs(forward) > Tolerance || Math.Abs(right) > Tolerance) && velocity.magnitude < 10)
                {
                    var speed = movementSpeed;
                    if (sprinting) speed *= sprintMovementScale;
                    velocity += direction * speed * Time.deltaTime;
                }
        }
        else
        {
            // Air movement

            if (Math.Abs(forward) > Tolerance || Math.Abs(right) > Tolerance)
            {
                var flatVelocity = Flatten(velocity);
                const float airAcceleration = 8f;
                if (flatVelocity.sqrMagnitude > Tolerance)
                {
                    var strafe = Vector3.Lerp(flatVelocity, flatVelocity.magnitude * direction,
                        airAcceleration / flatVelocity.magnitude * Time.deltaTime);

                    // Add speed if curving in the air
                    var strafeDistance =
                        Mathf.Sqrt(Mathf.Pow(velocity.x - strafe.x, 2) + Mathf.Pow(velocity.z - strafe.z, 2));
                    strafe += strafe.normalized * strafeDistance / 1.5f;

                    velocity.x = strafe.x;
                    velocity.z = strafe.z;
                }
                else velocity += direction * movementSpeed * Time.deltaTime;
            }
        }

        // Handle jump
        if (Input.GetAxis("Jump") > 0 && isGrounded() && !jumpLock)
        {
            jumpLock = true;
            velocity.y = jumpHeight;
        }
        else if (Input.GetAxis("Jump") < Tolerance && isGrounded() && jumpLock) jumpLock = false;

        // Gravity
        velocity.y -= fallSpeed * Time.deltaTime;
        if (Mathf.Abs(velocity.y) < controller.minMoveDistance) velocity.y = -controller.minMoveDistance * 10;

        grapple.HandleGrapple();

        // Movement happens here
        controller.Move(velocity * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0, Yaw, 0);
        camera.transform.rotation = Quaternion.Euler(new Vector3(Pitch + slamVectorLerp.y, Yaw, 0));

        // Collision momentum
        var collideVel = controller.velocity - velocity;
        slamVector += collideVel;
        slamVector = Vector3.Lerp(slamVector, new Vector3(0, 0, 0), Time.deltaTime * 8);
        slamVectorLerp = Vector3.Lerp(slamVectorLerp, slamVector, Time.deltaTime * 8);

        if (controller.velocity.magnitude > Tolerance || isGrounded())
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
                if (bow.Drawback < 0.3f) bow.Drawback += Time.deltaTime / 4;
            }
            else if (bow.Drawback > 0)
            {
                var trans = camera.transform;
                bow.Fire(trans.position, trans.forward);
            }
        }
    }

    public bool isGrounded()
    {
        return controller.isGrounded || Physics.Raycast(transform.position, Vector3.down, 2f);
    }

    private Vector3 RotateAroundX(Vector3 vec, float amt)
    {
        var y = vec.y * Mathf.Cos(amt) - vec.z * Mathf.Sin(amt);
        var z = vec.y * Mathf.Sin(amt) + vec.z * Mathf.Cos(amt);
        return new Vector3(vec.x, y, z);
    }

    private Vector3 RotateAroundY(Vector3 vec, float amt)
    {
        var x = vec.x * Mathf.Cos(amt) + vec.z * Mathf.Sin(amt);
        var z = -vec.x * Mathf.Sin(amt) + vec.z * Mathf.Cos(amt);
        return new Vector3(x, vec.y, z);
    }

    private Vector3 RotateAroundZ(Vector3 vec, float amt)
    {
        var x = vec.x * Mathf.Cos(amt) - vec.y * Mathf.Sin(amt);
        var y = vec.x * Mathf.Sin(amt) + vec.y * Mathf.Cos(amt);
        return new Vector3(x, y, vec.z);
    }

    private Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }

    private Vector3 Lengthen(Vector3 vec)
    {
        return new Vector3(0, vec.y, 0);
    }
}