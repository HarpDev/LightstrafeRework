using System;
using System.Linq;
using UnityEngine;

public class Collectible : MonoBehaviour
{
    public GameObject NoSprite;
    public GameObject GemSprite;
    public GameObject[] Requires;

    private GameObject nosprite;
    private GameObject gemsprite;

    private void Update()
    {
        transform.Rotate(0, 0, 50 * Time.deltaTime);
        if (RequirementsMet())
        {
            if (gemsprite == null)
            {
                gemsprite = Instantiate(GemSprite, Game.Canvas.transform);
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
        return Requires.All(req => req == null) || Requires.Length == 0;
    }

    public void Collect()
    {
        if (!RequirementsMet()) return;
        if (gemsprite != null) Destroy(gemsprite);
        if (nosprite != null) Destroy(nosprite);
        Destroy(gameObject);
    }
}
