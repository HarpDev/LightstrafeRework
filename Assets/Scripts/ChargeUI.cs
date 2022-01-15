using UnityEngine;
using UnityEngine.UI;

public class ChargeUI : MonoBehaviour
{
    public GameObject UiBar;

    private GameObject[] bars;
    private float[] fillAmounts;

    private Player player;

    private void Start()
    {
        player = Game.OnStartResolve<Player>();
        bars = new GameObject[Player.CHARGES];
        fillAmounts = new float[Player.CHARGES];
        var spacing = 5;

        for (var i = 0; i < Player.CHARGES; i++)
        {
            bars[i] = Instantiate(UiBar, transform);

            var spaceBetween = spacing + bars[i].GetComponent<RectTransform>().rect.width;
            var offset = (Player.CHARGES - 1) * spaceBetween / 2;
            
            bars[i].transform.localPosition = new Vector3(i * spaceBetween - offset, 0, 0);
        }
    }

    private void Update()
    {
        var shouldShow = player.GrappleEnabled || player.DashEnabled;
        for (var i = 0; i < Player.CHARGES; i++)
        {
            var amt = Mathf.Clamp01(player.Charges - i);
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
