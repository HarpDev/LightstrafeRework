
using UnityEngine;
using UnityEngine.Serialization;

public class Projectile : MonoBehaviour
{
    public Vector3 velocity;

    public ParticleSystem explodeParticle;
    public AudioSource explodeSound;

    public GameObject visual;

    public Radial radial;

    public float d = 5f;
    private float fuse = 0.35f;

    private bool hit;

    public void Fire(Vector3 vel, Vector3 realPosition, Vector3 visualPosition, float drop = 5f)
    {
        transform.position = realPosition;
        visual.transform.position = visualPosition;
        velocity = vel;
        d = drop;
        visual.transform.rotation = Quaternion.LookRotation(-velocity);
    }
    private Player player;
    private CanvasManager canvasManager;

    private void Start()
    {
        canvasManager = Game.OnStartResolve<CanvasManager>();
        player = Game.OnStartResolve<Player>();
    }

    private void Update()
    {
        if (hit) return;

        visual.transform.rotation = Quaternion.Lerp(visual.transform.rotation, Quaternion.LookRotation(-velocity), Time.deltaTime * 10);
        visual.transform.position = Vector3.Lerp(visual.transform.position, transform.position, Time.deltaTime * 8);
    }

    private void FixedUpdate()
    {
        if (this.hit)
        {
            if (fuse > 0)
            {
                fuse -= Time.fixedDeltaTime;
                if (fuse <= 0)
                {
                    if (explodeSound != null) explodeSound.Play();
                    
                    var boostVector = player.transform.position - transform.position;
                    player.velocity += Flatten(boostVector).normalized * 10;
                    
                    var r = Instantiate(radial.gameObject, canvasManager.baseCanvas.transform).GetComponent<Radial>();

                    var flatBoost = Flatten(boostVector).normalized;
                    r.position = -(Mathf.Rad2Deg * Mathf.Atan2(flatBoost.x, flatBoost.z) - player.Yaw + 180);
                    
                    Destroy(this, 3f);
                }
            }
            return;
        }

        if (Physics.Raycast(transform.position, velocity.normalized, out var hit, velocity.magnitude * Time.fixedDeltaTime, 1, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.CompareTag("Player")) return;
            transform.position = hit.point;
            this.hit = true;

            visual.transform.localPosition = new Vector3();

        } else
        {
            transform.position += velocity * Time.fixedDeltaTime;
        }

        velocity += Vector3.down * Time.fixedDeltaTime * d;
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}
