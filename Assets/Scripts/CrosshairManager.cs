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

    private float crosshairScale;
    private Vector4 crosshairGrey = new Color(0.39f, 0.39f, 0.39f, 0.39f);
    private Vector4 crosshairBlue = new Color(0f, 0.8f, 1f, 1f);
    private Vector4 crosshairGreen = new Color(0f, 0.8f, 0f, 1f);
    private Vector4 crosshairWhite = new Color(1f, 1f, 1f, 1f);

    private void Update()
    {
        if (player == null)
        {
            crosshair.color = new Color(0, 0, 0, 0);
            return;
        }

        var active = 0f;
        var fadeRange = 25f;
        var isInteractible = false;
        if (player.GrappleEnabled || player.DashEnabled)
        {
            if (player.GrappleDashCast(out var hit, out var howFarBeyond, fadeRange))
            {
                if (hit.transform.gameObject.GetComponent<MapInteractable>() != null) isInteractible = true;
                active = 1f;
            }
            else
            {
                active = 1 - (howFarBeyond / fadeRange);
            }
        }
        
        if (player.IsDashing || player.GrappleHooked) {
            crosshair.color = crosshairWhite;
            crosshair.transform.rotation = Quaternion.Euler(0, 0, 45);
        } else {
            crosshair.transform.rotation = Quaternion.Euler(0, 0,
                Mathf.Lerp(crosshair.transform.rotation.eulerAngles.z, 45 * active, Time.deltaTime * 20));
            
            crosshairScale = Mathf.Lerp(crosshairScale, active >= 1f ? 1.35f : 1f, Time.deltaTime * 20);

            var activeColor = crosshairBlue;
            if (isInteractible) activeColor = crosshairGreen;
            
            crosshair.color = Vector4.Lerp(crosshair.color, active >= 1f ? activeColor : crosshairGrey, Time.deltaTime * 20);
        }
        crosshair.transform.localScale = new Vector3(crosshairScale, crosshairScale, crosshairScale);

    }
}
