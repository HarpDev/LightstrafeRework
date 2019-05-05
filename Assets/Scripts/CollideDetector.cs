
using UnityEngine;

public class CollideDetector : MonoBehaviour
{
    public Arrow arrow;

    private Vector3 prevPosition;

    private void Update()
    {
        var trans = transform;
        var lookDir = trans.position - prevPosition;
        var vec = Vector3.RotateTowards(new Vector3(1, 0, 0), lookDir, 360, 0.0f);
        RaycastHit hit;
        Physics.Raycast(prevPosition, vec, out hit, lookDir.magnitude);
        if (hit.collider != null) arrow.Collide(hit);
        
        prevPosition = transform.position;
    }
}