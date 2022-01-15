using System;
using System.Linq;
using UnityEngine;

public class Collectible : MonoBehaviour
{
    public GameObject NoSprite;
    public GameObject GemSprite;
    public int Requires;
    public GameObject Visual;

    private bool chasingPlayer;

    private GameObject nosprite;
    private GameObject gemsprite;

    private Vector3 start;

    private void Awake()
    {
        start = transform.position;
        adjust = start;
        chaseTimeStart = chaseTime;
    }

    private Player player;
    private CanvasManager canvasManager;
    private Level level;

    private void Start()
    {
        canvasManager = Game.OnStartResolve<CanvasManager>();
        level = Game.OnStartResolve<Level>();
        player = Game.OnStartResolve<Player>();
    }

    private Vector3 adjust;
    private float chaseTime = 0.5f;
    private float chaseTimeStart;

    private void Update()
    {
        if (RequirementsMet() && !chasingPlayer)
        {
            var target = player.camera.transform.position;
            var towardTarget = target - start;
            var adjustVector = towardTarget.normalized *
                               Mathf.Min(Mathf.Max(0, 15 - towardTarget.magnitude), towardTarget.magnitude);

            adjustVector -= player.velocity.normalized *
                            Vector3.Dot(player.velocity.normalized, adjustVector);

            adjustVector = adjustVector.normalized * Mathf.Min(adjustVector.magnitude, 5);
            
            adjust = Vector3.Lerp(adjust, start + adjustVector, Time.deltaTime * 5);
            Visual.transform.position = adjust;
        }

        if (chasingPlayer)
        {
            transform.position = Vector3.Lerp(player.camera.transform.position, start, chaseTime / chaseTimeStart);
            chaseTime -= Time.deltaTime;
        }

        transform.Rotate(0, 0, 50 * Time.deltaTime);
        if (RequirementsMet())
        {
            if (gemsprite == null)
            {
                gemsprite = Instantiate(GemSprite, canvasManager.baseCanvas.transform);
                gemsprite.transform.SetAsFirstSibling();
            }

            if (nosprite != null) Destroy(nosprite);

            var toScreen = player.camera.WorldToScreenPoint(transform.position);
            gemsprite.transform.position = toScreen;
            var scale = Mathf.Clamp01(1 -
                                      (Vector3.Distance(transform.position, player.camera.transform.position) -
                                       20) / 20);
            scale = 1 - scale;
            if (player.camera.WorldToViewportPoint(transform.position).z < 0)
            {
                scale = 0;
            }

            scale /= 4;

            gemsprite.transform.localScale = new Vector3(scale, scale, 1);
        }
        else
        {
            if (nosprite == null)
            {
                nosprite = Instantiate(NoSprite, canvasManager.baseCanvas.transform);
                nosprite.transform.SetAsFirstSibling();
            }

            if (gemsprite != null) Destroy(gemsprite);

            var toScreen = player.camera.WorldToScreenPoint(transform.position);
            nosprite.transform.position = toScreen;
            var scale = Mathf.Clamp01(1 - Vector3.Distance(transform.position, player.camera.transform.position) /
                30);
            if (player.camera.WorldToViewportPoint(transform.position).z < 0)
            {
                scale = 0;
            }

            nosprite.transform.localScale = new Vector3(scale, scale, 1);
        }
    }

    private bool RequirementsMet()
    {
        return Requires <= 0;
    }

    public void Collect()
    {
        if (!RequirementsMet()) return;
        if (gemsprite != null) Destroy(gemsprite);
        if (nosprite != null) Destroy(nosprite);
        if (chasingPlayer)
        {
            var objects = FindObjectsOfType<Collectible>();
            foreach (var gem in objects)
            {
                gem.GetComponent<Collectible>().Requires--;
            }

            if (objects.Length == 1)
            {
                level.LevelFinished();
            }

            player.AudioManager.PlayOneShot(player.wow, false, 0.4f);

            Destroy(gameObject);
        }
        else
        {
            transform.position = Visual.transform.position;
            start = transform.position;
            Visual.transform.localPosition = Vector3.zero;
            GetComponent<SphereCollider>().radius = 1f;
            chasingPlayer = true;
        }
    }
}