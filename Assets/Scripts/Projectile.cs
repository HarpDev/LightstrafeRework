
using UnityEngine;

public class Projectile : MonoBehaviour
{

    public Vector3 velocity;

    private const float basespeed = 100;

    public ParticleSystem explodeParticle;
    public AudioSource explodeSound;

    public GameObject visual;

    public Radial radial;

    private const float drop = 3f;

    private bool _hit;

    private Target _hitTarget;

    public void Fire(Vector3 vel, Vector3 realPosition, Vector3 visualPosition)
    {
        transform.position = realPosition;
        visual.transform.position = visualPosition;
        velocity = vel;
        velocity += velocity.normalized * basespeed;
        visual.transform.rotation = Quaternion.LookRotation(-velocity);
    }

    private void Update()
    {
        if (_hit) return;

        visual.transform.rotation = Quaternion.Lerp(visual.transform.rotation, Quaternion.LookRotation(-velocity), Time.deltaTime * 10);
        visual.transform.position = Vector3.Lerp(visual.transform.position, transform.position, Time.deltaTime * 8);
    }

    private void FixedUpdate()
    {
        if (_hit) return;

        if (Physics.Raycast(transform.position, velocity.normalized, out var hit, velocity.magnitude * Time.fixedDeltaTime, 1, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.CompareTag("Player")) return;
            transform.position = hit.point;
            _hit = true;

            visual.transform.localPosition = new Vector3();

            _hitTarget = hit.collider.gameObject.GetComponent<Target>();
            if (_hitTarget != null) Game.Player.hitmarker.Display();

            if (hit.collider.CompareTag("Target"))
            {
                _hitTarget.Explode();
                visual.GetComponent<MeshRenderer>().enabled = false;
            }
        } else
        {
            transform.position += velocity * Time.fixedDeltaTime;
        }

        velocity += Vector3.down * Time.fixedDeltaTime * drop;
    }
}
