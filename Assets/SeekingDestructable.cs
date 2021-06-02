using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeekingDestructable : MonoBehaviour
{
    public AudioClip hitSound;

    private float lethalDistance = 30;
    private float armDistance = 100;

    private MeshRenderer mesh;

    public GameObject hurtBox;

    private bool seeking = false;
    private float speed = 0;

    private void Start()
    {
        mesh = GetComponent<MeshRenderer>();
        Game.Player.weaponManager.ShotEvent += Hit;
    }

    public void Hit(RaycastHit hit, ref bool doReload)
    {
        if (hit.collider.gameObject != hurtBox) return;

        var playerTarget = Game.Player.camera.transform.position;
        var towardPlayer = playerTarget - transform.position;
        if (towardPlayer.magnitude >= armDistance) return;
        Game.Canvas.hitmarker.Display();
        doReload = false;
        Game.Player.audioManager.PlayOneShot(hitSound);
        Game.Player.weaponManager.ShotEvent -= Hit;
        Destroy(gameObject);
    }

    private void Update()
    {
        var playerTarget = Game.Player.camera.transform.position;

        var towardPlayer = playerTarget - transform.position;

        if (towardPlayer.magnitude < lethalDistance)
        {
            seeking = true;
        }
        if (seeking)
        {
            mesh.material.SetFloat("_FlowSpeed", towardPlayer.magnitude);

            speed += Time.deltaTime * 20f;
            transform.position += towardPlayer.normalized * Mathf.Min(towardPlayer.magnitude, speed * Time.deltaTime);

            var color = mesh.material.GetColor("_RimColor");
            mesh.material.SetColor("_RimColor", Color.Lerp(color, Color.red, Time.deltaTime * 10));
        }
        else
        {
            var targetColor = Color.black;
            if (towardPlayer.magnitude < armDistance)
            {
                targetColor = Color.red / 1.5f;
            }

            var color = mesh.material.GetColor("_RimColor");
            mesh.material.SetColor("_RimColor", Color.Lerp(color, targetColor, Time.deltaTime * 10));
        }
    }
}
