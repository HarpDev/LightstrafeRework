using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoubleJump : MonoBehaviour
{
    public PlayerControls player;

    public float harshness = 5;

    public static bool doubleJumpSpent;

    private void Update()
    {
        if (player.movementEnabled)
        {
            if (Input.GetAxis("Jump") > 0 && !doubleJumpSpent && !player.JumpLock && !player.isGrounded())
            {
                if (player.velocity.y < player.jumpHeight)
                    player.velocity.y = player.jumpHeight;

                player.AirAccelerate(player.Wishdir, harshness);

                player.jump.Play();
                doubleJumpSpent = true;
                player.JumpLock = true;
            }
            else if (player.isGrounded())
            {
                doubleJumpSpent = false;
            }
        }
    }
}