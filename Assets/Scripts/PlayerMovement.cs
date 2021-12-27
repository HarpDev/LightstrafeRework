using System;
using System.Collections.Generic;
using FullSerializer;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    public Collider hitbox;
    public WeaponManager weaponManager;

    public new Camera camera;
    public Transform cameraParent;
    public new Rigidbody rigidbody;

    public Vector3 cameraPosition;
    public Vector3 velocity;

    public bool jumpKitEnabled = true;

    public PlayerAudioManager AudioManager { get; set; }

    public float Yaw { get; set; }
    public float YawFutureInterpolation { get; set; }
    public float YawIncrease { get; set; }
    public float Pitch { get; set; }
    public float PitchFutureInterpolation { get; set; }
    public float LookScale { get; set; }
    public float CameraRoll { get; set; }
    public Vector3 Wishdir { get; set; }
    public Vector3 CrosshairDirection { get; set; }

    public Vector3 InterpolatedPosition => Vector3.Lerp(previousPosition, transform.position,
        motionInterpolationDelta / Time.fixedDeltaTime);

    private bool sliding;
    private float crouchAmount;

    public bool DashAvailable { get; set; }
    public bool GrappleEnabled;
    public bool DashEnabled;

    public bool IsSliding
    {
        // Give player a little control over sliding by allowing them to hold back/neutral to stand
        get
        {
            if (IsOnRail) return false;
            if (!PlayerInput.GetKey(PlayerInput.MoveForward) &&
                !PlayerInput.GetKey(PlayerInput.MoveBackward) &&
                !PlayerInput.GetKey(PlayerInput.MoveRight) &&
                !PlayerInput.GetKey(PlayerInput.MoveLeft) &&
                Wishdir.magnitude <= 0.05f &&
                Flatten(velocity).magnitude < SLIDE_BOOST_SPEED) return false;
            if (Vector3.Dot(Wishdir, Flatten(velocity).normalized) < -0.2f &&
                Flatten(velocity).magnitude < SLIDE_BOOST_SPEED)
            {
                return false;
            }

            return sliding;
        }
        private set => sliding = value;
    }

    public const int SURFACE_MAX_LEVEL = 5;

    private float motionInterpolationDelta;

    private float cameraRotation;
    private float cameraRotationSpeed;

    public void SetCameraRoll(float target, float speed)
    {
        cameraRotation = target;
        // Cap tilt speed to prevent camera tilt being too snappy
        if (speed > 150) speed = 150;
        cameraRotationSpeed = speed;
    }

    public const float BASE_SPEED = 10;

    public bool DoubleJumpAvailable { get; set; }

    /* Audio */
    public AudioClip jump;
    public AudioClip jumpair;
    public AudioClip dash;
    public AudioClip dashCancel;
    public AudioClip groundLand;
    public AudioClip slide;
    public AudioClip wallJump;
    public AudioClip wallRun;
    public AudioClip grappleAttach;
    public AudioClip grappleRelease;
    public AudioClip railLand;
    public AudioClip railDuring;
    public AudioClip railEnd;
    public AudioClip wow;

    public static bool IsPaused()
    {
        return Game.UiTree.Count != 0;
    }

    public static void Pause()
    {
        Time.timeScale = 0;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Game.OpenPauseMenu();
    }

    public static void Unpause()
    {
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /*
░█████╗░░██╗░░░░░░░██╗░█████╗░██╗░░██╗███████╗
██╔══██╗░██║░░██╗░░██║██╔══██╗██║░██╔╝██╔════╝
███████║░╚██╗████╗██╔╝███████║█████═╝░█████╗░░
██╔══██║░░████╔═████║░██╔══██║██╔═██╗░██╔══╝░░
██║░░██║░░╚██╔╝░╚██╔╝░██║░░██║██║░╚██╗███████╗
╚═╝░░╚═╝░░░╚═╝░░░╚═╝░░╚═╝░░╚═╝╚═╝░░╚═╝╚══════╝
    */
    private void Awake()
    {
        AudioManager = GetComponent<PlayerAudioManager>();
        LookScale = 1;

        //PlayerInput.ReadReplayFile("C:\\Users\\Fzzy\\Desktop\\replay.txt");
        GrappleCharges = GRAPPLE_CHARGES;

        Yaw += transform.rotation.eulerAngles.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        grappleTether.useWorldSpace = true;
        grappleTether.enabled = false;

        var positionOverride = GameObject.Find("PlayerStartPositionOverride");
        if (positionOverride != null)
        {
            transform.position = positionOverride.transform.position;
        }

        // Try to start the player on ground so it doesnt play the stupid ground land sound frame 1
        if (Physics.Raycast(transform.position, Vector3.down, out var hit, 5f, 1, QueryTriggerInteraction.Ignore) &&
            Vector3.Angle(hit.normal, Vector3.up) < GROUND_ANGLE)
        {
            transform.position = hit.point + Vector3.up * 0.8f;
            IsOnGround = true;
            currentGround = hit.collider.gameObject;
            groundTickCount = 2;
        }
    }

    /*
██╗░░░██╗██████╗░██████╗░░█████╗░████████╗███████╗
██║░░░██║██╔══██╗██╔══██╗██╔══██╗╚══██╔══╝██╔════╝
██║░░░██║██████╔╝██║░░██║███████║░░░██║░░░█████╗░░
██║░░░██║██╔═══╝░██║░░██║██╔══██║░░░██║░░░██╔══╝░░
╚██████╔╝██║░░░░░██████╔╝██║░░██║░░░██║░░░███████╗
░╚═════╝░╚═╝░░░░░╚═════╝░╚═╝░░╚═╝░░░╚═╝░░░╚══════╝
    */

    private float crosshairColor = 255;
    private float velocityThunk;
    private float velocityThunkSmoothed;
    private float previousYVelocity;

    private void Update()
    {
        jumpBuffered = Mathf.Max(jumpBuffered - Time.deltaTime, 0);

        if (!IsPaused() && Cursor.visible) Unpause();

        if (Cursor.visible) return;

        // Mouse aim / Controller aim
        if (Time.timeScale > 0)
        {
            YawIncrease = Input.GetAxis("Mouse X") * (Game.Sensitivity / 10) * LookScale;
            YawIncrease += Input.GetAxis("Joy 1 X 2") * Game.Sensitivity * LookScale;

            Yaw = (Yaw + YawIncrease) % 360f;

            var yawinterpolation = Mathf.Lerp(Yaw, Yaw + YawFutureInterpolation, Time.deltaTime * 10) - Yaw;
            Yaw += yawinterpolation;
            YawFutureInterpolation -= yawinterpolation;

            Pitch -= Input.GetAxis("Mouse Y") * (Game.Sensitivity / 10) * LookScale;
            Pitch += Input.GetAxis("Joy 1 Y 2") * Game.Sensitivity * LookScale;

            var pitchinterpolation = Mathf.Lerp(Pitch, Pitch + PitchFutureInterpolation, Time.deltaTime * 10) - Pitch;
            Pitch += pitchinterpolation;
            PitchFutureInterpolation -= pitchinterpolation;

            Pitch = Mathf.Max(Pitch, -90);
            Pitch = Mathf.Min(Pitch, 90);
        }

        // change rotation and color of the crosshair when dash is used
        Game.Canvas.crosshair.color = new Color(crosshairColor, crosshairColor, crosshairColor);

        // Fade out kick feedback
        if (kickFeedback != null && kickFeedback.color.a > 0)
        {
            var color = kickFeedback.color;
            color.a -= Time.deltaTime * (1.03f - color.a) * 4f;
            kickFeedback.color = color;
        }

        var crosshairRotated = false;
        if (GrappleEnabled)
        {
            var start = camera.transform.position + CrosshairDirection * 1;
            if (GrappleCast(start, CrosshairDirection, out var hit))
            {
                crosshairRotated = true;
            }
        }

        if (crosshairRotated)
        {
            Game.Canvas.crosshair.transform.rotation = Quaternion.Euler(0, 0,
                Mathf.Lerp(Game.Canvas.crosshair.transform.rotation.eulerAngles.z, 45, Time.deltaTime * 20));
        }
        else
        {
            Game.Canvas.crosshair.transform.rotation = Quaternion.Euler(0, 0,
                Mathf.Lerp(Game.Canvas.crosshair.transform.rotation.eulerAngles.z, 0, Time.deltaTime * 20));
        }

        if (DashAvailable)
        {
            crosshairColor = Mathf.Lerp(crosshairColor, 1, Time.deltaTime * 20);
        }
        else
        {
            crosshairColor = Mathf.Lerp(crosshairColor, 100f / 255f, Time.deltaTime * 20);
        }

        // This is where orientation is handled, the camera is only adjusted by the pitch, and the entire player is adjusted by yaw
        var cam = camera.transform;

        velocityThunk = Mathf.Lerp(velocityThunk, 0, Time.deltaTime * 4);
        velocityThunkSmoothed = Mathf.Lerp(velocityThunkSmoothed, velocityThunk, Time.deltaTime * 16);
        if (!IsDashing && PlayerInput.tickCount - dashEndTimestamp > 5)
            velocityThunk += (velocity.y - previousYVelocity) / 3f;
        previousYVelocity = velocity.y;

        camera.transform.localRotation = Quaternion.Euler(new Vector3(Pitch + velocityThunkSmoothed, 0, CameraRoll));
        CrosshairDirection = cam.forward;
        transform.rotation = Quaternion.Euler(0, Yaw, 0);

        // This value is used to calcuate the positions in between each fixedupdate tick
        motionInterpolationDelta += Time.deltaTime;

        CameraRoll -= Mathf.Sign(CameraRoll - cameraRotation) * Mathf.Min(cameraRotationSpeed * Time.deltaTime,
            Mathf.Abs(CameraRoll - cameraRotation));

        var position = cameraPosition;

        if (IsSliding)
        {
            crouchAmount = Mathf.Lerp(crouchAmount, 1, Time.deltaTime * 5);
        }
        else
        {
            crouchAmount = Mathf.Lerp(crouchAmount, 0, Time.deltaTime * 5);
        }

        var ease = 1 - Mathf.Pow(1 - crouchAmount, 2);
        position.y -= 0.5f * ease;
        position.y += 0.3f * (1 - ease);

        // Camera position is interpolated between ticks
        camera.transform.position = InterpolatedPosition + position;

        // Grapple tether visual
        if (GrappleHooked)
        {
            if (!grappleTether.enabled) grappleTether.enabled = true;

            var list = new List<Vector3>
            {
                camera.transform.position + Vector3.up * GRAPPLE_Y_OFFSET, grappleAttachPosition
            };

            grappleTether.positionCount = list.Count;
            grappleTether.SetPositions(list.ToArray());
        }

        // FOV increases with speed
        var targetFOV = Flatten(velocity).magnitude + (100 - BASE_SPEED);
        targetFOV = Mathf.Max(targetFOV, 100);
        targetFOV = Mathf.Min(targetFOV, 120);
        if (teleportTime > 0)
        {
            targetFOV += 50;
        }

        camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, targetFOV, Time.deltaTime * 5);
    }

    /*
███████╗██╗██╗░░██╗███████╗██████╗░██╗░░░██╗██████╗░██████╗░░█████╗░████████╗███████╗
██╔════╝██║╚██╗██╔╝██╔════╝██╔══██╗██║░░░██║██╔══██╗██╔══██╗██╔══██╗╚══██╔══╝██╔════╝
█████╗░░██║░╚███╔╝░█████╗░░██║░░██║██║░░░██║██████╔╝██║░░██║███████║░░░██║░░░█████╗░░
██╔══╝░░██║░██╔██╗░██╔══╝░░██║░░██║██║░░░██║██╔═══╝░██║░░██║██╔══██║░░░██║░░░██╔══╝░░
██║░░░░░██║██╔╝╚██╗███████╗██████╔╝╚██████╔╝██║░░░░░██████╔╝██║░░██║░░░██║░░░███████╗
╚═╝░░░░░╚═╝╚═╝░░╚═╝╚══════╝╚═════╝░░╚═════╝░╚═╝░░░░░╚═════╝░╚═╝░░╚═╝░░░╚═╝░░░╚══════╝
    */

    private void FixedUpdate()
    {
        wallRecovery -= Mathf.Min(wallRecovery, Time.fixedDeltaTime);

        if (PlayerInput.SincePressed(PlayerInput.Pause) == 0)
        {
            if (!IsPaused())
            {
                PlayerInput.WriteReplayToFile();
                Pause();
            }
        }

        // Check for level restart
        if (PlayerInput.SincePressed(PlayerInput.RestartLevel) == 0) Game.RestartLevel();

        // Set Wishdir
        Wishdir = (transform.right * PlayerInput.GetAxisStrafeRight() +
                   transform.forward * PlayerInput.GetAxisStrafeForward()).normalized;
        if (Wishdir.magnitude <= 0)
        {
            Wishdir = (transform.right * Input.GetAxis("Joy 1 X") + transform.forward * -Input.GetAxis("Joy 1 Y"))
                .normalized;
        }

        // Timestamps used for coyote time
        if (IsOnGround) groundTimestamp = PlayerInput.tickCount;
        if (IsOnWall) wallTimestamp = PlayerInput.tickCount;
        if (IsOnRail) railTimestamp = PlayerInput.tickCount;

        // Movement happens here
        var factor = Time.fixedDeltaTime;
        if (IsDashing)
            DashMove(factor);
        else if (GrappleHooked)
            GrappleMove(factor);
        else if (IsOnRail)
            RailMove(factor);
        else if (IsOnWall)
            WallMove(factor);
        else if (IsOnGround)
            GroundMove(factor);
        else
            AirMove(ref velocity, factor);

        if (PlayerInput.SincePressed(PlayerInput.PrimaryInteract) == 0 && GrappleEnabled)
        {
            if (GrappleHooked) DetachGrapple();
            else
            {
                var start = camera.transform.position + CrosshairDirection * 1;
                if (GrappleCast(start, CrosshairDirection, out var hit))
                {
                    AttachGrapple(hit);
                }
            }
        }

        if (PlayerInput.SincePressed(PlayerInput.SecondaryInteract) <= 25 && DashEnabled)
        {
            if (DashAvailable)
            {
                PlayerInput.ConsumeBuffer(PlayerInput.SecondaryInteract);
                Dash(CrosshairDirection);
                DashAvailable = false;
            }
        }

        // do teleport after movement so collision is done first
        // makes buffering work so you can tp onto a surface and be registered as on the surface before inputs are calculated
        tpThisTick = false;
        if (teleportTime > 0)
        {
            teleportTime -= Time.fixedDeltaTime;
            if (teleportTime <= 0)
            {
                teleportTime = 0;

                var tpVector = teleportToPosition - teleportStartPosition;
                velocity = Flatten(velocity).magnitude * Flatten(tpVector).normalized;
                lastWall = null;
                EndRail();
                ConsumeCoyoteTimeBuffer();
                transform.position = teleportToPosition + (Vector3.up * 0.9f);
                tpThisTick = true;
            }
        }

        if (!IsOnWall) wallTickCount = 0;
        if (!IsOnGround) groundTickCount = 0;
        if (IsOnGround) airTickCount = 0;

        if (dashTime > 0)
        {
            slideLeanVector = Vector3.Lerp(slideLeanVector, Flatten(velocity).normalized, factor * 4);

            if (!ApproachingWall)
            {
                var leanProjection = Vector3.Dot(slideLeanVector, camera.transform.right);
                SetCameraRoll(leanProjection * 15, CAMERA_ROLL_CORRECT_SPEED);
            }

            if (dashTime - factor <= 0)
                StopDash();
            else
                dashTime -= factor;
        }

        if (GrappleCharges < GRAPPLE_CHARGES) GrappleCharges += Time.deltaTime * GRAPPLE_RECHARGE_RATE;
        if (GrappleCharges > GRAPPLE_CHARGES) GrappleCharges = GRAPPLE_CHARGES;
        if (GrappleCharges < 0) GrappleCharges = 0;

        if (dashCancelTempSpeed > 0)
        {
            var loss = factor * DASH_CANCEL_TEMP_SPEED_DECAY;
            velocity -= Flatten(velocity).normalized * loss;
            Game.Canvas.speedChangeDisplay.interpolation += loss;
            dashCancelTempSpeed -= loss;
        }

        // Interpolation delta resets to 0 every fixed update tick, its used to measure the time since the last fixed update tick
        motionInterpolationDelta = 0;

        // This variable is the total movement that will occur in this tick
        var movement = velocity * Time.fixedDeltaTime;
        previousPosition = transform.position;

        if (IsOnGround)
        {
            groundLevel--;
            if (!IsOnGround)
            {
                SetCameraRoll(0, CAMERA_ROLL_CORRECT_SPEED);
                AudioManager.StopAudio(slide);
                AudioManager.StopAudio(groundLand);
            }
        }

        if (IsOnWall)
        {
            wallLevel--;
            if (!IsOnWall)
            {
                AudioManager.StopAudio(slide);
            }
        }

        var iterations = 0;

        // Hold helps collision hold you against walls/ground even if they get a little bumpy
        var hold = 0.1f;
        if (IsOnGround)
        {
            movement += Vector3.down * hold;
        }

        if (IsOnWall)
        {
            movement -= wallNormal * hold;
        }

        if (surfAccelTime > 0) surfAccelTime -= Time.fixedDeltaTime;

        while (movement.magnitude > 0f && iterations < 5)
        {
            iterations++;

            // this is fucked lol
            // good luck
            if (Math.Abs(hitbox.transform.localScale.y - 1) < 0.05f && !IsSliding)
            {
                hitbox.transform.localScale = new Vector3(1, 2, 1);
                hitbox.transform.position += Vector3.up;
            }

            if (Math.Abs(hitbox.transform.localScale.y - 2) < 0.05f && IsSliding)
            {
                hitbox.transform.localScale = new Vector3(1, 1, 1);
                hitbox.transform.position -= Vector3.up;
            }

            var reducedscale = hitbox.transform.localScale * (1f - hitbox.contactOffset * 2);
            reducedscale.y = IsSliding ? reducedscale.y : 2f - hitbox.contactOffset * 2;
            var realscale = hitbox.transform.localScale;
            if (!IsSliding) realscale.y = 2;
            hitbox.transform.localScale = reducedscale;

            // a lot of these numbers are really particular and finnicky
            transform.position -= movement.normalized * 0.2f;
            movement += movement.normalized * 0.2f;
            if (rigidbody.SweepTest(movement.normalized, out var hit, movement.magnitude,
                QueryTriggerInteraction.Ignore) && CanCollide(hit.collider))
            {
                hitbox.transform.localScale = realscale;
                transform.position += movement.normalized * (hit.distance + 0.05f);
                movement -= movement.normalized * hit.distance;

                if (Physics.ComputePenetration(hitbox, hitbox.transform.position, hitbox.transform.rotation,
                    hit.collider, hit.collider.gameObject.transform.position,
                    hit.collider.gameObject.transform.rotation,
                    out var direction, out var distance))
                {
                    if (hit.collider.isTrigger)
                    {
                        ContactTrigger(direction, hit.collider);
                        continue;
                    }

                    if (CanCollide(hit.collider))
                    {
                        // Collide
                        ContactCollider(hit.collider, ref direction, ref distance);

                        // If youre standing on slanted ground and not sliding, we want the player not to slowly slide down
                        // So we treat all slanted ground as perfectly flat when not sliding
                        var angle = Vector3.Angle(Vector3.up, direction);
                        if (angle < GROUND_ANGLE && !IsSliding) direction = Vector3.up;

                        // Depenetrate
                        transform.position += direction * distance;

                        // Apply this collision to the movement for this tick
                        var movementProjection = Vector3.Dot(movement, -direction);
                        if (movementProjection > 0) movement += direction * movementProjection;
                    }
                }
            }
            else
            {
                hitbox.transform.localScale = realscale;
                transform.position += movement;
                movement = Vector3.zero;
            }
        }

        // After doing sweep tests, use ComputePenetration() to ensure that the player is not intersecting with anything
        // Apply collision function to everything touching the player
        var bounds = hitbox.bounds;
        var overlap = Physics.OverlapBox(bounds.center, bounds.extents);
        foreach (var other in overlap)
        {
            if (!CanCollide(other)) continue;
            if (Physics.ComputePenetration(hitbox, hitbox.transform.position, hitbox.transform.rotation, other,
                other.transform.position, other.transform.rotation, out var direction, out var distance))
            {
                if (other.isTrigger)
                {
                    ContactTrigger(direction, other);
                    continue;
                }

                if (CanCollide(other))
                {
                    ContactCollider(other, ref direction, ref distance);
                }

                // If youre standing on slanted ground and not sliding, we want the player not to slowly slide down
                // So we treat all slanted ground as perfectly flat when not sliding
                var angle = Vector3.Angle(Vector3.up, direction);
                if (angle < GROUND_ANGLE && !IsSliding) direction = Vector3.up;
                transform.position += direction * distance;
            }
        }

        previousSpeed = Flatten(velocity).magnitude;
    }

    /*
░█████╗░░█████╗░██╗░░░░░██╗░░░░░██╗██████╗░███████╗
██╔══██╗██╔══██╗██║░░░░░██║░░░░░██║██╔══██╗██╔════╝
██║░░╚═╝██║░░██║██║░░░░░██║░░░░░██║██║░░██║█████╗░░
██║░░██╗██║░░██║██║░░░░░██║░░░░░██║██║░░██║██╔══╝░░
╚█████╔╝╚█████╔╝███████╗███████╗██║██████╔╝███████╗
░╚════╝░░╚════╝░╚══════╝╚══════╝╚═╝╚═════╝░╚══════╝
    */
    public const float STEP_HEIGHT = 1.2f;
    public const float VERTICAL_COLLIDE_INEFFICIENCY = 0.5f;
    private float surfAccelTime;

    private bool CanCollide(Component other, bool ignoreUninteractable = true)
    {
        if (other.gameObject == gameObject) return false;
        if (other.CompareTag("Player")) return false;
        if (other.CompareTag("Target")) return false;
        if (other.CompareTag("Projectile")) return false;
        if (!ignoreUninteractable && other.CompareTag("Uninteractable")) return false;
        return true;
    }

    private void ContactCollider(Collider collider, ref Vector3 normal, ref float distance)
    {
        if (!IsOnWall && Vector3.Dot(Flatten(CrosshairDirection).normalized, Flatten(normal).normalized) < -0.5f)
        {
            var feetPosition = transform.position + Vector3.down;
            var stepCheck = feetPosition - Flatten(normal).normalized * (hitbox.bounds.size.x / 2 + 0.05f) +
                            Vector3.up * STEP_HEIGHT;
            if (Physics.Raycast(stepCheck, Vector3.down, out var stepHit, STEP_HEIGHT, 1,
                    QueryTriggerInteraction.Ignore) &&
                !IsOnGround)
            {
                if (Vector3.Angle(stepHit.normal, Vector3.up) < GROUND_ANGLE)
                {
                    normal = Vector3.up;
                    distance = STEP_HEIGHT - stepHit.distance;
                }
            }
        }

        var angle = Vector3.Angle(Vector3.up, normal);

        if (angle < GROUND_ANGLE && !collider.CompareTag("Uninteractable") && !collider.CompareTag("Wall"))
        {
            IsOnGround = true;
            currentGround = collider.gameObject;

            // If youre standing on slanted ground and not sliding, we want the player not to slowly slide down
            // So we treat all slanted ground as perfectly flat when not sliding
            if (!IsSliding) normal = Vector3.up;
        }

        var velocityProjection = Vector3.Dot(velocity, -normal);
        var impulse = normal * velocityProjection;
        if (velocityProjection > 0)
        {
            // If there is a tp happening on this tick, apply speed from before the collision to the speed after
            // Effectively makes you not lose any speed from the collision
            if (tpThisTick)
            {
                var speed = Flatten(velocity).magnitude;
                velocity += impulse;
                var y = velocity.y;
                velocity = Flatten(velocity).normalized * speed;
                velocity.y = y;
            }
            else
            {
                var wishdir = velocity + impulse;
                var x2 = Flatten(wishdir).magnitude;
                var x1 = Flatten(velocity).magnitude;
                var y2 = wishdir.y;
                var y1 = x1 * y2 / x2;

                var verticalCollide = velocity + impulse * VERTICAL_COLLIDE_INEFFICIENCY;
                verticalCollide.y = y1;

                var impulseCollide = velocity + impulse;

                if (x2 != 0 && Flatten(verticalCollide).magnitude > Flatten(impulseCollide).magnitude &&
                    Mathf.Abs(angle - 90) >= WALL_VERTICAL_ANGLE_GIVE)
                {
                    velocity += impulse * VERTICAL_COLLIDE_INEFFICIENCY;
                    velocity.y = y1;
                }
                else
                {
                    velocity += impulse;
                }
            }
        }

        if (angle >= GROUND_ANGLE && Mathf.Abs(angle - 90) >= WALL_VERTICAL_ANGLE_GIVE)
        {
            surfAccelTime = 0.5f;
            DoubleJumpAvailable = true;
        }

        if (!collider.CompareTag("Uninteractable")
            && Mathf.Abs(angle - 90) < WALL_VERTICAL_ANGLE_GIVE
            && !IsOnGround && jumpKitEnabled
            && (collider.gameObject != lastWall || Vector3.Dot(Flatten(normal).normalized, lastWallNormal) < 0.7 ||
                WALL_ALLOW_SAME_FACING ||
                PlayerInput.tickCount - wallJumpTimestamp > WALL_ALLOW_SAME_FACING_COOLDOWN_TICKS))
        {
            // If the normal of a wall changes more than 10 degrees in 1 tick, kick you off the wall
            if (IsOnWall && Vector3.Angle(wallNormal, Flatten(normal).normalized) > 10)
            {
                Accelerate(wallNormal, WALL_END_BOOST_SPEED, WALL_END_BOOST_SPEED);
                IsOnWall = false;
                AudioManager.StopAudio(wallRun);
            }
            else
            {
                IsOnWall = true;
                wallNormal = Flatten(normal).normalized;

                currentWall = collider.gameObject;

                if (Flatten(velocity).magnitude < SLIDE_BOOST_SPEED && wallTickCount == 0)
                {
                    var fromSlideBoostSpeed = SLIDE_BOOST_SPEED - Flatten(velocity).magnitude;
                    velocity += Flatten(velocity).normalized * Mathf.Min(velocityProjection, fromSlideBoostSpeed);
                }
            }
        }
    }

    private void ContactTrigger(Vector3 normal, Collider other)
    {
        if (other.CompareTag("Rail") && (PlayerInput.tickCount - railTimestamp > RAIL_COOLDOWN_TICKS ||
                                         other.transform.parent.gameObject != lastRail))
        {
            lastRail = other.transform.parent.gameObject;
            SetRail(other.gameObject.transform.parent.gameObject.GetComponent<Rail>());
        }

        if (other.CompareTag("Kill Block"))
        {
            Game.RestartLevel();
        }

        var collectible = other.gameObject.GetComponent<Collectible>();
        if (collectible != null)
        {
            collectible.Collect();
        }
    }

    /*
████████╗███████╗██╗░░░░░███████╗██████╗░░█████╗░██████╗░████████╗
╚══██╔══╝██╔════╝██║░░░░░██╔════╝██╔══██╗██╔══██╗██╔══██╗╚══██╔══╝
░░░██║░░░█████╗░░██║░░░░░█████╗░░██████╔╝██║░░██║██████╔╝░░░██║░░░
░░░██║░░░██╔══╝░░██║░░░░░██╔══╝░░██╔═══╝░██║░░██║██╔══██╗░░░██║░░░
░░░██║░░░███████╗███████╗███████╗██║░░░░░╚█████╔╝██║░░██║░░░██║░░░
░░░╚═╝░░░╚══════╝╚══════╝╚══════╝╚═╝░░░░░░╚════╝░╚═╝░░╚═╝░░░╚═╝░░░
    */
    private float teleportTime;
    private float teleportTotalTime;
    private Vector3 teleportToPosition;
    private Vector3 teleportStartPosition;
    private bool tpThisTick;

    public void Teleport(Vector3 position)
    {
        teleportToPosition = position;
        teleportTime = 0.2f;
        teleportTotalTime = 0.2f;
        teleportStartPosition = transform.position;
    }

    /*
██████╗░░█████╗░░██████╗██╗░░██╗
██╔══██╗██╔══██╗██╔════╝██║░░██║
██║░░██║███████║╚█████╗░███████║
██║░░██║██╔══██║░╚═══██╗██╔══██║
██████╔╝██║░░██║██████╔╝██║░░██║
╚═════╝░╚═╝░░╚═╝╚═════╝░╚═╝░░╚═╝
    */
    public bool IsDashing => dashTime > 0;

    public const float DASH_SPEED = 45;
    public const float DASH_CANCEL_TEMP_SPEED = 15;
    public const float DASH_CANCEL_SPEED = 12;
    public const float DASH_CANCEL_TEMP_SPEED_DECAY = 20;
    public const float DASH_CANCEL_SPEEDCAP = 40;
    public const float DASH_WALL_UPCANCEL_MULTIPLY = 2f;
    public const float DASH_DISTANCE = 30f;
    public const float DASH_UPVELOCITY_LIMIT = 20;
    private float dashTime;
    private float dashCancelTempSpeed;
    private int dashEndTimestamp;
    private float speedBeforeDash;

    public void Dash(Vector3 wishdir)
    {
        AudioManager.PlayOneShot(dash);

        if (velocity.magnitude < SLIDE_BOOST_SPEED) velocity = velocity.normalized * SLIDE_BOOST_SPEED;
        speedBeforeDash = Flatten(velocity).magnitude;

        var angle = Vector3.Angle(wishdir, Vector3.up);
        if (Mathf.Abs(angle - 90) < 45)
        {
            var x2 = Flatten(wishdir).magnitude;
            var x1 = Flatten(velocity).magnitude;
            var y2 = wishdir.y;
            var y1 = x1 * y2 / x2;

            velocity = Flatten(velocity).magnitude * Flatten(wishdir).normalized;
            velocity.y = y1;
        }
        else
        {
            velocity = velocity.magnitude * wishdir.normalized;
        }

        DoubleJumpAvailable = true;

        velocity += wishdir.normalized * DASH_SPEED;
        dashTime = DASH_DISTANCE / velocity.magnitude;
    }

    public void DashMove(float f)
    {
        if (!IsOnGround && !IsOnWall) AirMove(ref velocity, f);

        if (IsOnGround) DashAvailable = true;
        PlayerJump();
    }

    public bool StopDash()
    {
        if (IsDashing)
        {
            SetCameraRoll(0, CAMERA_ROLL_CORRECT_SPEED);
            dashTime = 0;
            dashEndTimestamp = PlayerInput.tickCount;

            velocity -= velocity.normalized * DASH_SPEED;
            var y = velocity.y;
            if (velocity.y > 0 && Flatten(velocity).magnitude < speedBeforeDash)
                velocity = Flatten(velocity).normalized * speedBeforeDash;

            velocity.y = Mathf.Min(DASH_UPVELOCITY_LIMIT, y);
            return true;
        }

        return false;
    }

    public bool CancelDash()
    {
        if (StopDash())
        {
            AudioManager.PlayOneShot(dashCancel);
            velocity = Mathf.Max(velocity.magnitude, SLIDE_BOOST_SPEED) * velocity.normalized;

            var gain = DASH_CANCEL_TEMP_SPEED - dashCancelTempSpeed;
            var rawgain = DASH_CANCEL_SPEED *
                          Mathf.Clamp01((DASH_CANCEL_SPEEDCAP - Flatten(velocity).magnitude) / DASH_CANCEL_SPEEDCAP);
            velocity += Flatten(velocity).normalized * (gain + rawgain);
            dashCancelTempSpeed += gain;
            Game.Canvas.speedChangeDisplay.interpolation -= gain;

            return true;
        }

        return false;
    }

    /*
██████╗░░█████╗░██╗██╗░░░░░███╗░░░███╗░█████╗░██╗░░░██╗███████╗
██╔══██╗██╔══██╗██║██║░░░░░████╗░████║██╔══██╗██║░░░██║██╔════╝
██████╔╝███████║██║██║░░░░░██╔████╔██║██║░░██║╚██╗░██╔╝█████╗░░
██╔══██╗██╔══██║██║██║░░░░░██║╚██╔╝██║██║░░██║░╚████╔╝░██╔══╝░░
██║░░██║██║░░██║██║███████╗██║░╚═╝░██║╚█████╔╝░░╚██╔╝░░███████╗
╚═╝░░╚═╝╚═╝░░╚═╝╚═╝╚══════╝╚═╝░░░░░╚═╝░╚════╝░░░░╚═╝░░░╚══════╝
    */
    public bool IsOnRail => currentRail != null;

    public const int RAIL_COOLDOWN_TICKS = 100;
    public const float RAIL_SPEED = 25;
    public const float RAIL_ACCELERATION = 1;
    private int railTimestamp = -100000;
    private int railDirection;
    private int railTickCount;
    private Vector3 railVector;
    private Vector3 railLeanVector;
    private GameObject lastRail;
    private Rail currentRail;

    public void RailMove(float f)
    {
        if (!IsOnRail) return;
        DoubleJumpAvailable = true;
        DashAvailable = true;

        // Find the point on the rail closest to the player
        var closeIndex = 0;
        var closeDistance = float.MaxValue;
        for (var i = 0; i < currentRail.smoothedPoints.Length; i++)
        {
            var close = currentRail.smoothedPoints[i] + GetBalanceVector(i);
            var distance = Vector3.Distance(transform.position, close);
            if (distance > closeDistance) continue;
            closeDistance = distance;
            closeIndex = i;
        }

        // If rail direction is 0, we need to determine which direction the player should go based on their velocity
        // We will do this with a simple angle comparison of the players velocity, and seeing which direction makes more sense
        // After doing this, we will do a simple collision calculation on the rail, reducing player speed the harder they hit the rail
        if (railDirection == 0)
        {
            var c = currentRail.smoothedPoints[closeIndex];
            var p1Angle = 90f;
            if (closeIndex != 0)
            {
                var a = currentRail.smoothedPoints[closeIndex - 1];
                p1Angle = Vector3.Angle(Flatten(velocity), Flatten(a - c));
            }

            var p2Angle = 90f;
            if (closeIndex < currentRail.smoothedPoints.Length - 1)
            {
                var a = currentRail.smoothedPoints[closeIndex + 1];
                p2Angle = Vector3.Angle(Flatten(velocity), Flatten(a - c));
            }

            if (p1Angle < p2Angle)
            {
                railDirection = -1;
            }
            else
            {
                railDirection = 1;
            }

            try
            {
                var forward = currentRail.smoothedPoints[closeIndex - railDirection] -
                              currentRail.smoothedPoints[closeIndex];
                var p = Vector3.Dot(velocity, Flatten(forward).normalized);
                velocity = Flatten(velocity).normalized * p;
            }
            catch (IndexOutOfRangeException)
            {
            }
        }

        var current = currentRail.smoothedPoints[closeIndex] + railLeanVector;

        // If youre on the last point of a rail, we use the vector of the final 2 points to extrapolate 1 extra point on the rail
        Vector3 next;
        if (railDirection == 1 && closeIndex == currentRail.smoothedPoints.Length - 1)
        {
            next = current + (current - (currentRail.smoothedPoints[closeIndex - 1] + railLeanVector));
        }
        else if (railDirection == -1 && closeIndex == 0)
        {
            next = current + (current - (currentRail.smoothedPoints[1] + railLeanVector));
        }
        else
        {
            next = currentRail.smoothedPoints[closeIndex + railDirection] + railLeanVector;
        }

        var previousVector = railVector;
        railVector = -(current - next).normalized;

        // Should the rail forcefully end this tick (riding off the edge)
        if ((railDirection == -1 && closeIndex == 0 ||
             railDirection == 1 && closeIndex == currentRail.smoothedPoints.Length - 1) &&
            Vector3.Dot(transform.position - current, railVector) > 0)
        {
            EndRail();
            return;
        }

        // Get the vector from current player position to the next rail point and lerp them towards it
        var correctionVector = -(transform.position - next).normalized;
        velocity = velocity.magnitude * Vector3.Lerp(railVector, correctionVector, f * 80).normalized;
        Accelerate(velocity.normalized, RAIL_SPEED, RAIL_ACCELERATION, f);

        // Apply gravity only if the player is moving down
        // This makes them gain speed on downhill rails without losing speed on uphill rails // stonks
        if (velocity.y < 0) GravityTick(f);

        // The balance vector is a vector that attempts to mimick which direction you would intuitively lean
        // to not fall off the rail with real world physics, we will calculate a camera tilt based on it
        var balanceVector = GetBalanceVector(closeIndex + railDirection);
        railLeanVector = Vector3.Lerp(railLeanVector, balanceVector, f);
        var totalAngle = Vector3.Angle(Vector3.up, railLeanVector) / 2f;
        var roll = Vector3.Dot(railLeanVector.normalized * totalAngle, -transform.right);
        var speed = Mathf.Abs(CameraRoll - roll) / Time.fixedDeltaTime;
        SetCameraRoll(roll, speed);

        // This whole section attempts to guide the players view along the rail
        // If you ride a rail through a 180 degree turn, its annoying to have to move your mouse in that 180 every time
        // Automating this turn is hard to get right without feeling like the game is ripping control from you
        // It applies more turning force when facing outward on the rail turn, and less when facing inward
        // Players naturally look at their destination when doing a turn on a rail, so when they do that i dont want to move their view
        // But if their view drifts away we want to pull it back in, and keep it smooth enough that it doesnt feel like the game is man handling you
        if (railTickCount > 0)
        {
            VectorToYawPitch(previousVector, out var prevVecYaw, out var prevVecPitch);
            VectorToYawPitch(railVector, out var currVecYaw, out var currVecPitch);

            var yawChange = currVecYaw - prevVecYaw;
            var pitchChange = currVecPitch - prevVecPitch;

            var prevAngle = Vector3.Angle(Flatten(CrosshairDirection), Flatten(previousVector));
            var currAngle = Vector3.Angle(Flatten(CrosshairDirection), Flatten(railVector));

            var change = Vector3.Angle(Flatten(railVector), Flatten(previousVector));

            if (yawChange < 0) change *= -1;

            if (prevAngle > currAngle)
            {
                yawChange = change * Mathf.Pow(1 - (Mathf.Clamp(currAngle, 0, 90) / 90f), 2);
            }
            else
            {
                yawChange = change * Mathf.Clamp01(Mathf.Pow(1 + (Mathf.Clamp(currAngle, 0, 90) / 90f), 2) + 0.5f);
            }

            // Fade out/in yaw change before the rail ends and as it starts
            var pointsToEndOfRail =
                railDirection == 1 ? currentRail.smoothedPoints.Length - 1 - closeIndex : closeIndex;
            yawChange *= Mathf.Clamp(pointsToEndOfRail, 0, 5) / 5f;
            yawChange *= Mathf.Clamp(railTickCount - 1, 0, 10) / 10f;

            pitchChange /= 2;

            YawFutureInterpolation += yawChange;
            PitchFutureInterpolation += pitchChange;
        }

        PlayerJump();
        railTickCount++;
    }

    public void SetRail(Rail rail)
    {
        if (IsOnRail) return;
        currentRail = rail;
        AudioManager.PlayOneShot(railLand);
        AudioManager.PlayAudio(railDuring, true);
        railTickCount = 0;
        if (IsDashing) StopDash();
        railLeanVector = Vector3.up;
        if (rail.railDirection == Rail.RailDirection.BACKWARD)
        {
            railDirection = -1;
        }
        else if (rail.railDirection == Rail.RailDirection.FORWARD)
        {
            railDirection = 1;
        }

        if (GrappleHooked) DetachGrapple();
    }

    public void EndRail()
    {
        if (!IsOnRail) return;

        // Jump impulse is applied on rail end instead of PlayerJump() so that you get it even if you just ride off the end of the rail
        // Also only apply if moving mostly upwards, sometimes we want rails to throw the player down
        if (velocity.y > -4) velocity.y += MIN_JUMP_HEIGHT;

        SetCameraRoll(0, CAMERA_ROLL_CORRECT_SPEED);
        currentRail = null;
        AudioManager.StopAudio(railDuring);
        AudioManager.PlayOneShot(railEnd);
        railDirection = 0;
    }

    // I dont remember what i was thinking when i made this lol
    // You can use Debug.DrawRay() to see what it does
    private Vector3 GetBalanceVector(int i)
    {
        var range = Mathf.Min((currentRail.smoothedPoints.Length - 1) / 2, 4);
        var index = i;
        if (index < range) index = range;
        if (index >= currentRail.smoothedPoints.Length - range) index = currentRail.smoothedPoints.Length - range - 1;
        var point = currentRail.smoothedPoints[index];

        var p1 = currentRail.smoothedPoints[index - range];
        var p2 = currentRail.smoothedPoints[index + range];

        var a = Vector3.Dot(point - p1, (p2 - p1).normalized);
        var b = Vector3.Dot(point - p2, (p1 - p2).normalized);

        var ratio = a / (a + b);

        var p3 = Vector3.Lerp(p1, p2, ratio);

        var leanVector = p3 - point;
        leanVector.y = Mathf.Abs(leanVector.y);

        var balance = leanVector + Vector3.up * 0.4f;

        return balance.normalized;
    }

    /*
░██████╗░██████╗░░█████╗░██████╗░██████╗░██╗░░░░░███████╗
██╔════╝░██╔══██╗██╔══██╗██╔══██╗██╔══██╗██║░░░░░██╔════╝
██║░░██╗░██████╔╝███████║██████╔╝██████╔╝██║░░░░░█████╗░░
██║░░╚██╗██╔══██╗██╔══██║██╔═══╝░██╔═══╝░██║░░░░░██╔══╝░░
╚██████╔╝██║░░██║██║░░██║██║░░░░░██║░░░░░███████╗███████╗
░╚═════╝░╚═╝░░╚═╝╚═╝░░╚═╝╚═╝░░░░░╚═╝░░░░░╚══════╝╚══════╝
    */
    public LineRenderer grappleTether;
    public bool GrappleHooked { get; set; }
    public const float GRAPPLE_Y_OFFSET = -1.2f;
    public const float GRAPPLE_CONTROL_ACCELERATION = 20f;
    public const float GRAPPLE_ACCELERATION = 30f;
    public const float GRAPPLE_RANGE = 35;
    public const float GRAPPLE_SPEED = 25;
    public const float GFORCE_SPEEDGAIN_DOWN_FRICTION = 1.4f;
    public const float GFORCE_SPEEDGAIN_UP_FRICTION = 5f;
    public const float GRAPPLE_UPWARD_PULL = 30;
    public const float GRAPPLE_DOUBLEJUMP_IMPULSE = 4;
    public const int GRAPPLE_CHARGES = 2;
    public const float GRAPPLE_RECHARGE_RATE = 0.2f;
    public float GrappleCharges { get; set; }
    private int grappleTicks;
    private Vector3 grappleAttachPosition;

    public void GrappleMove(float f)
    {
        var position = grappleAttachPosition;

        var towardPoint = (position - transform.position).normalized;
        var velocityProjection = Vector3.Dot(velocity, towardPoint);
        var tangentVector = (velocity + towardPoint * -velocityProjection).normalized;

        var swingProjection = Mathf.Max(0, Vector3.Dot(velocity, Vector3.Lerp(towardPoint, tangentVector, 0.5f)));

        if (velocity.magnitude < 0.05f) velocity += towardPoint * f * GRAPPLE_ACCELERATION;

        var speed = velocity.magnitude;
        var controldir = new Vector3(Wishdir.x, CrosshairDirection.y, Wishdir.z).normalized;
        velocity += controldir * GRAPPLE_CONTROL_ACCELERATION * f;
        velocity = velocity.normalized * speed;
        if (velocity.y < 0)
        {
            GravityTick(f);
        }
        else
        {
            if (Vector3.Distance(Flatten(transform.position), Flatten(position)) > 15)
            {
                velocity += towardPoint.normalized * GRAPPLE_ACCELERATION * f * 2;
            }
            else
            {
                velocity -= Flatten(towardPoint).normalized * GRAPPLE_ACCELERATION * f;
            }

            velocity = velocity.normalized * speed;
        }

        if (velocity.magnitude < GRAPPLE_SPEED) velocity += towardPoint.normalized * GRAPPLE_ACCELERATION * f;

        if (velocityProjection < 0)
        {
            if (Mathf.Abs(Vector3.Angle(Vector3.up, velocity) - 90) < 20)
            {
                var wishdir = velocity + (towardPoint.normalized * -velocityProjection);
                var x2 = Flatten(wishdir).magnitude;
                var x1 = Flatten(velocity).magnitude;
                var y2 = wishdir.y;
                var y1 = x1 * y2 / x2;

                velocity.y = y1;
            }
            else
            {
                var beforeSwing = velocity;
                velocity += towardPoint.normalized * -velocityProjection;
                var verticalG = Mathf.Abs((towardPoint.normalized * -velocityProjection).y);
                if (Flatten(velocity).magnitude > Flatten(beforeSwing).magnitude)
                {
                    if (velocity.y > 0)
                    {
                        ApplyFriction(f * verticalG * GFORCE_SPEEDGAIN_UP_FRICTION, SLIDE_BOOST_SPEED);
                    }
                    else
                    {
                        ApplyFriction(f * verticalG * GFORCE_SPEEDGAIN_DOWN_FRICTION, SLIDE_BOOST_SPEED);
                    }
                }
            }
        }

        if (velocity.y > 0)
        {
            var verticalProjection = Vector3.Dot(velocity.normalized, Vector3.up);
            velocity += Vector3.up * verticalProjection * GRAPPLE_UPWARD_PULL * f;
        }

        var gain = 0.3f;
        var absolute = (gain / f) / 6f;
        var projection = Mathf.Sqrt(swingProjection) * Vector3.Dot(-transform.right, towardPoint) * absolute;
        SetCameraRoll(projection, CAMERA_ROLL_CORRECT_SPEED);

        if (Vector3.Dot(towardPoint, CrosshairDirection) < 0) DetachGrapple();

        grappleTicks++;
        PlayerJump();
    }

    public void AttachGrapple(Vector3 position)
    {
        if (GrappleHooked) return;
        AudioManager.PlayOneShot(grappleAttach);
        if (IsOnRail) EndRail();
        grappleAttachPosition = position;
        GrappleHooked = true;
        grappleTicks = 0;
        DoubleJumpAvailable = true;
        GrappleCharges--;
    }

    public void DetachGrapple()
    {
        if (GrappleHooked) GrappleHooked = false;
        if (grappleTether.enabled) grappleTether.enabled = false;
        SetCameraRoll(0, CAMERA_ROLL_CORRECT_SPEED);

        AudioManager.PlayOneShot(grappleRelease);
    }

    public bool GrappleCast(Vector3 origin, Vector3 direction, out Vector3 hit)
    {
        hit = Vector3.zero;
        if (GrappleCharges < 1) return false;

        if (Physics.Raycast(origin, direction, out var rayhit, GRAPPLE_RANGE))
        {
            hit = rayhit.point;
            return true;
        }
        else if (Physics.SphereCast(origin, 2f, direction, out var spherehit, GRAPPLE_RANGE))
        {
            hit = spherehit.point;
            return true;
        }

        return false;
    }


    private void VectorToYawPitch(Vector3 vector, out float yaw, out float pitch)
    {
        yaw = Mathf.Atan2(vector.x, vector.z) * Mathf.Rad2Deg;
        pitch = -Mathf.Asin(vector.y) * Mathf.Rad2Deg;
    }

    private void LookTowardTick(Vector3 vector, float reduction)
    {
        VectorToYawPitch(vector, out var tangyaw, out var tangpitch);

        var yawChange = tangyaw - Yaw;
        if (Mathf.Abs(yawChange) > 200)
        {
            if (yawChange > 200) yawChange -= 360;
            else if (yawChange < 200) yawChange += 360;
        }

        var pitchChange = tangpitch - Pitch;
        YawFutureInterpolation += yawChange / reduction;
        PitchFutureInterpolation += pitchChange / reduction;
    }

    /*
░██╗░░░░░░░██╗░█████╗░██╗░░░░░██╗░░░░░███╗░░░███╗░█████╗░██╗░░░██╗███████╗
░██║░░██╗░░██║██╔══██╗██║░░░░░██║░░░░░████╗░████║██╔══██╗██║░░░██║██╔════╝
░╚██╗████╗██╔╝███████║██║░░░░░██║░░░░░██╔████╔██║██║░░██║╚██╗░██╔╝█████╗░░
░░████╔═████║░██╔══██║██║░░░░░██║░░░░░██║╚██╔╝██║██║░░██║░╚████╔╝░██╔══╝░░
░░╚██╔╝░╚██╔╝░██║░░██║███████╗███████╗██║░╚═╝░██║╚█████╔╝░░╚██╔╝░░███████╗
░░░╚═╝░░░╚═╝░░╚═╝░░╚═╝╚══════╝╚══════╝╚═╝░░░░░╚═╝░╚════╝░░░░╚═╝░░░╚══════╝
    */
    // Surfaces have a "level" to make them a little more sticky than being able to come off them in 1 tick.
    // This is to prevent repeated landings on surfaces.
    public bool IsOnWall
    {
        get => wallLevel > 0;
        set
        {
            if (value) wallLevel = SURFACE_MAX_LEVEL;
            else wallLevel = 0;
        }
    }

    private int wallLevel;
    public bool ApproachingWall { get; set; }
    public const float WALL_CATCH_FRICTION = 10f;
    public const float WALL_JUMP_ANGLE = 0.4f;
    public const float WALL_VERTICAL_ANGLE_GIVE = 10f;
    public const float WALL_AIR_ACCEL_RECOVERY = 0.35f;
    public const float WALL_END_BOOST_SPEED = 2;
    public const float WALL_LEAN_DEGREES = 15f;
    public const float WALL_SPEED = 10;
    public const float WALL_ACCELERATION = 1f;
    public const float WALL_LEAN_PREDICTION_TIME = 0.25f;
    public const float WALL_JUMP_SPEED = 6;
    public const int WALL_FRICTION_TICKS = 4;
    public const float WALL_FRICTION = 7.5f;
    public const bool WALL_ALLOW_SAME_FACING = false;
    public const int WALL_ALLOW_SAME_FACING_COOLDOWN_TICKS = 200;
    private Vector3 wallNormal;
    private Vector3 lastWallNormal;
    private bool wallLeanCancelled;
    private int wallTimestamp = -100000;
    private int wallTickCount;
    private int wallJumpTimestamp;
    private GameObject lastWall;
    private GameObject currentWall;
    private float wallRecovery;
    private float wallLeanAmount;
    private float wallLeanLerp;

    public Text kickFeedback;

    public void WallMove(float f)
    {
        DoubleJumpAvailable = true;
        lastGround = null;
        if (PlayerJump()) return;

        if (wallTickCount == 0)
        {
            AudioManager.PlayAudio(wallRun, true);
            GrappleCharges += 0.3f;
        }

        // Fade in wall run sound so if you jump off right away its silent
        AudioManager.SetVolume(wallRun, Mathf.Clamp01(wallTickCount / 10f));
        wallTickCount++;

        // Apply friction on walls only for a few ticks at the start of the wall
        if (wallTickCount <= WALL_FRICTION_TICKS)
        {
            ApplyFriction(f * WALL_FRICTION, BASE_SPEED);
        }

        // Apply camera roll from the wall
        var normal = Flatten(wallNormal);
        var projection = Vector3.Dot(CrosshairDirection, new Vector3(-normal.z, normal.y, normal.x));
        wallLeanAmount = Mathf.Clamp01(wallLeanAmount + Time.fixedDeltaTime / 0.1f);
        var roll = WALL_LEAN_DEGREES * -projection * wallLeanAmount;
        wallLeanLerp = wallTickCount == 1 ? roll : Mathf.Lerp(wallLeanLerp, roll, f * 8);
        var speed = Mathf.Abs(CameraRoll - wallLeanLerp) / Time.fixedDeltaTime;
        SetCameraRoll(wallLeanLerp, speed);

        if (Vector3.Dot(Flatten(velocity).normalized, Flatten(Wishdir).normalized) > 0.6f)
        {
            var alongView = (Wishdir - (Flatten(wallNormal).normalized *
                                        Vector3.Dot(Wishdir, Flatten(wallNormal).normalized)))
                .normalized;
            Accelerate(alongView, WALL_SPEED, WALL_ACCELERATION * 4, f);
        }

        // If you hold back on a wall, apply friction and accelerate in the opposite direction so you can turn around on walls
        if (Vector3.Dot(Flatten(velocity).normalized, Flatten(Wishdir).normalized) < -0.6f)
        {
            var alongView = (Wishdir - (Flatten(wallNormal).normalized *
                                        Vector3.Dot(Wishdir, Flatten(wallNormal).normalized)))
                .normalized;
            Accelerate(alongView, WALL_SPEED, WALL_ACCELERATION * 4, f);
            ApplyFriction(f * WALL_FRICTION);
        }
        else
        {
            // Apply wall speed in direction youre already going
            if (Flatten(velocity).magnitude > 0)
            {
                var s = Mathf.Lerp(WALL_SPEED, SLIDE_BOOST_SPEED, Mathf.Clamp01(wallTickCount / 200f));
                Accelerate(Flatten(velocity).normalized, s, WALL_ACCELERATION, f);
            }
            else
            {
                // If your velocity is 0, we cant accelerate in the direction youre moving, so we use CrosshairDirection instead
                var alongView = (CrosshairDirection - (Flatten(wallNormal).normalized *
                                                       Vector3.Dot(CrosshairDirection, Flatten(wallNormal).normalized)))
                    .normalized;
                Accelerate(alongView, BASE_SPEED, BASE_SPEED, f);
            }
        }

        // Reduce y velocity while on walls, breaking falls
        // This also applies to moving upwards on walls
        velocity.y = Mathf.Lerp(velocity.y, 0, f * WALL_CATCH_FRICTION);
        if (Mathf.Abs(velocity.y) < 5) velocity.y = 0;

        // Push you into the wall, holding you against it a bit
        Accelerate(-wallNormal, 1, Gravity, f);
    }

    /*
░██████╗░██████╗░░█████╗░██╗░░░██╗███╗░░██╗██████╗░███╗░░░███╗░█████╗░██╗░░░██╗███████╗
██╔════╝░██╔══██╗██╔══██╗██║░░░██║████╗░██║██╔══██╗████╗░████║██╔══██╗██║░░░██║██╔════╝
██║░░██╗░██████╔╝██║░░██║██║░░░██║██╔██╗██║██║░░██║██╔████╔██║██║░░██║╚██╗░██╔╝█████╗░░
██║░░╚██╗██╔══██╗██║░░██║██║░░░██║██║╚████║██║░░██║██║╚██╔╝██║██║░░██║░╚████╔╝░██╔══╝░░
╚██████╔╝██║░░██║╚█████╔╝╚██████╔╝██║░╚███║██████╔╝██║░╚═╝░██║╚█████╔╝░░╚██╔╝░░███████╗
░╚═════╝░╚═╝░░╚═╝░╚════╝░░╚═════╝░╚═╝░░╚══╝╚═════╝░╚═╝░░░░░╚═╝░╚════╝░░░░╚═╝░░░╚══════╝
    */
    // Surfaces have a "level" to make them a little more sticky than being able to come off them in 1 tick.
    // This is to prevent repeated landings on surfaces.
    public bool IsOnGround
    {
        get => groundLevel > 0;
        set
        {
            if (value) groundLevel = SURFACE_MAX_LEVEL;
            else groundLevel = 0;
        }
    }

    private int groundLevel;
    public const float CAMERA_ROLL_CORRECT_SPEED = 40f;
    public const float GROUND_ACCELERATION = 6.5f;
    public const float GROUND_ANGLE = 45;
    public const float GROUND_FRICTION = 6f;
    public const float SLIDE_FRICTION = 2f;
    public const int SLIDE_FRICTION_TICKS = 5;
    public const float SLIDE_BOOST_SPEED = 18f;
    private int groundTickCount;
    private int groundTimestamp = -100000;
    private Vector3 previousPosition;
    private float previousSpeed;
    private Vector3 slideLeanVector;
    private GameObject currentGround;
    private GameObject lastGround;
    private GameObject lastRefreshGround;

    public void GroundMove(float f)
    {
        if (currentGround.CompareTag("Finish"))
        {
            Game.EndTimer();
        }

        DoubleJumpAvailable = true;
        GravityTick(f);
        lastWall = null;
        lastWallNormal = Vector3.zero;
        wallLeanAmount = 0;
        if (groundTickCount == 0) GrappleCharges += 0.3f;

        DashAvailable = true;

        lastGround = currentGround;

        // Apply slide boost at the start of a slide or when landing on the ground while sliding
        if ((crouchAmount < 0.7 && IsSliding) || (groundTickCount == 0 && IsSliding))
        {
            var speed = Mathf.Lerp(0, SLIDE_BOOST_SPEED, Mathf.Clamp01(crouchAmount * 1.5f));
            Accelerate(Flatten(velocity).normalized, speed, BASE_SPEED);
        }

        if (PlayerJump()) return;

        groundTickCount++;

        if (groundTickCount == 1 && !IsSliding) AudioManager.PlayAudio(groundLand);

        // Stop sliding if you slow down enough
        if (Flatten(velocity).magnitude < BASE_SPEED)
        {
            IsSliding = false;
        }

        if (IsSliding)
        {
            if (!AudioManager.IsPlaying(slide))
            {
                AudioManager.PlayAudio(slide, true);
            }

            var volume = Mathf.Min(groundTickCount / 10f, crouchAmount, Flatten(velocity).magnitude / 10f);
            AudioManager.SetVolume(slide, volume);
        }
        else
        {
            AudioManager.StopAudio(slide);
        }

        // Camera roll for sliding, we calculate this outside the if statement so it can handle uncrouching
        var leanProjection = Vector3.Dot(slideLeanVector, camera.transform.right);
        var roll = leanProjection * 15 * crouchAmount;
        var rollspeed = Mathf.Abs(CameraRoll - roll) / Time.fixedDeltaTime;
        SetCameraRoll(roll, rollspeed);
        if (IsSliding)
        {
            // Sliding on ground has same movement as in the air
            if (groundTickCount < SLIDE_FRICTION_TICKS) ApplyFriction(f * SLIDE_FRICTION, 0, BASE_SPEED / 2);
            AirAccelerate(ref velocity, f, 1, 0);

            slideLeanVector = Vector3.Lerp(slideLeanVector, Flatten(velocity).normalized, f * 7);
        }
        else
        {
            ApplyFriction(f * GROUND_FRICTION, 0, BASE_SPEED / 3);
            Accelerate(Wishdir, BASE_SPEED, GROUND_ACCELERATION, f);
        }
    }

    // Returns speed gain
    public float Accelerate(Vector3 wishdir, float speed, float acceleration, float f = 1)
    {
        var currentspeed = Vector3.Dot(velocity, wishdir.normalized);
        var addspeed = Mathf.Abs(speed) - currentspeed;

        if (addspeed <= 0)
            return 0f;

        var accelspeed = acceleration * f * speed;
        if (accelspeed > addspeed)
            accelspeed = addspeed;

        velocity += accelspeed * wishdir;

        return accelspeed;
    }

    /*
░█████╗░██╗██████╗░███╗░░░███╗░█████╗░██╗░░░██╗███████╗
██╔══██╗██║██╔══██╗████╗░████║██╔══██╗██║░░░██║██╔════╝
███████║██║██████╔╝██╔████╔██║██║░░██║╚██╗░██╔╝█████╗░░
██╔══██║██║██╔══██╗██║╚██╔╝██║██║░░██║░╚████╔╝░██╔══╝░░
██║░░██║██║██║░░██║██║░╚═╝░██║╚█████╔╝░░╚██╔╝░░███████╗
╚═╝░░╚═╝╚═╝╚═╝░░╚═╝╚═╝░░░░░╚═╝░╚════╝░░░░╚═╝░░░╚══════╝
    */
    public bool ApproachingGround { get; set; }
    private const float AIR_SPEED = 1.8f;
    private const float SIDE_AIR_ACCELERATION = 50;
    private const float FORWARD_AIR_ACCELERATION = 70;
    private const float DIAGONAL_AIR_ACCEL_BONUS = 80;
    private const float BACKWARD_AIR_ACCELERATION = 35;
    private int airTickCount;

    public void AirMove(ref Vector3 vel, float f, float airAccelScale = 1)
    {
        GravityTick(f);
        slideLeanVector = Vector3.zero;
        airTickCount++;

        if (!jumpKitEnabled)
        {
            AirAccelerate(ref vel, f, airAccelScale);
            return;
        }

        if (rigidbody.SweepTest(Vector3.down, out var other, 0.3f, QueryTriggerInteraction.Ignore))
        {
            if (CanCollide(other.collider))
            {
                GravityTick(f);
            }
        }

        // Lean in
        var velWithoutDash = IsDashing ? vel - vel.normalized * DASH_SPEED : vel;
        var movement = velWithoutDash + (vel.normalized * DASH_SPEED * dashTime);
        var didHit = rigidbody.SweepTest(movement.normalized, out var hit,
            movement.magnitude * WALL_LEAN_PREDICTION_TIME, QueryTriggerInteraction.Ignore);

        var eatJump = false;
        var timeToWall = 0f;

        // Enter sliding state above speed threshold
        if (Flatten(velocity).magnitude > BASE_SPEED - 2)
        {
            IsSliding = true;
        }

        if (didHit
            && Mathf.Abs(Vector3.Angle(Vector3.up, hit.normal) - 90) < WALL_VERTICAL_ANGLE_GIVE
            && CanCollide(hit.collider, false)
            && (hit.collider.gameObject != lastWall ||
                Vector3.Dot(Flatten(hit.normal).normalized, lastWallNormal) < 0.7 ||
                WALL_ALLOW_SAME_FACING ||
                PlayerInput.tickCount - wallJumpTimestamp > WALL_ALLOW_SAME_FACING_COOLDOWN_TICKS))
        {
            // This variable gives us a prediction of how long it will take until we touch the wall
            timeToWall = (hit.distance / (movement.magnitude * WALL_LEAN_PREDICTION_TIME)) * WALL_LEAN_PREDICTION_TIME;

            wallNormal = Flatten(hit.normal).normalized;

            // Slowly increase lean amount, and ease it with the same ease function as titanfall
            if (wallLeanAmount < 1)
            {
                wallLeanAmount = Mathf.Clamp01(wallLeanAmount + Time.fixedDeltaTime / WALL_LEAN_PREDICTION_TIME);

                var easeOutSine = Mathf.Sin(wallLeanAmount * Mathf.PI / 2);

                var normal = Flatten(hit.normal);
                var projection = Vector3.Dot(CrosshairDirection, new Vector3(-normal.z, normal.y, normal.x));

                var roll = WALL_LEAN_DEGREES * easeOutSine * -projection;
                var speed = Mathf.Abs(CameraRoll - roll) / Time.fixedDeltaTime;
                SetCameraRoll(roll, speed);
            }

            // First tick on approach, tell view model that lean is starting
            if (!ApproachingWall)
            {
                if (cameraRotation < 0)
                {
                    weaponManager.LeftWallStart();
                }
                else
                {
                    weaponManager.RightWallStart();
                }
            }

            ApproachingWall = true;

            // Eat jump inputs if you are < buffering away from the wall to not eat double jump
            if (timeToWall < (WALL_JUMP_BUFFERING + 1) * Time.fixedDeltaTime) eatJump = true;

            // This is to prevent landing on the very bottom of a wall
            // If youre going towards the bottom of a wall
            // your velocity will be redirected up so the bottom of your hitbox hits the bottom of the wall
            /*var fromBottom = 1;
            var upCheck = transform.position + (vel.normalized * hit.distance) + (-hit.normal * 0.55f) +
                          (Vector3.down * 1);
            if (Physics.Raycast(upCheck + Vector3.down * fromBottom, Vector3.up, out var upHit, 2, 1,
                QueryTriggerInteraction.Ignore))
            {
                var vector = (transform.position + Vector3.down) - (upHit.point + Vector3.up * fromBottom);
                var p = Mathf.Atan2(vector.y, Flatten(vector).magnitude);
                if (p * Mathf.Rad2Deg < -45) p = Mathf.Deg2Rad * -45;
                var y = Mathf.Tan(-p) * Flatten(vel).magnitude;
                vel.y = y;
            }*/
        }
        else
        {
            // Tell view model that we're not approaching a wall
            weaponManager.WallStop();
            ApproachingWall = false;

            // Lean out of a wall, applying same ease function as titanfall
            if (wallLeanAmount > 0)
            {
                wallLeanAmount = Mathf.Clamp01(wallLeanAmount - Time.fixedDeltaTime / WALL_LEAN_PREDICTION_TIME);

                var leanOutProjection = Vector3.Dot(CrosshairDirection,
                    new Vector3(-wallNormal.z, wallNormal.y, wallNormal.x));

                var easeOut = wallLeanCancelled
                    ? Mathf.Sin(wallLeanAmount * Mathf.PI / 2)
                    : -(Mathf.Cos(Mathf.PI * wallLeanAmount) - 1) / 2;

                var roll = WALL_LEAN_DEGREES * easeOut * -leanOutProjection;
                var speed = Mathf.Abs(CameraRoll - roll) / Time.fixedDeltaTime;
                SetCameraRoll(roll, speed);
            }
            else
            {
                wallLeanCancelled = true;
            }
        }

        ApproachingGround = false;
        if (didHit && Vector3.Angle(Vector3.up, hit.normal) < GROUND_ANGLE && CanCollide(hit.collider))
        {
            // Eat jump inputs if you are < jumpForgiveness away from the ground to not eat double jump
            if (hit.distance / movement.magnitude / Time.fixedDeltaTime < GROUND_JUMP_BUFFERING)
            {
                eatJump = true;
                ApproachingGround = true;
            }
        }

        // Reduce air control as you approach a wall
        // Actually super important, without it its really easy to start leaning into a wall and strafe out of it
        var mod = 1f;
        if (ApproachingWall)
        {
            mod = Mathf.Clamp01(((timeToWall - (WALL_LEAN_PREDICTION_TIME / 2)) * 2) / WALL_LEAN_PREDICTION_TIME);
            mod = 1 - Mathf.Pow(1 - mod, 3);
        }

        mod *= airAccelScale;

        if (PlayerInput.usingAnalog)
        {
            AnalogAirAccelerate(ref vel, f, mod);
        }
        else
        {
            AirAccelerate(ref vel, f, mod);
        }

        // If we're eating jump inputs, dont check for PlayerJump()
        // Also set jumpBuffered higher than needed so that when PlayerJump() does eventually run it'll use the buffer
        if (eatJump || jumpBuffered > 0)
        {
            if (PlayerInput.SincePressed(PlayerInput.Jump) == 0)
            {
                jumpBuffered = 2 * Mathf.Max(WALL_JUMP_BUFFERING, GROUND_JUMP_BUFFERING) * Time.fixedDeltaTime;
            }
        }
        else PlayerJump();
    }

    public const float ANALOG_AIR_ACCELERATION = 90;

    public void AnalogAirAccelerate(ref Vector3 vel, float f, float accelMod = 1, float sideairspeedmod = 1f)
    {
        var accel = ANALOG_AIR_ACCELERATION * accelMod;

        var speed = Flatten(vel).magnitude;
        vel += Wishdir * accel * f;
        if (speed < Flatten(vel).magnitude)
        {
            var y = vel.y;
            vel = Flatten(vel).normalized * speed;
            vel.y = y;
        }
    }

    public void AirAccelerate(ref Vector3 vel, float f, float accelMod = 1, float sideairspeedmod = 1f)
    {
        var forward = transform.forward * PlayerInput.GetAxisStrafeForward();
        var right = transform.right * PlayerInput.GetAxisStrafeRight();

        var accel = FORWARD_AIR_ACCELERATION * accelMod;

        // Different acceleration for holding backwards lets me have high accel for air movement without
        // pressing s slamming you to a full stop
        if (Vector3.Dot(Flatten(vel), forward) < 0)
        {
            accel = BACKWARD_AIR_ACCELERATION * accelMod;
        }

        // Player can turn sharper if holding forward and proper side direction
        if (PlayerInput.GetAxisStrafeRight() != 0 && PlayerInput.GetAxisStrafeForward() > 0 &&
            Vector3.Dot(right, Flatten(vel)) < 0)
        {
            accel += DIAGONAL_AIR_ACCEL_BONUS;
            var speed = Flatten(vel).magnitude;
            vel += Wishdir * accel * f;
            if (speed < Flatten(vel).magnitude)
            {
                var y = vel.y;
                vel = Flatten(vel).normalized * speed;
                vel.y = y;
            }
        }
        else
        {
            var speed = Flatten(vel).magnitude;
            vel += forward * accel * f;
            if (speed < Flatten(vel).magnitude)
            {
                var y = vel.y;
                vel = Flatten(vel).normalized * speed;
                vel.y = y;
            }
        }


        if (PlayerInput.GetAxisStrafeRight() != 0 && PlayerInput.GetAxisStrafeForward() <= 0)
        {
            var sideaccel = SIDE_AIR_ACCELERATION * accelMod;
            var airspeed = AIR_SPEED * sideairspeedmod;

            // Bonus side accel makes surfing more responsive
            // This bonus persists for a bit after leaving a surf so you can actually jump off ramps
            // (also leaves some cool high level tech potential for slant boosts)
            if (surfAccelTime > 0)
            {
                sideaccel = 50000;
            }

            // Air strafing has an offset applied to it so it always pushes you to go straight forward regardless of air speed
            var offset = vel + right * airspeed;
            var angle = Mathf.Atan2(offset.z, offset.x) - Mathf.Atan2(vel.z, vel.x);

            var offsetAngle = Mathf.Atan2(right.z, right.x) - angle;
            right = new Vector3(Mathf.Cos(offsetAngle), 0, Mathf.Sin(offsetAngle));

            // This is just source air strafing
            var rightspeed = Vector3.Dot(vel, right);
            var rightaddspeed = Mathf.Abs(airspeed) - rightspeed;
            if (rightaddspeed > 0)
            {
                if (sideaccel * f > rightaddspeed)
                    sideaccel = rightaddspeed / f;

                var addvector = sideaccel * right;
                vel += addvector * f;
            }
        }
    }

    // My best attempt at recreating lurch from titanfall, definitely not perfect but it feels similar
    public void Lurch(Vector3 direction, float strength = 0.7f)
    {
        var wishdir = Flatten(direction).normalized;

        var max = Mathf.Min(BASE_SPEED, Flatten(velocity).magnitude) * 0.8f;

        var lurchdirection = Flatten(velocity) + wishdir * max;
        var strengthdirection = Vector3.Lerp(Flatten(velocity), lurchdirection, strength);

        var resultspeed = Mathf.Min(Vector3.Dot(strengthdirection.normalized, Flatten(velocity)),
            lurchdirection.magnitude);

        var lurch = strengthdirection.normalized * resultspeed;
        velocity.x = lurch.x;
        velocity.z = lurch.z;
    }

    public const float GRAVITY = 0.5f;
    public const float TERMINAL_VELOCITY = 60;
    public float Gravity => (velocity.y - Mathf.Lerp(velocity.y, -TERMINAL_VELOCITY, GRAVITY));

    public void GravityTick(float f)
    {
        velocity.y -= Gravity * f;
    }

    public void ApplyFriction(float f, float minimumSpeed = 0, float deceleration = 0)
    {
        var speed = Flatten(velocity).magnitude;
        if (speed < deceleration)
        {
            minimumSpeed = speed - deceleration;
        }

        var newspeed = Mathf.Lerp(speed, minimumSpeed, f);

        if (newspeed > speed) return;
        if (newspeed < 0)
        {
            newspeed = 0;
        }

        var y = velocity.y;
        velocity = Flatten(velocity).normalized * newspeed;
        velocity.y = y;
    }

    /*
░░░░░██╗██╗░░░██╗███╗░░░███╗██████╗░
░░░░░██║██║░░░██║████╗░████║██╔══██╗
░░░░░██║██║░░░██║██╔████╔██║██████╔╝
██╗░░██║██║░░░██║██║╚██╔╝██║██╔═══╝░
╚█████╔╝╚██████╔╝██║░╚═╝░██║██║░░░░░
░╚════╝░░╚═════╝░╚═╝░░░░░╚═╝╚═╝░░░░░
    */
    public const float MAX_JUMP_HEIGHT = 16f;
    public const float MIN_JUMP_HEIGHT = 14f;
    public const int JUMP_STAMINA_RECOVERY_TICKS = 10;
    public const int COYOTE_TICKS = 20;
    public int GROUND_JUMP_BUFFERING => IsDashing ? 25 : 4;
    public int WALL_JUMP_BUFFERING => IsDashing ? 25 : 4;

    private float jumpBuffered;
    private int jumpTimestamp;

    public bool PlayerJump()
    {
        int sinceJump = PlayerInput.SincePressed(PlayerInput.Jump);
        if (sinceJump <= Mathf.Min(WALL_JUMP_BUFFERING, GROUND_JUMP_BUFFERING) || jumpBuffered > 0)
        {
            // Infinite buffering while teleporting
            if (teleportTime > 0)
            {
                jumpBuffered = Mathf.Min(WALL_JUMP_BUFFERING, GROUND_JUMP_BUFFERING) * Time.fixedDeltaTime;
                return false;
            }

            // Pressing jump ends rail and does not give jump height
            // Rail jump impulse is given in EndRail()
            if (PlayerInput.tickCount - railTimestamp < COYOTE_TICKS)
            {
                if (railTickCount > 10)
                {
                    EndRail();
                    PlayerInput.ConsumeBuffer(PlayerInput.Jump);
                    jumpBuffered = 0;
                }

                return true;
            }

            var wallJump = PlayerInput.tickCount - wallTimestamp < COYOTE_TICKS;
            var groundJump = PlayerInput.tickCount - groundTimestamp < COYOTE_TICKS;
            var coyoteJump = PlayerInput.tickCount - wallTimestamp != 0 && PlayerInput.tickCount - groundTimestamp != 0;
            ConsumeCoyoteTimeBuffer();

            if (!groundJump && !wallJump && !DoubleJumpAvailable) return false;

            // Jumps give more height if you stay on the ground for a brief moment
            var jumpStamina = Mathf.Clamp01(Mathf.Max(groundTickCount, wallTickCount) / JUMP_STAMINA_RECOVERY_TICKS);
            var jumpHeight = Mathf.Lerp(MIN_JUMP_HEIGHT, MAX_JUMP_HEIGHT, jumpStamina);

            AudioManager.StopAudio(slide);
            AudioManager.StopAudio(groundLand);
            if (wallJump)
            {
                if (kickFeedback != null && !coyoteJump && wallTickCount <= WALL_FRICTION_TICKS)
                {
                    var frictionTicks = Mathf.Max(0, wallTickCount + sinceJump);
                    kickFeedback.text = (sinceJump > 0 ? "-" : "+") + frictionTicks;
                    kickFeedback.color = frictionTicks == 0 ? Color.green : Color.white;
                }

                for (var i = 0; i < sinceJump; i++)
                {
                    ApplyFriction(Time.fixedDeltaTime * WALL_FRICTION, BASE_SPEED);
                }

                wallRecovery = WALL_AIR_ACCEL_RECOVERY;
                ApproachingWall = false;
                AudioManager.PlayOneShot(jump);
                IsOnWall = false;
                wallLeanCancelled = false;
                if (CancelDash())
                {
                    velocity.y *= DASH_WALL_UPCANCEL_MULTIPLY;
                }

                lastWall = currentWall;
                wallJumpTimestamp = PlayerInput.tickCount;
                lastWallNormal = wallNormal;
                var y = velocity.y;

                // If holding back when jump off the wall, jump backwards off the wall
                // This is in line with holding back to turn on around in WallMove()
                // It is here as well so that if the player jumps off the wall before actually turning around from WallMove()
                // they will still jump off the desired direction
                if (Vector3.Dot(Flatten(velocity).normalized, Flatten(Wishdir).normalized) < -0.8f)
                {
                    velocity = -Flatten(velocity).normalized * BASE_SPEED;
                    velocity.y = y;
                }

                // We calculate wall jump angle on vector normals so angle off the wall doesnt change with speed
                var velDirection = Flatten(velocity).normalized;
                var normal = wallNormal;
                var strictDirection =
                    Flatten(Flatten(velDirection - normal * Vector3.Dot(velDirection, normal)).normalized +
                            normal * WALL_JUMP_ANGLE).normalized;
                var realDirection = (Flatten(velocity) + normal * WALL_JUMP_SPEED).normalized;
                var strictAngle = Vector3.Dot(strictDirection, normal);
                var realAngle = Vector3.Dot(realDirection, normal);
                if (realAngle > strictAngle)
                {
                    velocity = Flatten(velocity).magnitude * realDirection;
                }
                else
                {
                    velocity = Flatten(velocity).magnitude * strictDirection;
                }

                velocity += Flatten(velocity).normalized * WALL_JUMP_SPEED;

                velocity.y = y;

                velocity.y = Mathf.Max(jumpHeight, velocity.y);
            }
            else
            {
                SetCameraRoll(0, CAMERA_ROLL_CORRECT_SPEED);

                if (!groundJump)
                {
                    DoubleJumpAvailable = false;
                    if (GrappleHooked)
                        velocity.y = Mathf.Max(MAX_JUMP_HEIGHT, velocity.y + GRAPPLE_DOUBLEJUMP_IMPULSE);
                    else
                        velocity.y = Mathf.Max(MAX_JUMP_HEIGHT, velocity.y);
                    StopDash();
                    AudioManager.PlayOneShot(jumpair);

                    // Apply a lurch and give a bit of speed if youre below a certain speed
                    // Good for when players make big mistakes and can use double jump to recover from very low speeds in air
                    var speed = Flatten(velocity).magnitude;
                    var strength = Mathf.Clamp01((1 - speed / BASE_SPEED) * 4);
                    Lurch(Wishdir, strength);
                    var doubleJumpSpeed = BASE_SPEED / 1.5f;
                    if (Flatten(velocity).magnitude < doubleJumpSpeed)
                        velocity += Wishdir * (doubleJumpSpeed - Flatten(velocity).magnitude);
                }
                else
                {
                    AudioManager.PlayOneShot(jump);
                    IsOnGround = false;
                    var height = jumpHeight;
                    CancelDash();

                    if (kickFeedback != null && !coyoteJump && groundTickCount <= SLIDE_FRICTION_TICKS)
                    {
                        var frictionTicks = Mathf.Max(0, groundTickCount + sinceJump);
                        kickFeedback.text = (sinceJump > 0 ? "-" : "+") + frictionTicks;
                        kickFeedback.color = frictionTicks == 0 ? Color.green : Color.white;
                    }

                    for (var i = 0; i < sinceJump; i++)
                    {
                        ApplyFriction(Time.fixedDeltaTime * SLIDE_FRICTION, 0, BASE_SPEED / 2);
                    }

                    velocity.y = Mathf.Max(height, velocity.y);
                }
            }

            jumpTimestamp = PlayerInput.tickCount;
            PlayerInput.ConsumeBuffer(PlayerInput.Jump);
            jumpBuffered = 0;
            return true;
        }

        return false;
    }

    public bool IsInCoyoteTime()
    {
        var wallJump = PlayerInput.tickCount - wallTimestamp < COYOTE_TICKS;
        var groundJump = PlayerInput.tickCount - groundTimestamp < COYOTE_TICKS;
        return wallJump || groundJump;
    }

    public void ConsumeCoyoteTimeBuffer()
    {
        railTimestamp = -10000;
        wallTimestamp = -10000;
        IsOnWall = false;
        groundTimestamp = -10000;
        IsOnGround = false;
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}