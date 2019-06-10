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
    public Hitmarker hitmarker;

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

    public void Fire(Vector3 from, Vector3 vel, Vector3 add)
    {
        var arrow = Instantiate(arrowPrefab.gameObject).GetComponent<Arrow>();
        arrow.Hitmarker = hitmarker;
        arrow.Fire(Quaternion.LookRotation(vel), Drawback * 250 * vel + add);
        arrow.transform.position = from;

        Drawback = 0;
    }
}