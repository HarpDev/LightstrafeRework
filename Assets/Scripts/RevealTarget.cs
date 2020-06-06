using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RevealTarget : MonoBehaviour
{
        
    private void Start()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            var obj = transform.GetChild(i);
            obj.gameObject.SetActive(false);
        }

        Gun.ShotEvent += new Gun.GunShot(ShotEvent);
    }

    private void ShotEvent(RaycastHit hit)
    {
        Game.Canvas.hitmarker.Display();
        for (int i = 0; i < transform.childCount; i++)
        {
            var obj = transform.GetChild(i);
            obj.gameObject.SetActive(true);
        }
        GetComponent<MeshRenderer>().enabled = false;
        GetComponent<ParticleSystem>().Play();
    }
}
