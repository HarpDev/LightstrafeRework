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

    private Player player;
    private CanvasManager canvasManager;
    private Level level;

    public bool ChasingPlayer { get; private set; }

    private void Start()
    {
        canvasManager = Game.OnStartResolve<CanvasManager>();
        level = Game.OnStartResolve<Level>();
        player = Game.OnStartResolve<Player>();
        LeftToCollect = 5;
    }

    private void LateUpdate()
    {
        if (player == null) return;

        transform.Rotate(0, 0, 50 * Time.deltaTime);
        if (RequirementsMet)
        {
            if (gemsprite == null)
            {
                gemsprite = Instantiate(GemSprite, canvasManager.screenSizeCanvas.transform);
                gemsprite.transform.SetAsFirstSibling();
            }

            if (nosprite != null) Destroy(nosprite);

            var img = gemsprite.GetComponent<Image>();
            img.color = LeftToCollect == 1 ? Color.yellow : Color.green;

            var toScreen = player.camera.WorldToScreenPoint(transform.position);
            var scale = Mathf.Clamp01(1 -
                                      (Vector3.Distance(transform.position, player.camera.transform.position) -
                                       20) / 20);
            scale = 1 - scale;
            if (toScreen.x < MARGIN ||
                toScreen.y < MARGIN ||
                toScreen.x > Screen.width - MARGIN ||
                toScreen.y > Screen.height - MARGIN ||
                player.camera.WorldToViewportPoint(transform.position).z < 0)
            {
                var edge = new Vector3(toScreen.x, toScreen.y, toScreen.z);
                if (player.camera.WorldToViewportPoint(transform.position).z < 0)
                {
                    edge = new Vector3(Screen.width - toScreen.x, Screen.height - toScreen.y, toScreen.z);
                }

                edge.x -= Screen.width / 2f;
                edge.y -= Screen.height / 2f;

                var x = edge.x > 0 ? Screen.width / 2f : -Screen.width / 2f;
                var horizontalEdge = new Vector2(x, x * edge.y / edge.x);

                var y = edge.y > 0 ? Screen.height / 2f : -Screen.height / 2f;
                var verticalEdge = new Vector2(y * edge.x / edge.y, y);

                if (horizontalEdge.sqrMagnitude > verticalEdge.sqrMagnitude)
                {
                    edge.x = verticalEdge.x;
                    edge.y = verticalEdge.y;
                }
                else
                {
                    edge.x = horizontalEdge.x;
                    edge.y = horizontalEdge.y;
                }

                edge.x += Screen.width / 2f;
                edge.y += Screen.height / 2f;

                toScreen = edge;

                var scaleMod = Mathf.Clamp01(-Vector3.Dot(player.CrosshairDirection,
                    (transform.position - player.camera.transform.position).normalized) * 10);
                scale *= 1 + scaleMod;
            }

            scale /= 1.8f;

            if (toScreen.x < MARGIN) toScreen.x = MARGIN;
            if (toScreen.y < MARGIN) toScreen.y = MARGIN;
            if (toScreen.x > Screen.width - MARGIN) toScreen.x = Screen.width - MARGIN;
            if (toScreen.y > Screen.height - MARGIN) toScreen.y = Screen.height - MARGIN;
            gemsprite.transform.position = toScreen;
            gemsprite.transform.localScale = new Vector3(scale, scale, 1);
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

    public bool RequirementsMet => PreviousCollectible == null;

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

        Destroy(gameObject);
    }
}