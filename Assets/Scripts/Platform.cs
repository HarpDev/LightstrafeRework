using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class Platform : MonoBehaviour
{

    public MeshRenderer glow;
    public Light lightSource;

    private bool _glowing = false;

    private readonly float _projectileSpeed = 100;

    private Vector3 _lightProjectileVelocity;

    public GameObject lightProjectile;

    public ParticleSystem glowParticle;

    private Color _color;
    private float _range;
    private float _value;

    public bool startGlowing = false;
    public bool bouncePad = false;
    public float bouncePadStrength = 50;

    private GameObject _projectile;

    private bool _queued;

    private void Start()
    {
        if (bouncePad) Game.Player.PlayerJumpEvent += new PlayerMovement.Jump(BouncePadJump);
        if (startGlowing)
        {
            _queued = true;
            _glowing = true;
        }
        if (bouncePad)
        {
            color = Color.yellow;
        }
    }

    private Color color = Color.cyan;

    private void FixedUpdate()
    {
        if (_queued) return;
        if (_projectile != null) return;
        var distance = Vector3.Distance(Game.Player.transform.position, transform.position);

        var towardPlatform = (transform.position - Game.Player.transform.position).normalized;
        var angle = Vector3.Angle(Game.Player.CrosshairDirection, towardPlatform);
        if ((angle < 30 && distance < 100) || distance < 40)
        {
            if (Physics.Raycast(Game.Player.camera.transform.position, towardPlatform, out var hit, distance, 1, QueryTriggerInteraction.Ignore) && hit.collider.gameObject == gameObject)
            {
                _queued = true;
                var queue = Game.Player.rings.ThrowQueue;
                queue.Add(this);
                queue = queue.OrderBy(o => (o.transform.position - Game.Player.transform.position).sqrMagnitude).ToList();
                Game.Player.rings.ThrowQueue = queue;
            }
        }
    }

    private void BouncePadJump(ref PlayerMovement.JumpEvent jumpEvent)
    {
        if (jumpEvent.currentGround == gameObject && jumpEvent.type == PlayerMovement.JumpType.GROUND)
        {
            jumpEvent.jumpHeight = bouncePadStrength;
        }
    }

    private void Update()
    {

        if (_projectile != null && !_glowing)
        {
            var towardPlatform = (transform.position - _projectile.transform.position).normalized;
            _lightProjectileVelocity = Vector3.Lerp(_lightProjectileVelocity, towardPlatform * _projectileSpeed * 3, Time.deltaTime * 10);
            _projectile.transform.position += _lightProjectileVelocity * Time.deltaTime;
            if (Vector3.Distance(_projectile.transform.position, transform.position) < 2)
            {
                _glowing = true;
                glowParticle.Play();
                _projectile.GetComponent<MeshRenderer>().enabled = false;
                //Destroy(_projectile);
            }
        }

        //_glowing = Vector3.Distance(Game.Player.transform.position, transform.position) < 50;

        var brightness = 1.1f;

        if (_glowing)
        {
            _range = Mathf.Lerp(_range, 35, Time.deltaTime * 5);
            if (_value < brightness) _value += Time.deltaTime * 5;
            if (_value > brightness) _value = brightness;
        }
        else
        {
            _range = Mathf.Lerp(_range, 0, Time.deltaTime);
            if (_value > 0) _value -= Time.deltaTime;
            if (_value < 0) _value = 0;
        }

        Color.RGBToHSV(color * 2, out var h, out var s, out _);
        _color = Color.HSVToRGB(h, s, Mathf.Pow(_value, 2));
        lightSource.range = _range;
        glow.material.SetColor("_EmissionColor", _color);
        glow.material.EnableKeyword("_EMISSION");
    }

    public void BeginLight(Vector3 projectileStart, Vector3 projectileDirection)
    {
        _lightProjectileVelocity = projectileDirection.normalized * _projectileSpeed;

        _projectile = Instantiate(lightProjectile);
        _projectile.transform.position = projectileStart;
    }
}
