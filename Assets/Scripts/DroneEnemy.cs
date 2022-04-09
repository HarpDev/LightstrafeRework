using UnityEngine;

public class DroneEnemy : MonoBehaviour
{
    public GameObject visual;
    private Vector3 visualStart;

    private Player player;
    private void Start()
    {
        player = Game.OnStartResolve<Player>();
        visualStart = visual.transform.position;
    }

    private void Update()
    {
        visual.transform.position = visualStart + Vector3.up * Mathf.Sin(Time.time * 2);

        var ogRot = visual.transform.rotation;

        visual.transform.LookAt(player.transform.position);

        var atPlayer = visual.transform.rotation;

        visual.transform.rotation = Quaternion.Lerp(ogRot, atPlayer, Time.deltaTime * 5);
        var rot = visual.transform.rotation;
        var euler = rot.eulerAngles;
        euler.x = 0;
        euler.z = 0;
        rot.eulerAngles = euler;
        visual.transform.rotation = rot;
    }
}
