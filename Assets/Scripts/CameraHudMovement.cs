using System;
using UnityEngine;

public class CameraHudMovement : MonoBehaviour
{
    public new Camera camera;

    public PlayerControls player;
    
    private float hudYawOffset;
    private float hudPitchOffset;
    public float hudMovementReduction = 6;
    
    private const float BobbingSpeed = 0.58f;
    private const float BobbingWidth = 0.2f;
    private const float BobbingHeight = 0.2f;
    private float bobbingPos;
    
    public GameObject hud;

    private const float Tolerance = 0.05f;

    public Vector3 RotationSlamVector { get; set; }
    public Vector3 PositionSlamVector { get; set; }
    private Vector3 rotationSlamVectorLerp;
    private Vector3 positionSlamVectorLerp;

    private float prevYaw;
    private float prevPitch;

    private Vector3 prevVelocity;
    
    public Vector3 BobbingVector { get; set; }

    private void Update()
    {

        // Camera bobbing
        if (Math.Abs(player.velocity.magnitude) > Tolerance && player.isGrounded())
        {
            bobbingPos += Flatten(player.velocity).magnitude * BobbingSpeed * Time.deltaTime * 2;
            while (bobbingPos > Mathf.PI * 2) bobbingPos -= Mathf.PI * 2;

            var y = BobbingHeight * Mathf.Sin(bobbingPos * 2);
            var x = BobbingWidth * Mathf.Sin(bobbingPos + 1.8f);
            BobbingVector = new Vector3(x, y, 0);
            camera.transform.localPosition = Vector3.Lerp(camera.transform.localPosition,
                player.Sprinting ? BobbingVector * 3 : new Vector3(), Time.deltaTime);
        }
        camera.transform.rotation = Quaternion.Euler(new Vector3(player.Pitch + rotationSlamVectorLerp.y, player.Yaw, 0));

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
        var hudTransform = hud.transform.localPosition;
        hudTransform.y = -positionSlamVectorLerp.y / 32;
        hud.transform.localPosition = hudTransform;
        hud.transform.localRotation = Quaternion.Euler(-hudPitchOffset, -hudYawOffset, 0);
        
        prevYaw = player.Yaw;
        prevPitch = player.Pitch;
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}
