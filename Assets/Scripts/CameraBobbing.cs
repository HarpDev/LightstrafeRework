using System;
using UnityEngine;

public class CameraBobbing : MonoBehaviour
{

    public PlayerControls player;
    
    private const float Tolerance = 0.05f;
    
    private const float BobbingSpeed = 0.58f;
    private const float BobbingWidth = 0.2f;
    private const float BobbingHeight = 0.2f;
    private float bobbingPos;
    
    public static Vector3 BobbingVector { get; set; }

    // Update is called once per frame
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
            player.camera.transform.localPosition = Vector3.Lerp(player.camera.transform.localPosition, new Vector3(), Time.deltaTime);
        }
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}
