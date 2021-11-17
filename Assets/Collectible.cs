using UnityEngine;

public class Collectible : MonoBehaviour
{
    private void Update()
    {
        transform.Rotate(0, 0, 50 * Time.deltaTime);
    }

    public void Collect()
    {
        var objects = FindObjectsOfType<Collectible>();
        if (objects.Length == 1)
        {
            Game.EndTimer();
        }
        Game.Player.weaponManager.EquipGun(WeaponManager.GunType.Rifle);
        Destroy(gameObject);
    }
}
