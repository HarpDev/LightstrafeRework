using System;
using UnityEngine;

public class WallRunning : MonoBehaviour
{
    public PlayerControls player;

    private float lean;
    private Vector2 direction;

    public float deceleration = 10f;
    public float friction = 6f;

    public float jumpForce = 15f;

    public float verticalScale = 2f;

    private Collider wall;
    private bool touching;

    private float approach;
    private bool approaching;

    private long frameCount;

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (Mathf.Abs(hit.point.y - hit.controller.transform.position.y) > 1.8f) return;
        if (!touching)
        {
            wall = hit.collider;
            touching = true;
            player.gravityEnabled = false;
            player.movementEnabled = false;

            frameCount = Environment.TickCount;
        }
    }

    private void Update()
    {
        if (touching)
        {
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
                var drop = control * friction * Time.deltaTime * scale;
                var verticalDrop = control * friction * Time.deltaTime * verticalScale;

                var newspeed = speed - drop;
                if (newspeed < 0)
                    newspeed = 0;
                if (speed > 0)
                    newspeed /= speed;

                var newverticalspeed = speed - verticalDrop;
                if (newverticalspeed < 0)
                    newverticalspeed = 0;
                if (speed > 0)
                    newverticalspeed /= speed;

                player.velocity.x *= newspeed;
                player.velocity.y *= newverticalspeed;
                player.velocity.z *= newspeed;

                var relativePoint = player.transform.InverseTransformPoint(point);

                var value = Mathf.Atan2(relativePoint.z, relativePoint.x) * Mathf.Rad2Deg;
                value = Mathf.Abs(value);
                value -= 90;
                value /= 90;

                player.CameraRotation = Mathf.Lerp(player.CameraRotation, 15 * -value, Time.deltaTime * 15);

                var towardsWall = Flatten(point - player.transform.position).normalized;

                //player.GroundMove(1.2f);
                player.RefreshDoubleJump();

                if (Input.GetAxis("Jump") > 0)
                {
                    Debug.Log(Environment.TickCount - frameCount);
                    frameCount = 0;
                    player.Jump(new Vector3(-towardsWall.x * jumpForce, player.jumpHeight, -towardsWall.z * jumpForce));
                    touching = false;
                    player.gravityEnabled = true;
                    player.movementEnabled = true;
                }
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
                if (!approaching)
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