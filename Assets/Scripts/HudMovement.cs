using System;
using UnityEngine;

public class HudMovement : MonoBehaviour
{

    public PlayerMovement player;
    
    private float hudYawOffset;
    private float hudPitchOffset;
    public float hudMovementReduction = 6;


    public static Vector3 RotationSlamVector { get; set; }
    public static Vector3 PositionSlamVector { get; set; }
    public static Vector3 rotationSlamVectorLerp;
    private Vector3 positionSlamVectorLerp;

    private float prevYaw;
    private float prevPitch;

    private Vector3 prevVelocity;

    private void Update()
    {

        // Collision momentum
        var collideVel = player.velocity - prevVelocity;
        
        RotationSlamVector += collideVel;
        RotationSlamVector = Vector3.Lerp(RotationSlamVector, new Vector3(), Time.deltaTime * 8);
        rotationSlamVectorLerp = Vector3.Lerp(rotationSlamVectorLerp, RotationSlamVector, Time.deltaTime * 8);
        
        PositionSlamVector += collideVel;
        PositionSlamVector = Vector3.Lerp(PositionSlamVector, new Vector3(), Time.deltaTime * 8);
        positionSlamVectorLerp = Vector3.Lerp(positionSlamVectorLerp,  PositionSlamVector, Time.deltaTime * 8);
        
        prevVelocity = player.velocity;

        // Handle HUD momentum
        var yawMovement = prevYaw - player.Yaw;
        var pitchMovement = prevPitch - player.Pitch;
        if (yawMovement > 200) yawMovement = 0;
        hudYawOffset += yawMovement / hudMovementReduction;
        hudPitchOffset += pitchMovement / hudMovementReduction;
        hudYawOffset = Mathf.Lerp(hudYawOffset, 0, 0.05f);
        hudPitchOffset = Mathf.Lerp(hudPitchOffset, 0, 0.05f);
        var transform1 = transform;
        var hudTransform = transform1.localPosition;
        hudTransform.y = -positionSlamVectorLerp.y / 32;
        transform1.localPosition = hudTransform;
        transform.localRotation = Quaternion.Euler(-hudPitchOffset, -hudYawOffset, 0);
        
        prevYaw = player.Yaw;
        prevPitch = player.Pitch;
    }
}
