using System;
using UnityEngine;

public class CameraBobbing : MonoBehaviour
{

    public PlayerMovement player;
    
    private const float Tolerance = 0.05f;
    
    private const float BobbingSpeed = 0.3f;
    private const float BobbingWidth = 0.05f;
    private const float BobbingHeight = 0f;
    private float _bobbingPos;
    
    public static Vector3 BobbingVector { get; set; }

    private void Update()
    {
        if (Math.Abs(player.velocity.magnitude) > Tolerance && player.IsGrounded && !player.IsSliding)
        {
            _bobbingPos += Flatten(player.velocity).magnitude * BobbingSpeed * Time.deltaTime * 2;
            while (_bobbingPos > Mathf.PI * 2) _bobbingPos -= Mathf.PI * 2;

            var y = BobbingHeight * Mathf.Sin(_bobbingPos * 2);
            var x = BobbingWidth * Mathf.Sin(_bobbingPos + 1.8f);
            BobbingVector = new Vector3(x, y, 0);
        }
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}
