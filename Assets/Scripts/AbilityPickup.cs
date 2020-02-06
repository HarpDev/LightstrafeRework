using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayerMovement;

public class AbilityPickup : MonoBehaviour
{
    public Ability ability;

    private void Update()
    {
        transform.Rotate(new Vector3(1, 2, 3) * Time.deltaTime * 20);
    }
}
