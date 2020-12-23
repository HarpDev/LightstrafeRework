using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dyson : MonoBehaviour
{

    private WeaponManager _weaponManager;

    private Mesh _mesh;

    public GameObject lightProjectile;

    private float _chargeTimer;

    private void Start()
    {
        _weaponManager = Game.Player.GetComponent<WeaponManager>();
        //_weaponManager.ShotEvent += GunShotEvent;
        //_gun.ShotEvent += GunShotEvent;
        //Game.Player.TriggerEvent += PlayerTriggerEvent;

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

    private void LateUpdate()
    {
        transform.Rotate(new Vector3(Time.deltaTime * 40, Time.deltaTime * 60, Time.deltaTime * 50));
        _playerTriggerCooldown = Mathf.Max(_playerTriggerCooldown - Time.deltaTime, 0);

        if (_chargeTimer > 0)
        {
            _chargeTimer -= Time.deltaTime;
            if (_chargeTimer <= 0)
            {
                _chargeTimer = 0;
                //_gun.ChargeHands();
            }
        }
    }

    private float _playerTriggerCooldown;

    public void PlayerTriggerEvent(Vector3 normal, Collider collider)
    {
        if (collider.gameObject != gameObject) return;
        if (_playerTriggerCooldown > 0) return;
        _playerTriggerCooldown = 2f;

        Game.Canvas.hitmarker.Display();
        //_gun.CatchAbility();

        int density = 8;

        _chargeTimer = 0.4f;

        for (int i = 0; i < _mesh.vertexCount; i++)
        {
            if (i % density != 0) continue;
            var vertex = _mesh.vertices[i];

            var script = Instantiate(lightProjectile).GetComponent<LightProjectile>();
            script.gameObject.transform.position = transform.position + vertex;
            script.Target = _weaponManager.EquippedGun.leftHandCenter;
            script.AnimTime = _chargeTimer;
        }
    }

    public void GunShotEvent(RaycastHit hit)
    {
        if (hit.collider.gameObject != gameObject) return;

        Game.Canvas.hitmarker.Display();

        int density = 8;

        _chargeTimer = 0.3f;
        //_gun.CatchAbility();
        _weaponManager.EquippedGun.DoAbilityCatch();

        for (int i = 0; i < _mesh.vertexCount; i++)
        {
            if (i % density != 0) continue;
            var vertex = _mesh.vertices[i];

            var script = Instantiate(lightProjectile).GetComponent<LightProjectile>();
            script.gameObject.transform.position = transform.position + vertex;
            script.Target = _weaponManager.EquippedGun.leftHandCenter;
            script.AnimTime = _chargeTimer;
        }
    }
}
