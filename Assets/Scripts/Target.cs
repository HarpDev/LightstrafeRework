using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayerMovement;

public class Target : MonoBehaviour
{

    public GameObject debris;
    public Ability ability;

    private void Update()
    {
        if (!exploded) transform.Rotate(new Vector3(1, 2, 3) * Time.deltaTime * 20);
    }

    private bool exploded = false;

    public void Explode(Vector3 direction)
    {
        debris.transform.parent = null;
        exploded = true;
        debris.SetActive(true);
        foreach (var body in debris.GetComponentsInChildren<Rigidbody>())
        {
            body.AddTorque(new Vector3(Random.Range(-50, 50), Random.Range(-50, 50), Random.Range(-50, 50)));
            var force = direction.normalized * 150 + new Vector3(Random.Range(-100, 100), Random.Range(-100, 100), Random.Range(-100, 100));
            body.AddForce(force * 10);
        }
        Game.Level.player.AddAbility(ability);
        Destroy(gameObject);
    }
}
