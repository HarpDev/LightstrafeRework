using System;
using UnityEngine;

public class HudMovement : MonoBehaviour
{

    public PlayerMovement player;
    
    private float _hudYawOffset;
    private float _hudPitchOffset;
    private const float hudMovementReduction = 6;
    
    public static Vector3 RotationSlamVector { get; set; }
    public static Vector3 PositionSlamVector { get; set; }
    public static Vector3 rotationSlamVectorLerp;
    private Vector3 _positionSlamVectorLerp;

    private float _prevPitch;

    private float _hudRotation;

    private void Update()
    {
        RotationSlamVector = Vector3.Lerp(RotationSlamVector, new Vector3(), Time.deltaTime * 8);
        rotationSlamVectorLerp = Vector3.Lerp(rotationSlamVectorLerp, RotationSlamVector, Time.deltaTime * 8);
        
        PositionSlamVector = Vector3.Lerp(PositionSlamVector, new Vector3(), Time.deltaTime * 8);
        _positionSlamVectorLerp = Vector3.Lerp(_positionSlamVectorLerp,  PositionSlamVector, Time.deltaTime * 8);

        // Handle HUD momentum
        var yawMovement = player.YawIncrease;
        var pitchMovement = _prevPitch - player.Pitch;
        _hudYawOffset += yawMovement / 4;
        _hudPitchOffset += pitchMovement / 4;
        _hudYawOffset = Mathf.Lerp(_hudYawOffset, 0, Time.deltaTime * hudMovementReduction);
        _hudPitchOffset = Mathf.Lerp(_hudPitchOffset, 0, Time.deltaTime * hudMovementReduction);

        _hudRotation = Mathf.Lerp(_hudRotation, player.CameraRoll, Time.deltaTime * 8);
        
        _prevPitch = player.Pitch;
    }
}
