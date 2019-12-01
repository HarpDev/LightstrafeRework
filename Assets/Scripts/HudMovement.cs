using System;
using UnityEngine;

public class HudMovement : MonoBehaviour
{

    public PlayerMovement player;
    
    private float _hudYawOffset;
    private float _hudPitchOffset;
    public float hudMovementReduction = 6;
    
    public static Vector3 RotationSlamVector { get; set; }
    public static Vector3 PositionSlamVector { get; set; }
    public static Vector3 rotationSlamVectorLerp;
    private Vector3 _positionSlamVectorLerp;

    private float _prevYaw;
    private float _prevPitch;

    private Vector3 _prevVelocity;

    private float _hudRotation;

    private void Update()
    {

        // Collision momentum
        var collideVel = player.velocity - _prevVelocity;
        
        RotationSlamVector += collideVel;
        RotationSlamVector = Vector3.Lerp(RotationSlamVector, new Vector3(), Time.deltaTime * 8);
        rotationSlamVectorLerp = Vector3.Lerp(rotationSlamVectorLerp, RotationSlamVector, Time.deltaTime * 8);
        
        PositionSlamVector += collideVel;
        PositionSlamVector = Vector3.Lerp(PositionSlamVector, new Vector3(), Time.deltaTime * 8);
        _positionSlamVectorLerp = Vector3.Lerp(_positionSlamVectorLerp,  PositionSlamVector, Time.deltaTime * 8);
        
        _prevVelocity = player.velocity;

        // Handle HUD momentum
        var yawMovement = _prevYaw - player.Yaw;
        var pitchMovement = _prevPitch - player.Pitch;
        if (yawMovement > 200) yawMovement = 0;
        _hudYawOffset += yawMovement / hudMovementReduction;
        _hudPitchOffset += pitchMovement / hudMovementReduction;
        _hudYawOffset = Mathf.Lerp(_hudYawOffset, 0, 0.05f);
        _hudPitchOffset = Mathf.Lerp(_hudPitchOffset, 0, 0.05f);
        var transform1 = transform;
        var hudTransform = transform1.localPosition;
        hudTransform = _positionSlamVectorLerp / 50;
        transform1.localPosition = hudTransform;

        _hudRotation = Mathf.Lerp(_hudRotation, player.CameraRoll, Time.deltaTime * 8);
        
        transform.localRotation = Quaternion.Euler(-_hudPitchOffset, -_hudYawOffset, _hudRotation);
        
        _prevYaw = player.Yaw;
        _prevPitch = player.Pitch;
    }
}
