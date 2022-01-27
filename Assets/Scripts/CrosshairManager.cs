using UnityEngine;
using UnityEngine.UI;

public class CrosshairManager : MonoBehaviour
{
    public Image crosshair;

    private void Awake()
    {
        Game.OnAwakeBind(this);
    }

    private Player player;

    private void Start()
    {
        player = Game.OnStartResolve<Player>();
    }

    private float crosshairColor;

    private void Update()
    {
        if (player == null)
        {
            crosshair.color = new Color(0, 0, 0, 0);
            return;
        }

        var active = 0f;
        var fadeRange = 20f;
        if (player.GrappleEnabled || player.DashEnabled)
        {
            if (player.GrappleDashCast(out var hit, out var howFarBeyond, fadeRange))
            {
                active = 1f;
            }
            else
            {
                active = 1 - (howFarBeyond / fadeRange);
            }
        }

        crosshair.transform.rotation = Quaternion.Euler(0, 0,
            Mathf.Lerp(crosshair.transform.rotation.eulerAngles.z, 45 * active, Time.deltaTime * 20));
        crosshairColor = Mathf.Lerp(crosshairColor, active >= 1f ? 1 : 100 / 255f, Time.deltaTime * 20);

        crosshair.color = new Color(crosshairColor, crosshairColor, crosshairColor, crosshairColor);
    }
}