using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PseudoCheckpoint : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        Game.I.Player.ding.Play();
        Game.Score += Mathf.RoundToInt(100 + Game.I.Player.velocity.magnitude);
        Destroy(gameObject);
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}
