using System;
using UnityEngine;
using UnityEngine.UI;

public class WallRunning : MonoBehaviour
{
    public PlayerControls player;

    public Image feedbackDisplay;

    private float lean;
    private Vector2 direction;

    public float deceleration = 10f;
    public float friction = 0.1f;
    public float wallSpeed = 2;
    public float verticalFriction = 0.2f;

    public AudioClip wallLand;

    public float jumpForce = 10f;

    private Collider wall;
    private bool touching;

    private float approach;
    private bool approaching;

    public int noFrictionFrames = 2;

    private int frameCount;

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        var point = hit.collider.ClosestPoint(player.transform.position);
        var compare = point.y - transform.position.y;
        if (compare < -0.9f || compare > 0) return;
        if (!touching && !Game.I.Player.isGrounded())
        {
            wall = hit.collider;
            touching = true;
            player.gravityEnabled = false;
            player.movementEnabled = false;

            player.velocity.y += 3;
            player.grindSound.volume = 1;
            player.source.PlayOneShot(wallLand);
        }
    }

    private void Awake()
    {
        feedbackDisplay.gameObject.SetActive(true);
        var c = feedbackDisplay.color;
        c.a = 0;
        feedbackDisplay.color = c;
    }

    private bool wishJump;
    private bool jumpLock;

    private void FixedUpdate()
    {
        if (touching)
        {
            frameCount++;
            if (frameCount == 0) return;
            try
            {
                var point = wall.ClosestPoint(player.transform.position);
                var position = transform.position;
                var ycompare = point.y - position.y;
                var distance = (Flatten(point) - Flatten(position)).magnitude;
                
                if (ycompare < -0.9f || ycompare > 0 || distance > player.controller.radius * 2)
                {
                    touching = false;
                    player.gravityEnabled = true;
                    player.movementEnabled = true;
                    player.grindSound.volume = 0;
                    frameCount = -1;
                    return;
                }

                var scale = 1f;

                var speed = Flatten(player.velocity).magnitude;
                var control = speed < deceleration ? deceleration : speed;
                var drop = control * friction * scale;

                var newspeed = speed - drop;
                if (newspeed < 0)
                    newspeed = 0;
                if (speed > 0)
                    newspeed /= speed;

                var verticalspeed = Mathf.Abs(player.velocity.y);
                var verticalcontrol = verticalspeed < deceleration ? deceleration : verticalspeed;
                var verticaldrop = verticalcontrol * verticalFriction * scale;

                var newverticalspeed = verticalspeed - verticaldrop;
                if (newverticalspeed < 0)
                    newverticalspeed = 0;
                if (verticalspeed > 0)
                    newverticalspeed /= verticalspeed;

                player.velocity.y *= newverticalspeed;

                var towardsWall = Flatten(point - player.transform.position).normalized;

                if (wall.CompareTag("Launch Wall"))
                {
                    player.velocity.y += Mathf.Min(frameCount / 5f, 25f);
                }
                else
                {
                    var wallSpeedScale = Mathf.Pow(Flatten(player.velocity).magnitude / 15f + 0.6f, -2) + 1;
                    player.velocity += player.velocity.normalized * (wallSpeed / 10f * wallSpeedScale);
                    
                    if (frameCount > noFrictionFrames)
                    {
                        player.velocity.x *= newspeed;
                        player.velocity.z *= newspeed;
                    }
                }

                DoubleJump.doubleJumpSpent = false;

                if (wishJump && player.Jump())
                {
                    var kickScale = Mathf.Pow(Flatten(player.velocity).magnitude / 15f + 1f, -2) + 1;
                    touching = false;
                    player.gravityEnabled = true;
                    player.movementEnabled = true;
                    var c = feedbackDisplay.color;
                    
                    var directionX = player.velocity.x + -towardsWall.x * kickScale * (jumpForce / 10);
                    var directionY = player.velocity.y;
                    var directionZ = player.velocity.z + -towardsWall.z * kickScale * (jumpForce / 10);
                    var dir = new Vector3(directionX, directionY, directionZ).normalized;
                    
                    if (frameCount <= noFrictionFrames)
                    {
                        player.velocity.x += -towardsWall.x * kickScale * (jumpForce / 6);
                        player.velocity.z += -towardsWall.z * kickScale * (jumpForce / 6);
                        player.velocity = dir * player.velocity.magnitude;
                        c.a = 1;
                        c.r = 0;
                        c.b = 0;
                        c.g = 1;
                    }
                    else if (frameCount == noFrictionFrames + 1)
                    {
                        player.velocity.x += -towardsWall.x * kickScale * (jumpForce / 8);
                        player.velocity.z += -towardsWall.z * kickScale * (jumpForce / 8);
                        player.velocity = dir * player.velocity.magnitude;
                        c.a = 1;
                        c.r = 1;
                        c.b = 0;
                        c.g = 1;
                    }
                    else if (frameCount == noFrictionFrames + 2)
                    {
                        player.velocity.x += -towardsWall.x * kickScale * (jumpForce / 9);
                        player.velocity.z += -towardsWall.z * kickScale * (jumpForce / 9);
                        player.velocity = dir * player.velocity.magnitude;
                        c.a = 1;
                        c.r = 1;
                        c.b = 0;
                        c.g = 0;
                    }
                    else
                    {
                        player.velocity.x += -towardsWall.x * kickScale * (jumpForce / 14);
                        player.velocity.z += -towardsWall.z * kickScale * (jumpForce / 14);
                    }

                    feedbackDisplay.color = c;
                    frameCount = -1;
                    player.grindSound.volume = 0;
                }
            }
            catch (Exception)
            {
                touching = false;
                player.gravityEnabled = true;
                player.movementEnabled = true;
            }
        }

        wishJump = false;
    }

    private float TOLERANCE = 0.01f;

    private void Update()
    {
        var c = feedbackDisplay.color;
        if (c.a > 0) c.a -= Time.deltaTime;
        feedbackDisplay.color = c;

        if (Input.GetAxis("Jump") > 0 && !wishJump && !jumpLock)
        {
            wishJump = true;
            jumpLock = true;
        }
        else if (Math.Abs(Input.GetAxis("Jump")) < TOLERANCE) jumpLock = false;

        if (touching)
        {
            try
            {
                var point = wall.ClosestPoint(player.transform.position);

                var relativePoint = player.transform.InverseTransformPoint(point);

                var value = Mathf.Atan2(relativePoint.z, relativePoint.x) * Mathf.Rad2Deg;
                value = Mathf.Abs(value);
                value -= 90;
                value /= 90;

                player.CameraRotation = Mathf.Lerp(player.CameraRotation, 15 * -value, Time.deltaTime);
            }
            catch (Exception)
            {
                touching = false;
                player.gravityEnabled = true;
                player.movementEnabled = true;
            }
        }
        else
        {
            if (!approaching)
            {
                approach = 100000f;
                approaching = false;
            }

            player.CameraRotation = Mathf.Lerp(player.CameraRotation, 0, Time.deltaTime * 6);
        }

        RaycastHit hit;
        var layermask = ~(1 << 9);

        var pos = player.transform.position;
        var didHit = Physics.CapsuleCast(pos, pos + new Vector3(0, 1f, 0), player.controller.radius, Flatten(player.velocity).normalized, out hit, 40, layermask);
        if (didHit)
        {
            var position = player.transform.position;
            var close = hit.collider.ClosestPoint(position);
            
            var projection = Vector3.Dot(Flatten(player.velocity) / 3f, Flatten(close - position).normalized);

            var distance = Flatten(close - position).magnitude - player.controller.radius;
            if (projection >= distance && distance <= approach)
            {
                if (!approaching && !Game.I.Player.isGrounded())
                {
                    approaching = true;
                    approach = distance;
                }

                var rotation = (approach - distance) / approach;

                var relativePoint = player.transform.InverseTransformPoint(close);

                var value = Mathf.Atan2(relativePoint.z, relativePoint.x) * Mathf.Rad2Deg;
                value = Mathf.Abs(value);
                value -= 90;
                value /= 90;

                player.CameraRotation = Mathf.Lerp(player.CameraRotation, 25 * rotation * -value, Time.deltaTime * 6);
            }
        }
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}