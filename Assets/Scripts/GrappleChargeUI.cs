using UnityEngine;
using UnityEngine.UI;

public class GrappleChargeUI : MonoBehaviour
{
    public GameObject UiBar;

    private GameObject[] bars;

    private void Start()
    {
        bars = new GameObject[PlayerMovement.GRAPPLE_CHARGES];
        var spacing = 5;

        for (var i = 0; i < PlayerMovement.GRAPPLE_CHARGES; i++)
        {
            bars[i] = Instantiate(UiBar, transform);

            var spaceBetween = spacing + bars[i].GetComponent<RectTransform>().rect.width;
            var offset = (PlayerMovement.GRAPPLE_CHARGES - 1) * spaceBetween / 2;
            
            bars[i].transform.localPosition = new Vector3(i * spaceBetween - offset, 0, 0);
        }
    }

    private void Update()
    {
        for (var i = 0; i < PlayerMovement.GRAPPLE_CHARGES; i++)
        {
            var amt = Mathf.Clamp01(Game.Player.GrappleCharges - i);
            bars[i].GetComponent<Image>().fillAmount = amt;
            if (bars[i].activeSelf && !Game.Player.GrappleEnabled)
            {
                bars[i].SetActive(false);
            }
            if (!bars[i].activeSelf && Game.Player.GrappleEnabled)
            {
                bars[i].SetActive(true);
            }
        }
    }
}
