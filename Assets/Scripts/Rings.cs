using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class Rings : MonoBehaviour
{

    public PlayerMovement player;

    public Animator rightHand;
    public Animator leftHand;

    public Projectile projectile;

    private AnimatorStateInfo _rightInfo;
    private AnimatorStateInfo _leftInfo;

    public SkinnedMeshRenderer rightSphere;
    public SkinnedMeshRenderer leftSphere;

    private bool _rightAvailable;
    private bool _leftAvailable;

    private bool _shootRight = true;

    public Queue<Platform> ThrowQueue { get; set; }

    public static bool Fire { get; set; }

    private void Start()
    {
        ThrowQueue = new Queue<Platform>();
        _rightAvailable = true;
        _leftAvailable = true;
        Time.timeScale = 0.1f;
    }

    private void FixedUpdate()
    {
        if (rightHand == null || leftHand == null) return;
        _rightInfo = rightHand.GetCurrentAnimatorStateInfo(0);
        _leftInfo = leftHand.GetCurrentAnimatorStateInfo(0);

        if (!_rightInfo.IsName("Idle"))
        {
            rightHand.SetBool("Fire", false);
            _rightAvailable = true;
        }

        if (!_leftInfo.IsName("Idle"))
        {
            leftHand.SetBool("Fire", false);
            _leftAvailable = true;
        }
    }

    private bool _throttle;
    private void Update()
    {
        if (Input.GetKey(PlayerInput.PrimaryInteract))
        {
            if (!_throttle)
            {
                _throttle = true;
                Fire = true;
            }
        }
        else
        {
            _throttle = false;
        }
        if (ThrowQueue.Count > 0 && Time.timeScale > 0 && ((_rightInfo.IsName("Idle") && _rightAvailable) && (_leftInfo.IsName("Idle") && _leftAvailable)))
        {
            //var proj = Instantiate(projectile.gameObject).GetComponent<Projectile>();

            //var projection = Vector3.Dot(player.velocity, player.CrosshairDirection);
            //if (projection < 1) projection = 1;
            //proj.Fire(player.CrosshairDirection * projection, player.camera.transform.position, player.camera.transform.position);

            rightSphere.enabled = true;
            leftSphere.enabled = true;

            if (_shootRight)
            {
                rightHand.SetInteger("throwNumber", UnityEngine.Random.Range(0, 2));
                rightHand.SetBool("Fire", true);
                _rightAvailable = false;
            }
            else
            {
                leftHand.SetInteger("throwNumber", UnityEngine.Random.Range(0, 2));
                leftHand.SetBool("Fire", true);
                _leftAvailable = false;
            }

            _shootRight = !_shootRight;
        }
    }

    public void Throw(float angle, Transform sphereTransform, Camera viewModel, bool flip)
    {
        var target = ThrowQueue.Dequeue();
        var screen = viewModel.WorldToViewportPoint(sphereTransform.position);

        if (flip) angle = -angle;

        var up = Game.Player.camera.transform.up;
        var right = Game.Player.camera.transform.right;

        var x = Mathf.Cos(angle * Mathf.Deg2Rad);
        var y = Mathf.Sin(angle * Mathf.Deg2Rad);

        var vector = (up * x) + (right * y);

        target.BeginLight(Game.Player.camera.ViewportToWorldPoint(screen), vector);

        rightSphere.enabled = false;
        leftSphere.enabled = false;
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}
