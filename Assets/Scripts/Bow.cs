using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class Bow : MonoBehaviour
{
    public GameObject top;
    public GameObject bottom;
    public Arrow arrowPrefab;
    public Arrow arrowModel;

    public LineRenderer bowString;

    public Vector3 boltPosition = new Vector3(0, -0.05f, 0.5f);

    public float yVelocityReduction = 100;
    public float yVelocityLimit = 0.4f;
    public float hVelocityReduction = 260;
    public float hVelocityLimit = 0.1f;

    public float Drawback { get; set; }

    private void Update()
    {
        var list = new List<Vector3> {top.transform.localPosition};

        var trans = transform;
        var activeTrans = arrowModel.transform;
        var position = trans.position;
        activeTrans.position = position + trans.up * (boltPosition.z - Drawback * 1.5f) +
                               trans.forward * boltPosition.y + trans.right * boltPosition.x;
        activeTrans.rotation = trans.rotation;

        var relative = transform.InverseTransformPoint(arrowModel.nockPosition.position);
        list.Add(relative);

        list.Add(bottom.transform.localPosition);
        bowString.positionCount = list.Count;
        bowString.SetPositions(list.ToArray());
    }

    public void Fire(Vector3 from, Vector3 vel)
    {
        var arrow = Instantiate(arrowPrefab.gameObject).GetComponent<Arrow>();
        arrow.transform.localScale = new Vector3(1, 1, 1);
        arrow.Fire(Quaternion.LookRotation(vel), Drawback * 450 * vel);
        arrow.transform.position = from;
        arrow.FiredVelocity = Flatten(Game.I.Player.velocity).magnitude;

        Drawback = 0;
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}