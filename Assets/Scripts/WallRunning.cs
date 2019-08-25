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
        if (Mathf.Abs(point.y - hit.controller.transform.position.y) > 0.5f) return;
        if (!touching && !Game.I.Player.isGrounded())
        {
            wall = hit.collider;
            touching = true;
            player.gravityEnabled = false;
            player.movementEnabled = false;

            player.velocity.y += 3;
        }
    }

    private bool wishJump;
    private bool jumpLock;

    private void FixedUpdate()
    {
        if (touching)
        {
            frameCount++;
            try
            {
                var point = wall.ClosestPoint(player.transform.position);
                if ((point - transform.position).magnitude >= 0.8f)
                {
                    touching = false;
                    player.gravityEnabled = true;
                    player.movementEnabled = true;
                }

                var scale = 1f;

                var speed = player.velocity.magnitude;
                var control = speed < deceleration ? deceleration : speed;
                var drop = control * friction * scale;

                var newspeed = speed - drop;
                if (newspeed < 0)
                    newspeed = 0;
                if (speed > 0)
                    newspeed /= speed;

                if (frameCount > noFrictionFrames)
                {
                        player.velocity.x *= newspeed;
                        player.velocity.z *= newspeed;
                }

                var verticaldrop = control * verticalFriction * scale;

                var newverticalspeed = speed - verticaldrop;
                if (newverticalspeed < 0)
                    newverticalspeed = 0;
                if (speed > 0)
                    newverticalspeed /= speed;

                player.velocity.y *= newverticalspeed;

                var towardsWall = Flatten(point - player.transform.position).normalized;

                player.velocity += player.velocity.normalized * wallSpeed;

                DoubleJump.doubleJumpSpent = false;

                var jump = new Vector3(-towardsWall.x * jumpForce, player.jumpHeight, -towardsWall.z * jumpForce);
                if (wishJump && player.Jump(jump))
                {
                    player.velocity.x += player.velocity.normalized.x * (jumpForce / 12);
                    player.velocity.z += player.velocity.normalized.z * (jumpForce / 12);
                    touching = false;
                    player.gravityEnabled = true;
                    player.movementEnabled = true;
                    var c = feedbackDisplay.color;
                    if (frameCount <= noFrictionFrames)
                    {
                        c.r = 0;
                        c.b = 0;
                        c.g = 1;
                    }
                    else if (frameCount == noFrictionFrames + 1)
                    {
                        c.r = 1;
                        c.b = 0;
                        c.g = 1;
                    }
                    else if (frameCount == noFrictionFrames + 2)
                    {
                        c.r = 1;
                        c.b = 0;
                        c.g = 0;
                    }

                    feedbackDisplay.color = c;
                    frameCount = 0;
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
        if (c.r < 1) c.r += Time.deltaTime;
        if (c.g < 1) c.g += Time.deltaTime;
        if (c.b < 1) c.b += Time.deltaTime;
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

                player.CameraRotation = Mathf.Lerp(player.CameraRotation, 15 * -value, Time.deltaTime * 15);
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

            player.CameraRotation = Mathf.Lerp(player.CameraRotation, 0, Time.deltaTime * 10);
        }

        RaycastHit hit;
        var layermask = ~(1 << 9);
        var didHit = Physics.Raycast(player.transform.position, Flatten(player.velocity), out hit,
            20, layermask);
        if (didHit)
        {
            var position = player.transform.position;
            var close = hit.collider.ClosestPoint(position);
            var projection = Vector3.Dot(Flatten(player.velocity / 3.5f), Flatten(close - position).normalized);

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