using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class StringCurve : MonoBehaviour
{

    public Transform point1;
    public LineRenderer lineRenderer;
    public Vector3 direction;
    public float distance;
    public float curve;
    public float range = 100;
    public float slant { get; set; }

    public List<Vector3> list = new List<Vector3>();

    void Update()
    {
        list.Clear();
        var sqrtCurve = Mathf.Sqrt(curve);
        var v = curve / sqrtCurve;
        if (curve == 0) v = 0;
        float d = 0;
        float x = 0;
        Vector3 prevPoint = point1.position;
        while (d < range)
        {
            // https://www.desmos.com/calculator/21qntsdtlb
            var y = -Mathf.Pow(x * 2 / distance * v - sqrtCurve, 2) + curve;

            var rotated = rotateAroundX(new Vector3(x, y, 0), slant);
            rotated = rotateAroundZ(rotated, Mathf.Asin(direction.y / direction.magnitude));
            rotated = rotateAroundY(rotated, -Mathf.Atan2(direction.z, direction.x));

            var linePoint = point1.position + rotated;
            list.Add(linePoint);
            x += 0.5f;

            d += Vector3.Distance(prevPoint, linePoint);
            prevPoint = linePoint;
        }
        lineRenderer.positionCount = list.Count;
        lineRenderer.SetPositions(list.ToArray());
    }

    private Vector3 rotateAroundX(Vector3 vec, float amt)
    {
        var y = vec.y * Mathf.Cos(amt) - vec.z * Mathf.Sin(amt);
        var z = vec.y * Mathf.Sin(amt) + vec.z * Mathf.Cos(amt);
        return new Vector3(vec.x, y, z);
    }

    private Vector3 rotateAroundY(Vector3 vec, float amt)
    {
        var x = vec.x * Mathf.Cos(amt) + vec.z * Mathf.Sin(amt);
        var z = -vec.x * Mathf.Sin(amt) + vec.z * Mathf.Cos(amt);
        return new Vector3(x, vec.y, z);
    }

    private Vector3 rotateAroundZ(Vector3 vec, float amt)
    {
        var x = vec.x * Mathf.Cos(amt) - vec.y * Mathf.Sin(amt);
        var y = vec.x * Mathf.Sin(amt) + vec.y * Mathf.Cos(amt);
        return new Vector3(x, y, vec.z);
    }
}
