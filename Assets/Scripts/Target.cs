using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{

    public GameObject debris;

    private void Update()
    {
        if (!exploded) transform.Rotate(new Vector3(1, 2, 3) * Time.deltaTime * 20);
    }

    private bool exploded = false;

    public void Explode(Vector3 direction)
    {
        exploded = true;
        debris.SetActive(true);
        foreach (var body in debris.GetComponentsInChildren<Rigidbody>())
        {
            body.AddTorque(new Vector3(Random.Range(-50, 50), Random.Range(-50, 50), Random.Range(-50, 50)));
            var force = direction.normalized * 150 + new Vector3(Random.Range(-100, 100), Random.Range(-100, 100), Random.Range(-100, 100));
            body.AddForce(force * 10);
        }
        gameObject.GetComponent<MeshRenderer>().enabled = false;
        gameObject.GetComponent<BoxCollider>().enabled = false;
        Invoke("End", 4f);
    }

    public void End()
    {
        Destroy(gameObject);
    }
}
