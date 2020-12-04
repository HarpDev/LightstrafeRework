using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dyson : MonoBehaviour
{

    private Gun _gun;

    private Mesh _mesh;

    public Transform targetTransform;
    public GameObject lightProjectile;

    private float _chargeTimer;

    private void Start()
    {

        _gun = Game.Player.gameObject.GetComponentInChildren<Gun>();
        _gun.ShotEvent += GunShotEvent;

        var meshes = GetComponentsInChildren<MeshFilter>();

        int smallestIndex = 0;
        int smallestVertices = int.MaxValue;
        for (int i = 0; i < meshes.Length; i++)
        {
            var m = meshes[i].mesh;
            if (m.vertexCount < smallestVertices)
            {
                smallestVertices = m.vertexCount;
                smallestIndex = i;
            }
        }
        _mesh = meshes[smallestIndex].mesh;
    }

    private float spin;

    private void LateUpdate()
    {
        transform.Rotate(new Vector3(Time.deltaTime * 40, Time.deltaTime * 60, Time.deltaTime * 50));

        if (_chargeTimer > 0)
        {
            _chargeTimer -= Time.deltaTime;
            if (_chargeTimer <= 0)
            {
                _chargeTimer = 0;
                _gun.ChargeHands();
            }
        }
    }

    public void GunShotEvent(RaycastHit hit, ref bool doCatch)
    {
        if (hit.collider.gameObject != gameObject) return;

        Game.Canvas.hitmarker.Display();
        doCatch = true;

        int density = 8;

        _chargeTimer = 0.9f;

        for (int i = 0; i < _mesh.vertexCount; i++)
        {
            if (i % density != 0) continue;
            var vertex = _mesh.vertices[i];

            var script = Instantiate(lightProjectile).GetComponent<LightProjectile>();
            script.gameObject.transform.position = transform.position + vertex;
            script.Target = targetTransform;
        }
    }
}
