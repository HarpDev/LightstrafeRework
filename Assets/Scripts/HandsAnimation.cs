using System;
using System.Numerics;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class HandsAnimation : MonoBehaviour
{
    public Animator animator;
    public Transform rightHand;
    public Transform leftHand;

    public LineRenderer grappleTether;
    public Transform grappleAttachLeft;
    public Transform grappleAttachRight;

    private Player player;
    private Quaternion startRotation;

    private void Start()
    {
        player = Game.OnStartResolve<Player>();
        startRotation = transform.localRotation;
    }

    private bool wasDashing;

    private AnimatorStateInfo info;

    private bool setSpeed;
    private float totalDashTime;

    private void FixedUpdate()
    {
        info = animator.GetCurrentAnimatorStateInfo(0);
        if (info.IsName("Dash"))
        {
            var clipInfo = animator.GetCurrentAnimatorClipInfo(0);
            if (clipInfo.Length > 0)
            {
                if (setSpeed)
                {
                    totalDashTime = player.DashTime;
                    animator.speed = Mathf.Min(animator.GetCurrentAnimatorClipInfo(0)[0].clip.length / totalDashTime, 3) - 0.1f;
                    setSpeed = false;
                }
            }
        }
        else setSpeed = true;

        if ((player.ApproachingWall || player.IsOnWall) &&
            Vector3.Angle(Flatten(-player.WallNormal), Flatten(player.CrosshairDirection)) > 45)
        {
            if (player.WallRightSide)
            {
                animator.SetBool("WallRunRight", true);
                animator.SetBool("WallRunLeft", false);
                if (info.IsName("Idle")) animator.Play("WallRunRight");
            }
            else
            {
                animator.SetBool("WallRunLeft", true);
                animator.SetBool("WallRunRight", false);
                if (info.IsName("Idle")) animator.Play("WallRunLeft");
            }
        }
        else if (!player.IsDashing)
        {
            if (info.IsName("WallRunRight")) animator.Play("WallRunRightReverse");
            if (info.IsName("WallRunLeft")) animator.Play("WallRunLeftReverse");
            animator.SetBool("WallRunRight", false);
            animator.SetBool("WallRunLeft", false);
        }

        animator.SetBool("Walking", !player.IsSliding && player.IsOnGround && player.Speed > 0.1f);
        animator.SetBool("GrappleHooked", player.GrappleHooked);
    }

    private void Update()
    {
        if (player.IsDashing)
        {
            transform.localRotation = startRotation;
            if (!wasDashing)
            {
                animator.Play("Dash");
            }
        }
        else
        {
            animator.speed = 1;
            transform.localRotation = startRotation;
        }

        wasDashing = player.IsDashing;
    }

    private float grapplePositionAmount;

    private int grappleHand = -1;

    private void LateUpdate()
    {
        if (player.IsDashing || info.IsName("Dash"))
        {
            var angle = Vector3.Angle(player.camera.transform.right, player.DashTargetNormal) - 90;
            //angle *= player.DashTime / totalDashTime;
            angle *= 0.7f;
            if (totalDashTime > 0 && Mathf.Abs(angle) > 0)
            {
                rightHand.RotateAround(transform.position, player.camera.transform.forward, angle);
                leftHand.RotateAround(transform.position, player.camera.transform.forward, angle);
            }
        }

        if (player.GrappleHooked)
        {
            grapplePositionAmount += Mathf.Min(Time.deltaTime * 6f, -(grapplePositionAmount - 1));
            var positions = new Vector3[2];

            if (grappleHand == -1)
            {
                var handProject = (player.transform.position - player.GrappleAttachPosition).normalized;
                var dot = Vector3.Dot(Vector3.Cross(player.velocity, Vector3.up), handProject);
                grappleHand = dot < 0 ? 0 : 1;
            }
            else
            {
                if (grappleHand == 0)
                {
                    var pos = rightHand.transform.position;
                    pos = player.camera.transform.position + player.CrosshairDirection * 0.1f;

                    var toGrapple = player.GrappleAttachPosition - pos;
                    //toGrapple -= player.CrosshairDirection * Vector3.Dot(player.CrosshairDirection, toGrapple);
                    pos += toGrapple.normalized * 0.1f;
                    pos += -player.transform.right * 0.05f;
                    pos += Vector3.down * 0.05f;

                    rightHand.transform.position =
                        Vector3.Lerp(rightHand.transform.position + -player.transform.right + Vector3.down, pos,
                            grapplePositionAmount);

                    var target = player.GrappleAttachPosition - pos;

                    target.y *= -1;
                    rightHand.LookAt(pos + target);
                    rightHand.Rotate(Vector3.up, 180, Space.World);
                    rightHand.Rotate(Vector3.right, -110, Space.Self);
                    rightHand.Rotate(Vector3.up, 140, Space.Self);
                    positions[1] = grappleAttachRight.position;
                }
                else
                {
                    var pos = leftHand.transform.position;
                    pos = player.camera.transform.position + player.CrosshairDirection * 0.1f;

                    var toGrapple = player.GrappleAttachPosition - pos;
                    //toGrapple -= player.CrosshairDirection * Vector3.Dot(player.CrosshairDirection, toGrapple);
                    pos += toGrapple.normalized * 0.1f;
                    pos += player.transform.right * 0.05f;
                    pos += Vector3.down * 0.1f;

                    leftHand.transform.position =
                        Vector3.Lerp(leftHand.transform.position + player.transform.right + Vector3.down, pos,
                            grapplePositionAmount);

                    var target = player.GrappleAttachPosition - pos;

                    target.y *= -1;
                    leftHand.LookAt(pos + target);
                    leftHand.Rotate(Vector3.up, 180, Space.World);
                    leftHand.Rotate(Vector3.right, -110, Space.Self);
                    leftHand.Rotate(Vector3.up, 200, Space.Self);
                    positions[1] = grappleAttachLeft.position;
                }


                if (!grappleTether.enabled && grapplePositionAmount > 0.8f) grappleTether.enabled = true;
                positions[0] = player.GrappleAttachPosition;

                grappleTether.SetPositions(positions);
            }
        }
        else
        {
            grapplePositionAmount -= Mathf.Min(Time.deltaTime, grapplePositionAmount);
            if (grappleTether.enabled) grappleTether.enabled = false;
            grappleHand = -1;
        }
    }

    private Quaternion rotation;
    private Quaternion targetRotation;

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}