using System;
using UnityEngine;
using UnityEngine.UI;

public class Collectible : MonoBehaviour
{
    public const int MARGIN = 50;

    public GameObject NoSprite;
    public GameObject GemSprite;
    public Collectible PreviousCollectible;
    public int LeftToCollect { get; set; }

    private GameObject nosprite;
    private GameObject gemsprite;

    public GameObject visual;

    public Transform quickspawn;
    private Vector3 quickspawnVelocity;

    private Player player;
    private CanvasManager canvasManager;
    private Level level;

    private float initialPresentation;

    public bool CollectedInQuickspawn { get; set; }

    private void Start()
    {
        canvasManager = Game.OnStartResolve<CanvasManager>();
        level = Game.OnStartResolve<Level>();
        player = Game.OnStartResolve<Player>();
        LeftToCollect = FindObjectsOfType<Collectible>().Length;

        var vel = GetComponentInChildren<QuickspawnVelocity>();
        if (vel != null) quickspawnVelocity = vel.Velocity;
    }

    private void LateUpdate()
    {
        if (player == null) return;

        visual.transform.Rotate(0, 0, 50 * Time.deltaTime);
        if (RequirementsMet)
        {
            initialPresentation = Mathf.Clamp01(initialPresentation + Time.deltaTime);
            if (gemsprite == null)
            {
                gemsprite = Instantiate(GemSprite, canvasManager.screenSizeCanvas.transform);
                gemsprite.transform.SetAsFirstSibling();
            }

            if (nosprite != null) Destroy(nosprite);

            var img = gemsprite.GetComponent<Image>();
            img.color = LeftToCollect == 1 ? Color.yellow : Color.white;
            var toScreen = player.camera.WorldToScreenPoint(transform.position);
            gemsprite.transform.position = toScreen;
            if (toScreen.z < 0)
            {
                var direction = gemsprite.transform.localPosition.normalized;
                var size = canvasManager.screenSizeCanvas.GetComponent<CanvasScaler>().referenceResolution.x;
                var offScreen = direction * (gemsprite.transform.localPosition.magnitude + size);
                
                gemsprite.transform.localPosition = offScreen;
            }

            var scale = Mathf.Clamp01(1 -
                                      (Vector3.Distance(transform.position, player.camera.transform.position) -
                                       20) / 20);
            scale = 1 - scale;
            scale /= 1.8f;

            gemsprite.transform.localScale = new Vector3(scale, scale, 1);

            var intendedPosition = gemsprite.transform.localPosition;
            gemsprite.transform.localPosition = Vector3.Lerp(Vector3.zero, intendedPosition, Mathf.Pow(initialPresentation, 3));
        }
        else
        {
            if (nosprite == null)
            {
                nosprite = Instantiate(NoSprite, canvasManager.screenSizeCanvas.transform);
                nosprite.transform.SetAsFirstSibling();
            }

            if (gemsprite != null) Destroy(gemsprite);

            var toScreen = player.camera.WorldToScreenPoint(transform.position);
            nosprite.transform.position = toScreen;
            var scale = Mathf.Clamp01(1 - Vector3.Distance(transform.position, player.camera.transform.position) /
                70);
            var toPlayer = player.camera.transform.position - transform.position;
            if (Physics.Raycast(transform.position, toPlayer.normalized, out var hit,
                toPlayer.magnitude, 1, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.gameObject != player.gameObject) scale = 0;
            }

            if (player.camera.WorldToViewportPoint(transform.position).z < 0)
            {
                scale = 0;
            }

            nosprite.transform.localScale = new Vector3(scale, scale, 1);
        }
    }

    public bool RequirementsMet => PreviousCollectible == null || !PreviousCollectible.gameObject.activeSelf;

    public void Collect()
    {
        if (!RequirementsMet) return;
        if (gemsprite != null) Destroy(gemsprite);
        if (nosprite != null) Destroy(nosprite);
        var objects = FindObjectsOfType<Collectible>();
        foreach (var gem in objects)
        {
            var coll = gem.GetComponent<Collectible>();
            coll.LeftToCollect = objects.Length - 1;
        }

        if (objects.Length == 1)
        {
            level.LevelFinished();
        }

        player.AudioManager.PlayOneShot(player.wow, false, 0.4f);
        player.Recharge();
        player.Recharge();
        player.DoubleJumpAvailable = true;

        if (quickspawn != null && quickspawn.gameObject.activeSelf)
            player.SetQuickDeathPosition(quickspawn.position, quickspawn.rotation.eulerAngles.y, quickspawnVelocity);

        gameObject.SetActive(false);
    }
    
    private void OnDrawGizmos()
    {
        if (quickspawn != null && quickspawn.gameObject.activeSelf)
        {
            Gizmos.color = Color.cyan;
            
            Gizmos.DrawRay(quickspawn.position, quickspawn.forward);
            Gizmos.DrawSphere(quickspawn.position, 0.5f);
        }
    }
}