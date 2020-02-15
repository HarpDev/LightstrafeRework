using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class Gun : MonoBehaviour
{

    public PlayerMovement player;

    private Vector3 position = new Vector3(0.25f, -0.25f, 0.6f);

    public Transform barrel;

    private Vector3 _angleEulers;
    private const float lerpSpeed = 50;

    private float _fireKick = 0;

    private const float crouchPositionSpeed = 10;

    private float _crouchPositionAmt;

    private float _forwardChange;
    private float _rightChange;
    private float _upChange;

    private float _prevPitch;

    private float _forwardSoften;
    private float _rightSoften;

    public Projectile projectile;

    public AudioClip fireSound;

    private void Update()
    {
        var pos = position;

        var angle = 0f;

        if (player.IsSliding && !player.approachingWall)
        {
            if (_crouchPositionAmt < 1) _crouchPositionAmt += Time.deltaTime * crouchPositionSpeed;
        }
        else if (_crouchPositionAmt > 0) _crouchPositionAmt -= Time.deltaTime * crouchPositionSpeed;

        angle -= 50 * _crouchPositionAmt;
        pos += new Vector3(-0.23f, -0.05f, 0) * _crouchPositionAmt;

        var yawMovement = player.YawIncrease;
        var pitchMovement = _prevPitch - player.Pitch;

        _upChange -= player.CollisionImpulse.y / 80;

        _rightChange -= yawMovement / 400;
        _upChange -= pitchMovement / 400;

        _forwardChange = Mathf.Lerp(_forwardChange, 0, Time.deltaTime * 8);
        _rightChange = Mathf.Lerp(_rightChange, 0, Time.deltaTime * 7);
        _upChange = Mathf.Lerp(_upChange, 0, Time.deltaTime * 7);

        _forwardSoften = Mathf.Lerp(_forwardSoften, _forwardChange, Time.deltaTime * 20);
        _rightSoften = Mathf.Lerp(_rightSoften, _rightChange, Time.deltaTime * 20);

        pos.z += _forwardSoften;
        pos.x += _rightSoften;
        pos.y += _upChange;

        pos.y += _fireKick / 60;
        pos.z -= _fireKick / 30;

        if (_fireKick > 0) _fireKick -= Time.deltaTime * 30; else _fireKick = 0;

        //if (player.IsGrounded) pos += CameraBobbing.BobbingVector;

        _angleEulers = Vector3.Lerp(_angleEulers, new Vector3(90 - angle, -90, -90), Time.deltaTime * lerpSpeed);
        transform.localRotation = Quaternion.Euler(_angleEulers);
        transform.Rotate(new Vector3(-_fireKick, 0, 0), Space.Self);

        transform.localPosition = Vector3.Lerp(transform.localPosition, pos, Time.deltaTime * lerpSpeed);

        _prevPitch = player.Pitch;

        if (Input.GetKeyDown(PlayerInput.PrimaryInteract))
        {
            var proj = Instantiate(projectile.gameObject).GetComponent<Projectile>();

            var projection = Vector3.Dot(player.velocity, player.CrosshairDirection);
            if (projection < 1) projection = 1;
            proj.Fire(player.CrosshairDirection * projection, player.camera.transform.position, barrel.position);

            player.source.PlayOneShot(fireSound);

            _fireKick = 10;

            HudMovement.RotationSlamVector -= Vector3.up * 10;
        }
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}
