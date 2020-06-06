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

    private bool _rightAvailable;
    private bool _leftAvailable;

    private bool _shootRight = true;

    private void Start()
    {
        _rightAvailable = true;
        _leftAvailable = true;
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

    private bool _inputThrottle = false;

    private void Update()
    {
        if (!Input.GetKey(PlayerInput.PrimaryInteract))
        {
            _inputThrottle = false;
        }
        if (Input.GetKey(PlayerInput.PrimaryInteract) && Time.timeScale > 0 && !_inputThrottle && ((_rightInfo.IsName("Idle") && _rightAvailable) || (_leftInfo.IsName("Idle") && _leftAvailable)))
        {
            _inputThrottle = true;
            var proj = Instantiate(projectile.gameObject).GetComponent<Projectile>();

            var projection = Vector3.Dot(player.velocity, player.CrosshairDirection);
            if (projection < 1) projection = 1;
            proj.Fire(player.CrosshairDirection * projection, player.camera.transform.position, player.camera.transform.position);

            if (_shootRight)
            {
                rightHand.SetBool("Fire", true);
                _rightAvailable = false;
            } else
            {
                leftHand.SetBool("Fire", true);
                _leftAvailable = false;
            }

            _shootRight = !_shootRight;
        }
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}
