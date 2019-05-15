using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class Bow : MonoBehaviour
{
    public GameObject top;
    public GameObject bottom;
    public Arrow arrowPrefab;

    public LineRenderer bowString;

    public Vector3 boltPosition = new Vector3(0, -0.05f, 0.5f);

    public float yVelocityReduction = 100;
    public float yVelocityLimit = 0.4f;
    public float hVelocityReduction = 260;
    public float hVelocityLimit = 0.1f;

    public Arrow ActiveArrow { get; set; }

    public float Drawback { get; set; }

    private void Start()
    {
        if (Application.isPlaying) NockArrow();
    }

    private void Update()
    {
        var list = new List<Vector3> {top.transform.localPosition};

        if (Application.isPlaying && !ActiveArrow.Fired)
        {
            var trans = transform;
            var activeTrans = ActiveArrow.gameObject.transform;
            var position = trans.position;
            activeTrans.position = position + trans.up * (boltPosition.z - Drawback * 1.5f) +
                                   trans.forward * boltPosition.y + trans.right * boltPosition.x;
            activeTrans.rotation = trans.rotation;

            var relative = transform.InverseTransformPoint(ActiveArrow.nockPosition.position);
            list.Add(relative);
        }

        list.Add(bottom.transform.localPosition);
        bowString.positionCount = list.Count;
        bowString.SetPositions(list.ToArray());
    }

    private void NockArrow()
    {
        ActiveArrow = Instantiate(arrowPrefab.gameObject).GetComponent<Arrow>();
        ActiveArrow.gameObject.GetComponent<Rigidbody>().isKinematic = true;
    }

    public void Fire(Vector3 from, Vector3 vel, Vector3 add)
    {
        if (!ActiveArrow.Fired)
        {
            ActiveArrow.Fire(Quaternion.LookRotation(vel), Drawback * 250 * vel + add);
            ActiveArrow.transform.position = from - vel;
        }

        Drawback = 0;
        NockArrow();
    }
}