using System.Linq;
using UnityEngine;

public class Collectible : MonoBehaviour
{
    public GameObject NoSprite;
    public GameObject GemSprite;
    public int Requires;

    private GameObject nosprite;
    private GameObject gemsprite;

    private Vector3 start;

    private void Awake()
    {
        start = transform.position;
        adjust = start;
    }

    private Vector3 adjust;
    private void Update()
    {

        if (RequirementsMet())
        {
            var target = Game.Player.camera.transform.position;
            var towardTarget = target - start;
            var adjustVector = towardTarget.normalized *
                               Mathf.Min(Mathf.Max(0, 15 - towardTarget.magnitude), towardTarget.magnitude);

            adjustVector -= Game.Player.velocity.normalized *
                            Vector3.Dot(Game.Player.velocity.normalized, adjustVector);

            adjustVector = adjustVector.normalized * Mathf.Min(adjustVector.magnitude, 5);

            adjust = Vector3.Lerp(adjust, start + adjustVector, Time.deltaTime * 5);
            transform.position = adjust;
        }
        
        transform.Rotate(0, 0, 50 * Time.deltaTime);
        if (RequirementsMet())
        {
            if (gemsprite == null)
            {
                gemsprite = Instantiate(GemSprite, Game.Canvas.transform);
                gemsprite.transform.SetAsFirstSibling();
            }
            if (nosprite != null) Destroy(nosprite);

            var toScreen = Game.Player.camera.WorldToScreenPoint(transform.position);
            gemsprite.transform.position = toScreen;
            var scale = Mathf.Clamp01(1 - (Vector3.Distance(transform.position, Game.Player.camera.transform.position) - 20) / 20);
            scale = 1 - scale;
            if (Game.Player.camera.WorldToViewportPoint(transform.position).z < 0)
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
                nosprite = Instantiate(NoSprite, Game.Canvas.transform);
                nosprite.transform.SetAsFirstSibling();
            }
            if (gemsprite != null) Destroy(gemsprite);

            var toScreen = Game.Player.camera.WorldToScreenPoint(transform.position);
            nosprite.transform.position = toScreen;
            var scale = Mathf.Clamp01(1 - Vector3.Distance(transform.position, Game.Player.camera.transform.position) / 30);
            if (Game.Player.camera.WorldToViewportPoint(transform.position).z < 0)
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
        var objects = FindObjectsOfType<Collectible>();
        foreach (var gem in objects)
        {
            gem.GetComponent<Collectible>().Requires--;
        }
        if (objects.Length == 1)
        {
            Game.EndTimer();
        }
        
        Game.Player.AudioManager.PlayOneShot(Game.Player.wow, false, 0.4f);
        
        Destroy(gameObject);
    }
}
