using System;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public Collider hitbox;
    public WeaponManager weaponManager;

    public new Camera camera;
    public Transform cameraParent;
    public new Rigidbody rigidbody;

    public Vector3 cameraPosition;
    public Vector3 velocity;

    private Vector3 quickDeathFromLocation;
    private float quickDeathFromYaw;
    private Vector3 quickDeathToLocation;
    private float quickDeathToYaw;
    private float quickDeathLerp = 1;
    private const float QUICKDEATH_SPEED = 1;

    public void SetQuickDeathPosition()
    {
        if (quickDeathLerp < 1) return;
        if (IsOnRail) return;
        if (Physics.Raycast(transform.position, Vector3.down, out var hit, 5f, ExcludePlayerMask,
                QueryTriggerInteraction.Ignore) &&
            Vector3.Angle(hit.normal, Vector3.up) < GROUND_ANGLE)
        {
            quickDeathToLocation = hit.point + Vector3.up * 0.8f;
            quickDeathToYaw = Yaw;
        }
    }

    public void DoQuickDeath()
    {
        if (quickDeathToLocation.sqrMagnitude <= 0.01f)
        {
            level.RestartLevel();
            return;
        }

        if (quickDeathLerp < 1) return;
        DetachGrapple();
        EndRail();
        quickDeathFromLocation = camera.transform.position;
        quickDeathFromYaw = Yaw;
        quickDeathLerp = 0;
    }

    public int ExcludePlayerMask => ~((1 << 10) | (1 << 2));

    private float speed;

    public float Speed
    {
        get
        {
            var flat = Flatten(velocity);
            if (Math.Abs(Mathf.Pow(speed, 2) - flat.sqrMagnitude) > 0.01f)
            {
                speed = flat.magnitude;
            }

            return speed;
        }
        set
        {
            var y = velocity.y;
            velocity = Flatten(velocity).normalized * value;
            velocity.y = y;
        }
    }

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

    public const int CHARGES = 2;
    public const float CHARGE_START = 1f;
    public const float CHARGE_TOUCH_RECHARGE = 0.5f;
    public const float CHARGE_RECHARGE_RATE = 0f;
    private bool surfaceTouched = true;
    private int rechargeCooldownTimestamp;
    private int rechargeCooldown;
    public float Charges { get; set; }

    public void ChargeRefreshCooldown(int ticks)
    {
        rechargeCooldown = ticks;
        rechargeCooldownTimestamp = PlayerInput.tickCount;
    }

    public void Recharge()
    {
        var sinceLastRecharge = PlayerInput.tickCount - rechargeCooldownTimestamp;
        if (sinceLastRecharge <= rechargeCooldown) return;
        Charges += CHARGE_TOUCH_RECHARGE;
        if (Charges > CHARGES) Charges = Charges;
    }

    public bool DashAvailable => Charges >= 1;

    public bool GrappleEnabled;
    public bool DashEnabled;
    public const float GRAPPLE_DASH_RANGE = 35;

    public bool GrappleDashCast(out RaycastHit hit)
    {
        var h = GrappleDashCast(out var ray, out var howFarBeyond, 0);
        hit = ray;
        return h;
    }

    public bool GrappleDashCast(out RaycastHit hit, out float howFarBeyond, float beyond = 10f)
    {
        var origin = camera.transform.position;
        var direction = CrosshairDirection;
        hit = new RaycastHit();
        howFarBeyond = beyond;
        if (quickDeathLerp < 1) return false;
        if (Charges < 1) return false;

        if (Physics.Raycast(origin, direction, out var rayhit, GRAPPLE_DASH_RANGE + beyond, ExcludePlayerMask,
            QueryTriggerInteraction.Collide))
        {
            hit = rayhit;
            howFarBeyond = hit.distance - GRAPPLE_DASH_RANGE;
            return hit.distance < GRAPPLE_DASH_RANGE;
        }

        if (Physics.SphereCast(origin, 2f, direction, out var spherehit, GRAPPLE_DASH_RANGE - 2f + beyond,
            ExcludePlayerMask,
            QueryTriggerInteraction.Collide))
        {
            hit = spherehit;
            howFarBeyond = hit.distance - GRAPPLE_DASH_RANGE;
            return hit.distance < GRAPPLE_DASH_RANGE;
        }

        return false;
    }

    public bool IsSliding
    {
        // Give player a little control over sliding by allowing them to hold back/neutral to stand
        get
        {
            if (uncrouchBlocked) return true;
            if (IsOnRail) return false;
            if (!PlayerInput.GetKey(PlayerInput.MoveForward) &&
                !PlayerInput.GetKey(PlayerInput.MoveBackward) &&
                !PlayerInput.GetKey(PlayerInput.MoveRight) &&
                !PlayerInput.GetKey(PlayerInput.MoveLeft) &&
                Wishdir.magnitude <= 0.05f &&
                Speed < SLIDE_BOOST_SPEED) return false;
            if (Vector3.Dot(Wishdir, Flatten(velocity).normalized) < -0.2f &&
                Speed < SLIDE_BOOST_SPEED)
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
        Game.OnAwakeBind(this);
        AudioManager = GetComponent<PlayerAudioManager>();
        LookScale = 1;

        Charges = CHARGE_START;

        Yaw += transform.rotation.eulerAngles.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        grappleTether.useWorldSpace = true;
        grappleTether.enabled = false;
    }

    private Level level;
    private KickFeedback kickFeedback;

    private void Start()
    {
        level = Game.OnStartResolve<Level>();
        kickFeedback = Game.OnStartResolve<KickFeedback>();
    }

    /*
██╗░░░██╗██████╗░██████╗░░█████╗░████████╗███████╗
██║░░░██║██╔══██╗██╔══██╗██╔══██╗╚══██╔══╝██╔════╝
██║░░░██║██████╔╝██║░░██║███████║░░░██║░░░█████╗░░
██║░░░██║██╔═══╝░██║░░██║██╔══██║░░░██║░░░██╔══╝░░
╚██████╔╝██║░░░░░██████╔╝██║░░██║░░░██║░░░███████╗
░╚═════╝░╚═╝░░░░░╚═════╝░╚═╝░░╚═╝░░░╚═╝░░░╚══════╝
    */

    private float velocityThunk;
    private float velocityThunkSmoothed;
    private float previousYVelocity;

    private void Update()
    {
        if (Cursor.visible) return;

        // Mouse aim / Controller aim
        if (Time.timeScale > 0)
        {
            YawIncrease = Input.GetAxis("Mouse X") * (GameSettings.Sensitivity / 10) * LookScale;
            YawIncrease += Input.GetAxis("Joy 1 X 2") * GameSettings.Sensitivity * LookScale;

            if (quickDeathLerp < 1)
                quickDeathToYaw += YawIncrease;
            else
                Yaw = (Yaw + YawIncrease) % 360f;

            var yawinterpolation = Mathf.Lerp(Yaw, Yaw + YawFutureInterpolation, Time.deltaTime * 10) - Yaw;
            Yaw += yawinterpolation;
            YawFutureInterpolation -= yawinterpolation;

            Pitch -= Input.GetAxis("Mouse Y") * (GameSettings.Sensitivity / 10) * LookScale;
            Pitch += Input.GetAxis("Joy 1 Y 2") * GameSettings.Sensitivity * LookScale;

            var pitchinterpolation = Mathf.Lerp(Pitch, Pitch + PitchFutureInterpolation, Time.deltaTime * 10) - Pitch;
            Pitch += pitchinterpolation;
            PitchFutureInterpolation -= pitchinterpolation;

            Pitch = Mathf.Max(Pitch, -85);
            Pitch = Mathf.Min(Pitch, 85);
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
        var cameraTargetPos = InterpolatedPosition + position;
        if (quickDeathLerp < 1)
        {
            quickDeathLerp += Time.deltaTime * QUICKDEATH_SPEED;
            var x = quickDeathLerp;
            var quickSpawnEase = x < 0.5f ? 16 * x * x * x * x * x : 1 - Mathf.Pow(-2 * x + 2, 5) / 2;
            var verticalAmt = 0f;
            if (x < 0.5f)
            {
                var upEase = 1 - Mathf.Pow(1 - (x * 2), 3);
                verticalAmt = upEase;
            }
            else
            {
                var downEase = Mathf.Pow(2 - (x * 2), 3);
                verticalAmt = downEase;
            }

            camera.transform.position = Vector3.Lerp(quickDeathFromLocation, cameraTargetPos, quickSpawnEase) +
                                        Vector3.up * verticalAmt * 50;
            Yaw = Mathf.Lerp(quickDeathFromYaw, quickDeathToYaw, quickSpawnEase);
        }
        else
        {
            camera.transform.position = InterpolatedPosition + position;
        }

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
        var targetFOV = 110;
        if (IsDashing) targetFOV += 20;
        if (teleportTime > 0)
        {
            targetFOV += 50;
        }

        var lerpSpeed = 5;
        if (IsDashing) lerpSpeed += 5;
        camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, targetFOV, Time.deltaTime * lerpSpeed);
    }

    /*
███████╗██╗██╗░░██╗███████╗██████╗░██╗░░░██╗██████╗░██████╗░░█████╗░████████╗███████╗
██╔════╝██║╚██╗██╔╝██╔════╝██╔══██╗██║░░░██║██╔══██╗██╔══██╗██╔══██╗╚══██╔══╝██╔════╝
█████╗░░██║░╚███╔╝░█████╗░░██║░░██║██║░░░██║██████╔╝██║░░██║███████║░░░██║░░░█████╗░░
██╔══╝░░██║░██╔██╗░██╔══╝░░██║░░██║██║░░░██║██╔═══╝░██║░░██║██╔══██║░░░██║░░░██╔══╝░░
██║░░░░░██║██╔╝╚██╗███████╗██████╔╝╚██████╔╝██║░░░░░██████╔╝██║░░██║░░░██║░░░███████╗
╚═╝░░░░░╚═╝╚═╝░░╚═╝╚══════╝╚═════╝░░╚═════╝░╚═╝░░░░░╚═════╝░╚═╝░░╚═╝░░░╚═╝░░░╚══════╝
    */

    private bool uncrouchBlocked;
    private int startGrounded = 5;
    public bool IsSurfing { get; set; }

    private void FixedUpdate()
    {
        // Try to start the player on ground so it doesnt play the stupid ground land sound frame 1
        // idk why but on box colliders unity likes to ignore this for the first couple ticks
        // so i just repeat it for 5 ticks and it works
        if (startGrounded > 0)
        {
            if (startGrounded == 5)
            {
                if (rigidbody.SweepTest(Vector3.down, out var hit, 5f, QueryTriggerInteraction.Ignore) &&
                    Vector3.Angle(hit.normal, Vector3.up) < GROUND_ANGLE)
                {
                    transform.position += Vector3.down * hit.distance;
                    currentGround = hit.collider.gameObject;
                }
            }

            startGrounded--;
            IsOnGround = true;
            groundTickCount = 2;
        }

        wallRecovery -= Mathf.Min(wallRecovery, Time.fixedDeltaTime);
        jumpBuffered = Mathf.Max(jumpBuffered - Time.fixedDeltaTime, 0);
        eatJumpInputs = Mathf.Max(eatJumpInputs - Time.fixedDeltaTime, 0);
        wallJumpDiagonalRecovery -= Mathf.Min(wallJumpDiagonalRecovery, Time.fixedDeltaTime);

        // Check for level restart
        if (PlayerInput.SincePressed(PlayerInput.RestartLevel) == 0)
        {
            DoQuickDeath();
        }

        if (PlayerInput.SincePressed(PlayerInput.RestartLevel) == 100 && PlayerInput.GetKey(PlayerInput.RestartLevel))
        {
            level.RestartLevel();
        }

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

        if (IsOnGround || IsOnWall)
        {
            if (!surfaceTouched)
            {
                surfaceTouched = true;
                Recharge();
            }
        }
        else surfaceTouched = false;

        if (IsOnGround)
        {
            lastWall = null;
            lastWallNormal = Vector3.zero;
        }

        if (PlayerInput.SincePressed(PlayerInput.PrimaryInteract) <= 25 && GrappleEnabled)
        {
            if (!GrappleHooked)
            {
                if (GrappleDashCast(out var hit))
                {
                    AttachGrapple(hit.point);
                    PlayerInput.ConsumeBuffer(PlayerInput.PrimaryInteract);
                }
            }
        }

        if (PlayerInput.SincePressed(PlayerInput.SecondaryInteract) <= 25 && DashEnabled)
        {
            if (DashAvailable)
            {
                PlayerInput.ConsumeBuffer(PlayerInput.SecondaryInteract);
                Dash();
            }
        }

        // Movement happens here
        var factor = Time.fixedDeltaTime;
        if (GrappleHooked)
            GrappleMove(factor);
        else if (IsOnRail)
            RailMove(factor);
        else if (IsOnWall)
            WallMove(factor);
        else if (IsOnGround)
            GroundMove(factor);
        else
            AirMove(ref velocity, factor);

        if (quickDeathLerp < 0.9f)
        {
            velocity = Vector3.zero;
            transform.position = quickDeathToLocation;
        }

        if (dashTime > 0)
        {
            if (dashTime - factor <= 0)
            {
                if (IsOnGround || IsOnWall)
                {
                    PlayerInput.SimulateKeyPress(PlayerInput.Jump);
                    PlayerJump();
                    eatJumpInputs = 0.2f;
                }
                else
                    StopDash();
            }
            else
            {
                dashTime -= factor;
            }
        }

        if (dashCancelTempSpeed > 0)
        {
            var loss = factor * DASH_CANCEL_TEMP_SPEED_DECAY;
            dashCancelTempSpeed -= Mathf.Min(loss, dashCancelTempSpeed);
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
                velocity = Speed * Flatten(tpVector).normalized;
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

        if (Charges < CHARGES) Charges += Time.deltaTime * CHARGE_RECHARGE_RATE;
        if (Charges > CHARGES) Charges = CHARGES;
        if (Charges < 0) Charges = 0;

        // Interpolation delta resets to 0 every fixed update tick, its used to measure the time since the last fixed update tick
        motionInterpolationDelta = 0;

        IsSurfing = false;

        // This variable is the total movement that will occur in this tick
        var movement = (IsDashing ? dashVector : velocity) * Time.fixedDeltaTime;
        movement += Flatten(movement).normalized * dashCancelTempSpeed * Time.fixedDeltaTime;
        previousPosition = transform.position;

        var hold = 0.4f;
        if (rigidbody.SweepTest(Vector3.down, out var groundHoldHit, hold, QueryTriggerInteraction.Ignore))
        {
            if (IsViableGround(groundHoldHit.collider, groundHoldHit.normal) && !ApproachingGround &&
                velocity.y < MIN_JUMP_HEIGHT - 2)
            {
                transform.position += Vector3.down * groundHoldHit.distance;
                IsOnGround = true;
                currentGround = groundHoldHit.collider.gameObject;
            }
        }

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

        if (surfAccelTime > 0) surfAccelTime -= Time.fixedDeltaTime;
        uncrouchBlocked = false;

        while (movement.magnitude > 0f && iterations < 5)
        {
            iterations++;

            // this is fucked lol
            // good luck
            var crouchHitboxHeight = 1.2f;
            var hitboxTransform = hitbox.transform;
            if (Math.Abs(hitbox.transform.localScale.y - crouchHitboxHeight) < 0.05f && !IsSliding)
            {
                hitboxTransform.localScale = new Vector3(1, 2, 1);
                hitboxTransform.localPosition = Vector3.up * (hitboxTransform.localScale.y - 1);
                var extents = hitbox.bounds.extents;
                var center = hitbox.bounds.center;
                extents.y /= 2f;
                center.y += 1;
                foreach (var c in Physics.OverlapBox(center, extents))
                {
                    if (CanCollide(c))
                    {
                        uncrouchBlocked = true;
                        hitboxTransform.localScale = new Vector3(1, crouchHitboxHeight, 1);
                        break;
                    }
                }
            }

            if (Math.Abs(hitbox.transform.localScale.y - 2) < 0.05f && IsSliding)
            {
                hitboxTransform.localScale = new Vector3(1, crouchHitboxHeight, 1);
            }

            hitboxTransform.localPosition = Vector3.up * (hitboxTransform.localScale.y - 1);

            var reducedscale = hitbox.transform.localScale * (1f - hitbox.contactOffset * 2);
            reducedscale.y = IsSliding ? reducedscale.y : 2f - hitbox.contactOffset * 2;
            var realscale = hitbox.transform.localScale;
            if (!IsSliding) realscale.y = 2;
            hitbox.transform.localScale = reducedscale;

            // a lot of these numbers are really particular and finnicky
            transform.position -= movement.normalized * 0.2f;
            movement += movement.normalized * 0.2f;
            var collided = false;
            if (rigidbody.SweepTest(movement.normalized, out var hit, movement.magnitude,
                QueryTriggerInteraction.Ignore) && CanCollide(hit.collider))
            {
                hitbox.transform.localScale = realscale;
                transform.position += movement.normalized * (hit.distance + 0.05f);
                if (Physics.ComputePenetration(hitbox, hitbox.transform.position, hitbox.transform.rotation,
                    hit.collider, hit.collider.gameObject.transform.position,
                    hit.collider.gameObject.transform.rotation,
                    out var direction, out var distance))
                {
                    if (Vector3.Dot(hit.normal, direction) > 0)
                    {
                        collided = true;
                        movement -= movement.normalized * hit.distance;

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
            }

            if (!collided)
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

        previousSpeed = Speed;
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
    public const float VERTICAL_COLLIDE_INEFFICIENCY = 0.6f;
    public const float WALL_COLLIDE_REFUND_MAX = 16;
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
        if (!IsOnWall && Vector3.Dot(Flatten(CrosshairDirection).normalized, Flatten(normal).normalized) < -0.5f &&
            surfAccelTime <= 0)
        {
            var feetPosition = transform.position + Vector3.down;
            var stepCheck = feetPosition - Flatten(normal).normalized * (hitbox.bounds.size.x / 2 + 0.05f) +
                            Vector3.up * STEP_HEIGHT;
            if (Physics.Raycast(stepCheck, Vector3.down, out var stepHit, STEP_HEIGHT, ExcludePlayerMask,
                    QueryTriggerInteraction.Ignore) &&
                !IsOnGround)
            {
                if (Vector3.Angle(stepHit.normal, Vector3.up) < GROUND_ANGLE)
                {
                    if (Physics.OverlapSphere(stepCheck + Vector3.up * 0.11f, 0.1f, ExcludePlayerMask,
                        QueryTriggerInteraction.Ignore).Length == 0)
                    {
                        normal = Vector3.up;
                        velocity.y = -WALL_END_BOOST_SPEED;
                        IsOnGround = true;
                        ApproachingWall = false;
                        distance = STEP_HEIGHT - stepHit.distance;
                    }
                }
            }
        }

        var dashProjection = Vector3.Dot(dashVector, -normal);
        if (dashProjection > 0)
        {
            dashVector += normal * dashProjection;
        }

        var angle = Vector3.Angle(Vector3.up, normal);

        if (IsViableGround(collider, normal))
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
                var speed = Speed;
                velocity += impulse;
                var y = velocity.y;
                velocity = Flatten(velocity).normalized * speed;
                velocity.y = y;
            }
            else
            {
                var wishdir = velocity + impulse;
                var y = CalculateYForDirection(wishdir);

                var verticalCollide = velocity + impulse * VERTICAL_COLLIDE_INEFFICIENCY;
                verticalCollide.y = y;

                var impulseCollide = velocity + impulse;

                if (Flatten(wishdir).magnitude != 0 &&
                    Flatten(verticalCollide).magnitude > Flatten(impulseCollide).magnitude &&
                    Mathf.Abs(angle - 90) >= WALL_VERTICAL_ANGLE_GIVE)
                {
                    if (!IsViableGround(collider, normal))
                    {
                        velocity += impulse * VERTICAL_COLLIDE_INEFFICIENCY;
                    }

                    velocity.y = y;
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
            IsSurfing = true;
        }

        if (IsViableWall(collider, normal) && !IsOnGround)
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

                currentWall = GetTopParent(collider.transform).gameObject;

                if (Speed < WALL_COLLIDE_REFUND_MAX && wallTickCount == 0)
                {
                    var fromRefundMax = WALL_COLLIDE_REFUND_MAX - Speed;
                    var mod = Mathf.Abs(Vector3.Dot(Flatten(CrosshairDirection).normalized,
                        Flatten(velocity).normalized));
                    if (Vector3.Dot(CrosshairDirection, Flatten(velocity)) < 0) mod = 0;
                    mod = Mathf.Pow(mod, 3);
                    velocity += Flatten(velocity).normalized * Mathf.Min(velocityProjection, fromRefundMax) * mod;
                }
            }
        }
    }

    public bool IsViableGround(Collider collider, Vector3 normal)
    {
        var angle = Vector3.Angle(Vector3.up, normal);

        return angle < GROUND_ANGLE && !collider.CompareTag("Uninteractable") && !collider.CompareTag("Wall");
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
            level.RestartLevel();
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
    private Vector3 teleportToPosition;
    private Vector3 teleportStartPosition;
    private bool tpThisTick;

    public void Teleport(Vector3 position)
    {
        teleportToPosition = position;
        teleportTime = 0.2f;
        //teleportTotalTime = 0.2f;
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

    public const float DASH_SPEED = 70;
    public const float DASH_CANCEL_TEMP_SPEED = 5;
    public const float DASH_SPEEDGAIN = 10;
    public const float DASH_SPEEDGAIN_CAP = 35;
    public const float DASH_CANCEL_TEMP_SPEED_DECAY = 1.5f;
    public const float DASH_UPVELOCITY_LIMIT = 16;
    public const float DASH_MAX_REFUND = 5;
    public const int DASH_TICKS_BEYOND_SURFACE = 8;
    private float dashTime;
    private float dashCancelTempSpeed;
    private int dashEndTimestamp;
    private Vector3 velocityBeforeDash;
    private Vector3 dashVector;

    public void Dash()
    {
        var wishdir = CrosshairDirection;
        if (!DashAvailable) return;
        if (IsDashing) return;
        if (GrappleDashCast(out var hit))
        {
            AudioManager.PlayOneShot(dash);
            StopDash();
            wishdir = (hit.point - transform.position).normalized;

            if (velocity.magnitude < SLIDE_BOOST_SPEED)
                velocity = wishdir.normalized * SLIDE_BOOST_SPEED;
            velocityBeforeDash = velocity;

            var y = CalculateYForDirection(wishdir);

            var onlyYChange = Speed * Flatten(wishdir).normalized;
            onlyYChange.y = y;

            velocity = Mathf.Min(velocity.magnitude, onlyYChange.magnitude) * wishdir.normalized;

            Charges--;

            dashVector = wishdir.normalized * DASH_SPEED;
            dashTime = hit.distance / dashVector.magnitude;
            dashTime += Time.fixedDeltaTime * DASH_TICKS_BEYOND_SURFACE;
        }
    }

    public bool StopDash()
    {
        if (IsDashing)
        {
            if (ApproachingGround || ApproachingWall) return false;
            dashTime = 0;
            dashEndTimestamp = PlayerInput.tickCount;

            var wishdir = dashVector.normalized;

            var y1 = CalculateYForDirection(wishdir);

            var onlyYChange = Flatten(velocityBeforeDash).magnitude * Flatten(wishdir).normalized;
            onlyYChange.y = y1;

            velocity = Mathf.Min(velocityBeforeDash.magnitude, onlyYChange.magnitude) * wishdir.normalized;

            var y = velocity.y;
            if (dashVector.y > 0 && Flatten(velocity).sqrMagnitude < Flatten(velocityBeforeDash).sqrMagnitude)
            {
                var refundAmt = Flatten(velocityBeforeDash).magnitude - Speed;
                if (refundAmt > DASH_MAX_REFUND) refundAmt = DASH_MAX_REFUND;
                velocity += Flatten(velocity).normalized * refundAmt;
            }

            velocity.y = Mathf.Min(DASH_UPVELOCITY_LIMIT, y);

            var rawgain = DASH_SPEEDGAIN *
                          Mathf.Clamp01((DASH_SPEEDGAIN_CAP - Speed) / DASH_SPEEDGAIN_CAP);
            velocity += Flatten(velocity).normalized * rawgain;
            return true;
        }

        return false;
    }

    public bool CancelDash(bool ground)
    {
        if (StopDash())
        {
            dashEndTimestamp = 0;
            AudioManager.PlayOneShot(dashCancel);
            var tempSpeed = DASH_CANCEL_TEMP_SPEED;

            dashCancelTempSpeed = tempSpeed;
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
    public const float RAIL_SLANT_EFFICIENCY = 0.1f;
    public const float RAIL_VERTICAL_VELCITYLIMIT = 30f;
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

        var speedBeforeAnything = Speed;
        if (closeIndex + railDirection >= 0 && closeIndex + railDirection < currentRail.smoothedPoints.Length)
        {
            var y = velocity.y;
            velocity = Speed * Flatten(railVector).normalized;
            velocity.y = y;
            var projection = Vector3.Dot(velocity, railVector.normalized);
            var slantVec = railVector.normalized * projection;
            if (Flatten(slantVec).magnitude > speedBeforeAnything)
            {
                var bonus = Flatten(slantVec).magnitude - speedBeforeAnything;
                speedBeforeAnything += bonus * RAIL_SLANT_EFFICIENCY;
            }
        }

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

        var desiredDirection = velocity.normalized;
        velocity = Flatten(velocity).normalized * speedBeforeAnything;
        velocity.y = CalculateYForDirection(desiredDirection);

        Accelerate(velocity.normalized, RAIL_SPEED, RAIL_ACCELERATION, f);

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

        rail.ChangeHitboxLayer(2);

        Recharge();

        if (GrappleHooked) DetachGrapple();
    }

    public void EndRail()
    {
        if (!IsOnRail) return;

        if (Mathf.Abs(velocity.y) > RAIL_VERTICAL_VELCITYLIMIT)
        {
            velocity.y = Mathf.Sign(velocity.y) * RAIL_VERTICAL_VELCITYLIMIT;
        }

        // Jump impulse is applied on rail end instead of PlayerJump() so that you get it even if you just ride off the end of the rail
        // Also only apply if moving mostly upwards, sometimes we want rails to throw the player down
        if (velocity.y > -4) velocity.y += MIN_JUMP_HEIGHT;

        currentRail.ChangeHitboxLayer(0);
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
    public const float GRAPPLE_CROSSHAIR_CONTROL_ACCELERATION = 30f;
    public const float GRAPPLE_WISHDIR_CONTROL_ACCELERATION = 3f;
    public const float GRAPPLE_REFUND_ACCELERATION = 2f;
    public const float GRAPPLE_MIN_SPEED = 20;
    public const float GFORCE_SPEEDGAIN_DOWN_FRICTION = 1.9f;
    public const float GFORCE_SPEEDGAIN_UP_FRICTION = 4.5f;
    public const float GRAPPLE_UPWARD_PULL = 10;
    public const float GRAPPLE_INWARD_PULL = 20;
    public const float GRAPPLE_DOUBLEJUMP_IMPULSE = 4;
    public const float GRAPPLE_SPEEDGAIN = 8;
    public const float GRAPPLE_SPEEDGAIN_CAP = 45;
    private int grappleTicks;
    private Vector3 grappleAttachPosition;
    private float speedOnAttach;


    public void GrappleMove(float f)
    {
        var position = grappleAttachPosition;

        var towardPoint = (position - transform.position).normalized;

        var velocityProjection = Vector3.Dot(velocity, towardPoint);
        var tangentVector = (velocity + towardPoint * -velocityProjection).normalized;

        var swingProjection = Mathf.Max(0, Vector3.Dot(velocity, Vector3.Lerp(towardPoint, tangentVector, 0.5f)));

        if (velocity.magnitude < 0.05f) velocity += towardPoint * f * GRAPPLE_CROSSHAIR_CONTROL_ACCELERATION;

        var speed = velocity.magnitude;
        var controlDirection = (CrosshairDirection - (Vector3.Dot(CrosshairDirection, towardPoint) * towardPoint))
            .normalized;
        velocity += controlDirection * GRAPPLE_CROSSHAIR_CONTROL_ACCELERATION * f;
        velocity = velocity.normalized * speed;
        velocity += towardPoint.normalized * GRAPPLE_CROSSHAIR_CONTROL_ACCELERATION * f * 2;
        velocity = velocity.normalized * speed;
        if (velocity.y < 0)
        {
            //GravityTick(f * -velocity.normalized.y);
        }

        Accelerate(Wishdir, SLIDE_BOOST_SPEED, GRAPPLE_WISHDIR_CONTROL_ACCELERATION, f);

        if (velocityProjection < 0)
        {
            var wishdir = velocity + towardPoint.normalized * -velocityProjection;
            if (wishdir.magnitude > 0.5f && Mathf.Abs(wishdir.normalized.y) < 0.35f)
            {
                var y = CalculateYForDirection(wishdir);

                velocity.y = y;
            }
            else
            {
                var beforeSwing = velocity;
                velocity += towardPoint.normalized * -velocityProjection;
                var verticalG = Mathf.Abs((towardPoint.normalized * -velocityProjection).y);
                if (Speed > Flatten(beforeSwing).magnitude)
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
            velocity += Vector3.up * GRAPPLE_UPWARD_PULL * f * velocity.normalized.y;
        }

        var towardPointAngle = Vector3.Angle(towardPoint, Vector3.up);
        var velocityAngle = Vector3.Angle(velocity, Vector3.up);
        var upCross = Vector3.Cross(towardPoint, Vector3.Cross(towardPoint, Vector3.down));
        if (Vector3.Dot(CrosshairDirection, upCross) > 0.1f && velocityAngle > towardPointAngle)
        {
            var flatProjection = Vector3.Dot(velocity, Flatten(towardPoint).normalized);
            if (flatProjection > 0)
            {
                var efficiency = 0.4f;
                velocity -= Flatten(towardPoint).normalized * flatProjection * efficiency;
                velocity += Vector3.up * flatProjection * efficiency;
            }
        }

        var rawgain = GRAPPLE_SPEEDGAIN *
                      Mathf.Clamp01((GRAPPLE_SPEEDGAIN_CAP - Speed) / GRAPPLE_SPEEDGAIN_CAP);
        var minSpeed = Mathf.Max(speedOnAttach, GRAPPLE_MIN_SPEED) + rawgain;
        if (Speed < minSpeed) Accelerate(CrosshairDirection, minSpeed, GRAPPLE_REFUND_ACCELERATION, f);

        var gain = 0.3f;
        var absolute = (gain / f) / 6f;
        var projection = Mathf.Sqrt(swingProjection) * Vector3.Dot(-transform.right, towardPoint) * absolute;
        SetCameraRoll(projection, CAMERA_ROLL_CORRECT_SPEED);

        if (Vector3.Dot(towardPoint, CrosshairDirection) < (towardPoint.y > 0 ? -0.2f : 0f)) DetachGrapple();

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
        speedOnAttach = Speed;
        Charges--;
    }

    public void DetachGrapple()
    {
        if (GrappleHooked)
        {
            GrappleHooked = false;
            if (grappleTether.enabled) grappleTether.enabled = false;
            SetCameraRoll(0, CAMERA_ROLL_CORRECT_SPEED);

            var s = speedOnAttach - Speed;
            if (s > 0)
            {
                velocity += Flatten(velocity).normalized * Mathf.Min(s, 15);
            }

            AudioManager.PlayOneShot(grappleRelease);
        }
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
    public const float WALL_JUMP_ANGLE = 14f;
    public const float WALL_VERTICAL_ANGLE_GIVE = 10f;
    public const float WALL_AIR_ACCEL_RECOVERY = 0.35f;
    public const float WALL_END_BOOST_SPEED = 2;
    public const float WALL_LEAN_DEGREES = 15f;
    public const float WALL_SPEED = 10;
    public const float WALL_ACCELERATION = 0.5f;
    public const float WALL_TURNAROUND_ACCELERATION = 1.2f;
    public const float WALL_LEAN_PREDICTION_TIME = 0.25f;
    public const float WALL_JUMP_SPEED = 8;
    public const int WALL_FRICTION_TICKS = 8;
    public const float WALL_FRICTION = 6f;
    public const float WALLRUN_TIME = 1.2f;
    public const float WALLRUN_FALLOFF_START = 0.4f;
    private Vector3 wallNormal;
    private Vector3 lastWallNormal;
    private bool wallLeanCancelled;
    private int wallTimestamp = -100000;
    private int wallTickCount;
    private int wallJumpTimestamp;
    private GameObject lastWall;
    private GameObject currentWall;
    private float wallRecovery;
    private float wallRunTime;
    private float wallLeanAmount;
    private float wallLeanLerp;
    private int wallFrictionTicks;

    public void WallMove(float f)
    {
        DoubleJumpAvailable = true;
        lastGround = null;

        if (wallTickCount == 0)
        {
            AudioManager.PlayAudio(wallRun, true);
            wallFrictionTicks = 0;
            wallRunTime = WALLRUN_TIME;
        }

        wallRunTime -= f;

        // Fade in wall run sound so if you jump off right away its silent
        AudioManager.SetVolume(wallRun, Mathf.Clamp01(wallTickCount / 10f));
        wallTickCount++;

        // Apply friction on walls only for a few ticks at the start of the wall
        if (wallTickCount is > 0 and <= WALL_FRICTION_TICKS)
        {
            ApplyFriction(f * WALL_FRICTION, BASE_SPEED);
            wallFrictionTicks++;
        }

        // Apply camera roll from the wall
        var normal = Flatten(wallNormal);
        var projection = Vector3.Dot(CrosshairDirection, new Vector3(-normal.z, normal.y, normal.x));

        var endModifier = Mathf.Clamp01(wallRunTime / WALLRUN_FALLOFF_START);
        if (endModifier >= 1)
        {
            wallLeanAmount = Mathf.Clamp01(wallLeanAmount + Time.fixedDeltaTime * 10f);
        }
        else
        {
            var ease = Mathf.Pow(endModifier, 3);
            wallLeanAmount = ease;
        }

        var roll = WALL_LEAN_DEGREES * -projection * wallLeanAmount;
        wallLeanLerp = wallTickCount <= 1 ? roll : Mathf.Lerp(wallLeanLerp, roll, f * 20);
        var speed = Mathf.Abs(CameraRoll - wallLeanLerp) / Time.fixedDeltaTime;
        SetCameraRoll(wallLeanLerp, speed);
        ApproachingWall = false;

        if (Vector3.Dot(Flatten(velocity).normalized, Flatten(Wishdir).normalized) > 0.6f)
        {
            var alongView = (Wishdir - (Flatten(wallNormal).normalized *
                                        Vector3.Dot(Wishdir, Flatten(wallNormal).normalized)))
                .normalized;
            Accelerate(alongView, WALL_SPEED, WALL_ACCELERATION * 4, f);
            Accelerate(alongView, BASE_SPEED, WALL_TURNAROUND_ACCELERATION * 4, f);
        }

        // If you hold back on a wall, apply friction and accelerate in the opposite direction so you can turn around on walls
        if (Vector3.Dot(Flatten(velocity).normalized, Flatten(Wishdir).normalized) < -0.6f)
        {
            var alongView = (Wishdir - (Flatten(wallNormal).normalized *
                                        Vector3.Dot(Wishdir, Flatten(wallNormal).normalized)))
                .normalized;
            Accelerate(alongView, BASE_SPEED, WALL_TURNAROUND_ACCELERATION * 4, f);
            ApplyFriction(f * WALL_FRICTION);
            wallFrictionTicks++;
        }
        else
        {
            // Apply wall speed in direction youre already going
            if (Speed > 0)
            {
                Accelerate(Flatten(velocity).normalized, SLIDE_BOOST_SPEED, WALL_ACCELERATION, f);
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
        if (velocity.y < 5)
        {
            velocity.y = 0;
        }
        else
        {
            GravityTick(f);
        }

        // Push you into the wall, holding you against it a bit
        Accelerate(-wallNormal, 1, Gravity, f);
        if (IsDashing) Accelerate(ref dashVector, -wallNormal, 1, Gravity, f);
        if (wallRunTime <= 0)
        {
            Accelerate(wallNormal, WALL_END_BOOST_SPEED, WALL_END_BOOST_SPEED);
            IsOnWall = false;
            AudioManager.StopAudio(wallRun);
            lastWall = currentWall;
            lastWallNormal = wallNormal;
            wallLeanCancelled = false;
            ApproachingWall = false;
            wallRecovery = WALL_AIR_ACCEL_RECOVERY;
        }

        PlayerJump();
    }

    public bool IsViableWall(Collider wall, Vector3 normal)
    {
        return !wall.CompareTag("Uninteractable")
               && Mathf.Abs(Vector3.Angle(Vector3.up, normal) - 90) < WALL_VERTICAL_ANGLE_GIVE
               && CanCollide(wall, false)
               && (GetTopParent(wall.transform).gameObject != lastWall ||
                   Vector3.Dot(Flatten(normal).normalized, lastWallNormal) < 0.9f);
    }

    public Transform GetTopParent(Transform child)
    {
        var top = child.transform;
        while (top.parent != null)
        {
            var parent = top.parent;
            if (parent == null) break;
            top = parent;
        }

        return top;
    }

    public bool IsViableWall(RaycastHit hit)
    {
        return IsViableWall(hit.collider, hit.normal);
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
            if (value && (jumpTimestamp == 0 || PlayerInput.tickCount - jumpTimestamp > 10))
                groundLevel = SURFACE_MAX_LEVEL;
            else groundLevel = 0;
        }
    }

    private int groundLevel;
    public const float CAMERA_ROLL_CORRECT_SPEED = 40f;
    public const float GROUND_ACCELERATION = 11f;
    public const float GROUND_ANGLE = 45;
    public const float GROUND_FRICTION = 6f;
    public const float SLIDE_FRICTION = 10f;
    public const float LANDING_FRICTION_TAX = 4f;
    public const int SLIDE_FRICTION_TICKS = 20;
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
        DoubleJumpAvailable = true;
        GravityTick(f);
        wallLeanAmount = 0;
        ApproachingGround = false;

        lastGround = currentGround;

        // Apply slide boost at the start of a slide or when landing on the ground while sliding
        if ((crouchAmount < 0.7 && IsSliding) || (groundTickCount == 0 && IsSliding))
        {
            var speed = Mathf.Lerp(0, SLIDE_BOOST_SPEED, Mathf.Clamp01(crouchAmount * 1.5f));
            Accelerate(Flatten(velocity).normalized, speed, BASE_SPEED);
        }

        if (IsDashing)
        {
            groundTickCount = -1;
            SetQuickDeathPosition();
        }

        groundTickCount++;

        if (groundTickCount == 1)
        {
            if (!IsSliding && quickDeathLerp >= 1) AudioManager.PlayAudio(groundLand);
            ApplyFriction(f * LANDING_FRICTION_TAX, 0, BASE_SPEED / 2);
            SetQuickDeathPosition();
        }

        // Stop sliding if you slow down enough
        if (Speed < BASE_SPEED)
        {
            IsSliding = false;
        }

        if (IsSliding)
        {
            if (!AudioManager.IsPlaying(slide))
            {
                AudioManager.PlayAudio(slide, true);
            }

            var volume = Mathf.Min(groundTickCount / 10f, crouchAmount, Speed / 10f);
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
        if (!IsDashing)
        {
            if (IsSliding)
            {
                // Sliding on ground has same movement as in the air
                if (groundTickCount is > GROUND_JUMP_BUFFERING and <= SLIDE_FRICTION_TICKS + GROUND_JUMP_BUFFERING)
                {
                    var slideFriction = f * SLIDE_FRICTION;
                    if (Speed > slideFriction)
                    {
                        velocity -= Flatten(velocity).normalized * slideFriction;
                    }
                }

                var speedBeforeAir = Speed;
                AirAccelerate(ref velocity, f);
                if (Speed > speedBeforeAir)
                {
                    Speed = speedBeforeAir;
                }

                slideLeanVector = Vector3.Lerp(slideLeanVector, Flatten(velocity).normalized, f * 7);
            }
            else
            {
                ApplyFriction(f * GROUND_FRICTION, 0, BASE_SPEED / 3);
                var speed = Speed;
                Accelerate(Wishdir, BASE_SPEED, GROUND_ACCELERATION, f);
                if (Speed > BASE_SPEED)
                {
                    var y = velocity.y;
                    velocity = Flatten(velocity).normalized * Mathf.Max(speed, BASE_SPEED);
                    velocity.y = y;
                }
            }
        }

        PlayerJump();
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

    public float Accelerate(ref Vector3 vel, Vector3 wishdir, float speed, float acceleration, float f = 1)
    {
        var currentspeed = Vector3.Dot(vel, wishdir.normalized);
        var addspeed = Mathf.Abs(speed) - currentspeed;

        if (addspeed <= 0)
            return 0f;

        var accelspeed = acceleration * f * speed;
        if (accelspeed > addspeed)
            accelspeed = addspeed;

        vel += accelspeed * wishdir;

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
    private const float AIR_SPEED = 2.4f;
    private const float SIDE_AIR_ACCELERATION = 50;
    private const float FORWARD_AIR_ACCELERATION = 70;
    private const float DIAGONAL_AIR_ACCEL_BONUS = 80;
    private const float BACKWARD_AIR_ACCELERATION = 35;
    private int airTickCount;
    private float timeToWall;
    private const float WALLJUMP_DIAGONAL_RECOVERY = 0.25f;
    private float wallJumpDiagonalRecovery;

    public void AirMove(ref Vector3 vel, float f)
    {
        if (!IsDashing) GravityTick(f);
        slideLeanVector = Vector3.zero;
        airTickCount++;

        if (!jumpKitEnabled)
        {
            AirAccelerate(ref vel, f);
            return;
        }

        // Lean in
        var movement = IsDashing ? dashVector : velocity;
        var didHit = rigidbody.SweepTest(movement.normalized, out var hit,
            movement.magnitude * WALL_LEAN_PREDICTION_TIME, QueryTriggerInteraction.Ignore);

        var eatJump = false;

        // Enter sliding state above speed threshold
        if (Speed > BASE_SPEED - 2)
        {
            IsSliding = true;
        }

        if (didHit && IsViableWall(hit))
        {
            // This variable gives us a prediction of how long it will take until we touch the wall
            timeToWall = (hit.distance / (movement.magnitude * WALL_LEAN_PREDICTION_TIME)) *
                         WALL_LEAN_PREDICTION_TIME;

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
            /*if (!ApproachingWall)
            {
                if (cameraRotation < 0)
                {
                    weaponManager.LeftWallStart();
                }
                else
                {
                    weaponManager.RightWallStart();
                }
            }*/

            ApproachingWall = true;

            // Eat jump inputs if you are < buffering away from the wall to not eat double jump
            if (timeToWall < (WALL_JUMP_BUFFERING + 1) * Time.fixedDeltaTime) eatJump = true;
        }
        else
        {
            // Tell view model that we're not approaching a wall
            //weaponManager.WallStop();
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
        var mod = AirAcclerationMod;
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
                jumpBuffered = Mathf.Max(WALL_JUMP_BUFFERING, GROUND_JUMP_BUFFERING) + Time.fixedDeltaTime * 2;
            }
        }
        else PlayerJump();
    }

    public float AirAcclerationMod
    {
        get
        {
            var mod = 1f;
            if (ApproachingWall)
            {
                mod = Mathf.Clamp01(((timeToWall - (WALL_LEAN_PREDICTION_TIME / 2)) * 2) / WALL_LEAN_PREDICTION_TIME);
                mod = 1 - Mathf.Pow(1 - mod, 3);
            }

            return mod;
        }
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
        if (Speed < BASE_SPEED / 2)
        {
            Accelerate(Wishdir, BASE_SPEED, GROUND_ACCELERATION / 3, f);
            return;
        }

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
            Vector3.Dot(right, Flatten(vel)) < 0 && wallJumpDiagonalRecovery <= 0)
        {
            if (!IsSurfing)
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


        if (PlayerInput.GetAxisStrafeRight() != 0 && (PlayerInput.GetAxisStrafeForward() <= 0 || IsSurfing))
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

        var max = Mathf.Min(BASE_SPEED, Speed) * 0.8f;

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
        if (IsDashing)
        {
            dashVector.y -= Gravity * f;
        }
    }

    public void ApplyFriction(float f, float minimumSpeed = 0, float deceleration = 0)
    {
        var speed = Speed;
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
    public const float MAX_JUMP_HEIGHT = 18f;
    public const float MIN_JUMP_HEIGHT = 14f;
    public const int JUMP_STAMINA_RECOVERY_TICKS = 10;
    public const int COYOTE_TICKS = 20;
    public const int GROUND_JUMP_BUFFERING = 4;
    public const int WALL_JUMP_BUFFERING = 7;
    public const int GROUNDJUMP_RECHARGE_COOLDOWN = 40;

    private float jumpBuffered;
    private float eatJumpInputs;
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

            if (eatJumpInputs > 0)
            {
                PlayerInput.ConsumeBuffer(PlayerInput.Jump);
                return true;
            }

            // Pressing jump ends rail and does not give jump height
            // Rail jump impulse is given in EndRail()
            if (PlayerInput.tickCount - railTimestamp < COYOTE_TICKS)
            {
                EndRail();
                PlayerInput.ConsumeBuffer(PlayerInput.Jump);
                jumpBuffered = 0;

                return true;
            }

            var wallJump = PlayerInput.tickCount - wallTimestamp < COYOTE_TICKS;
            var groundJump = PlayerInput.tickCount - groundTimestamp < COYOTE_TICKS;
            var coyoteJump = PlayerInput.tickCount - wallTimestamp != 0 && PlayerInput.tickCount - groundTimestamp != 0;

            if (!groundJump && !wallJump && !DoubleJumpAvailable) return false;
            if (!groundJump && !wallJump && IsDashing)
            {
                jumpBuffered = dashTime + Time.fixedDeltaTime * 2;
                return false;
            }

            ConsumeCoyoteTimeBuffer();

            // Jumps give more height if you stay on the ground for a brief moment
            var jumpStamina = Mathf.Clamp01(Mathf.Max(groundTickCount, wallTickCount) / JUMP_STAMINA_RECOVERY_TICKS);
            var jumpHeight = Mathf.Lerp(MIN_JUMP_HEIGHT, MAX_JUMP_HEIGHT, jumpStamina);

            AudioManager.StopAudio(slide);
            AudioManager.StopAudio(groundLand);
            if (wallJump)
            {
                AudioManager.PlayOneShot(jump);
                IsOnWall = false;

                var negativeFrictionTicks = Mathf.Min(sinceJump, WALL_JUMP_BUFFERING);
                var speedGain = WALL_JUMP_SPEED;

                if (CancelDash(false))
                {
                    wallFrictionTicks = 0;
                    for (var i = 0; i < Mathf.Min(wallTickCount, WALL_FRICTION_TICKS); i++)
                    {
                        ApplyFriction(Time.fixedDeltaTime * WALL_FRICTION, BASE_SPEED);
                        wallFrictionTicks++;
                    }
                }

                for (var i = 0; i < negativeFrictionTicks; i++)
                {
                    ApplyFriction(Time.fixedDeltaTime * WALL_FRICTION, BASE_SPEED);
                    wallFrictionTicks++;
                }

                if (negativeFrictionTicks > 0) wallFrictionTicks *= -1;

                kickFeedback.Display(wallFrictionTicks, wallFrictionTicks == 1 ? Color.green : Color.white);

                wallJumpDiagonalRecovery = WALLJUMP_DIAGONAL_RECOVERY;
                lastWall = currentWall;
                lastWallNormal = wallNormal;
                wallLeanCancelled = false;
                ApproachingWall = false;
                wallRecovery = WALL_AIR_ACCEL_RECOVERY;
                wallJumpTimestamp = PlayerInput.tickCount;
                var y = velocity.y;

                var speed = Speed;
                velocity += wallNormal * WALL_JUMP_ANGLE * (Mathf.Clamp01(speed / 50) + 0.2f);
                velocity = speed * Flatten(velocity).normalized;

                velocity += Flatten(velocity).normalized * speedGain;
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
                    AudioManager.PlayOneShot(jumpair);

                    // Apply a lurch and give a bit of speed if youre below a certain speed
                    // Good for when players make big mistakes and can use double jump to recover from very low speeds in air
                    var speed = Speed;
                    var strength = Mathf.Clamp01((1 - speed / BASE_SPEED) * 4);
                    Lurch(Wishdir, strength);
                    var doubleJumpSpeed = BASE_SPEED / 1.5f;
                    if (Speed < doubleJumpSpeed)
                        velocity += Wishdir * (doubleJumpSpeed - Speed);
                }
                else
                {
                    AudioManager.PlayOneShot(jump);
                    IsOnGround = false;
                    var height = jumpHeight;
                    if (velocity.y > 0 && !coyoteJump) height += velocity.y;
                    CancelDash(true);

                    velocity.y = Mathf.Max(height, velocity.y);
                    ChargeRefreshCooldown(GROUNDJUMP_RECHARGE_COOLDOWN);
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

    public float CalculateYForDirection(Vector3 direction, float max = 100f)
    {
        var wishdir = direction.normalized;
        var x2 = Flatten(wishdir).magnitude;
        var x1 = Speed;
        var y2 = wishdir.y;
        var y1 = x1 * y2 / x2;

        if (Mathf.Abs(y1) > max) y1 = Mathf.Sign(y1) * max;
        return y1;
    }
}