using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayerMovement;

public class GimmickPickup : MonoBehaviour
{
    public Gimmick gimmick;

    private void Update()
    {
        transform.Rotate(new Vector3(1, 2, 3) * Time.deltaTime * 20);
    }
}
