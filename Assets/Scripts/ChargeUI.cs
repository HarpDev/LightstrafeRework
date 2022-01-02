using UnityEngine;
using UnityEngine.UI;

public class ChargeUI : MonoBehaviour
{
    public GameObject UiBar;

    private GameObject[] bars;
    private float[] fillAmounts;

    private void Start()
    {
        bars = new GameObject[PlayerMovement.CHARGES];
        fillAmounts = new float[PlayerMovement.CHARGES];
        var spacing = 5;

        for (var i = 0; i < PlayerMovement.CHARGES; i++)
        {
            bars[i] = Instantiate(UiBar, transform);

            var spaceBetween = spacing + bars[i].GetComponent<RectTransform>().rect.width;
            var offset = (PlayerMovement.CHARGES - 1) * spaceBetween / 2;
            
            bars[i].transform.localPosition = new Vector3(i * spaceBetween - offset, 0, 0);
        }
    }

    private void Update()
    {
        var shouldShow = Game.Player.GrappleEnabled || Game.Player.DashEnabled;
        for (var i = 0; i < PlayerMovement.CHARGES; i++)
        {
            var amt = Mathf.Clamp01(Game.Player.Charges - i);
            var img = bars[i].GetComponent<Image>();
            img.fillAmount = amt;
            if (amt >= 1 && fillAmounts[i] < 1)
            {
                img.color = Color.white;
            }
            else
            {
                img.color = Color.Lerp(img.color, Color.gray, Time.deltaTime * 3);
            }
            fillAmounts[i] = amt;
            
            if (bars[i].activeSelf && !shouldShow)
            {
                bars[i].SetActive(false);
            }
            if (!bars[i].activeSelf && shouldShow)
            {
                bars[i].SetActive(true);
            }
        }
    }
}
