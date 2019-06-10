
using UnityEngine;

public class ExplosiveArrow : MonoBehaviour
{

    public Arrow arrow;
    public ParticleSystem explosion;

    private void Start()
    {
        arrow.OnArrowCollide += ArrowCollide;
    }

    private void ArrowCollide(RaycastHit hit)
    {
        explosion.Play();
        Invoke("DestroySelf", 5f);
        Destroy(arrow.model);
        if (Game.I.Player != null)
        {
            var position = transform.position;
            var lookDir = Game.I.Player.GetComponent<PlayerControls>().cameraHudMovement.camera.transform.position - position;
            var add = Flatten(Vector3.RotateTowards(new Vector3(1, 0, 0), lookDir, 360, 0.0f)).normalized;
            var multiply = 30 - Vector3.Distance(position, Game.I.Player.transform.position) * 2;
            multiply = Mathf.Min(20, multiply);
            multiply = Mathf.Max(0, multiply);
            add *= multiply;
            Game.I.Player.GetComponent<PlayerControls>().velocity += add;
        }
    }

    private Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }

    private void DestroySelf()
    {
        Destroy(gameObject);
    }
}
