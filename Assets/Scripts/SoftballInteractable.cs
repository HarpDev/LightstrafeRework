using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoftballInteractable : MonoBehaviour, MapInteractable
{
    private float explosionTime;
    private Player player;

    private void Start()
    {
        player = Game.OnStartResolve<Player>();
    }

    public void Proc(RaycastHit hit)
    {
        explosionTime = 0.25f;
    }

    private void Update()
    {
        if (explosionTime > 0)
        {
            explosionTime -= Time.deltaTime;
        }
        if (explosionTime < 0)
        {
            var towardPlayer = player.transform.position - transform.position;
            var beforeVel = player.velocity;
            var verticalBonus = Mathf.Clamp01(towardPlayer.normalized.y) * 30;

            if (towardPlayer.y > 0 && player.velocity.y < 0)
            {
                player.velocity.y = 0;
            }

            player.Accelerate(towardPlayer.normalized, 40 + verticalBonus, 0.7f);
            player.DoubleJumpAvailable = true;
            if (player.velocity.magnitude < beforeVel.magnitude)
            {
                player.velocity = player.velocity.normalized * beforeVel.magnitude;
            }
            if (player.Speed < Flatten(beforeVel).magnitude)
            {
                player.Speed = Flatten(beforeVel).magnitude;
            }
            explosionTime = 0;
        }
    }
    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}
