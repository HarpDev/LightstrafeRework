using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;

public class Collectible : MonoBehaviour
{
    public const int MARGIN = 50;
    
    public GameObject NoSprite;
    public GameObject GemSprite;
    public int Requires;
    public int LeftToCollect { get; set; }
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
        LeftToCollect = 5;
    }

    private Vector3 adjust;
    private float chaseTime = 0.5f;
    private float chaseTimeStart;

    private void LateUpdate()
    {
        /*if (RequirementsMet() && !chasingPlayer)
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
        }*/
        if (player == null) return;

        if (chasingPlayer)
        {
            if (chaseTime <= 0)
            {
                transform.position = player.camera.transform.position;
            }
            else
            {
                transform.position = Vector3.Lerp(player.camera.transform.position, start, chaseTime / chaseTimeStart);
                chaseTime -= Time.deltaTime;
            }
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

            var img = gemsprite.GetComponent<Image>();
            img.color = LeftToCollect == 1 ? Color.yellow : Color.green;

            var toScreen = player.camera.WorldToScreenPoint(transform.position);
            var scale = Mathf.Clamp01(1 -
                                      (Vector3.Distance(transform.position, player.camera.transform.position) -
                                       20) / 20);
            scale = 1 - scale;
            if (toScreen.x < MARGIN ||
                toScreen.y < MARGIN ||
                toScreen.x > Screen.width - MARGIN||
                toScreen.y > Screen.height - MARGIN||
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
                scale *= 2;
            }

            scale /= 4;

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
                nosprite = Instantiate(NoSprite, canvasManager.baseCanvas.transform);
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
                var coll = gem.GetComponent<Collectible>();
                coll.Requires--;
                coll.LeftToCollect = objects.Length - 1;
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