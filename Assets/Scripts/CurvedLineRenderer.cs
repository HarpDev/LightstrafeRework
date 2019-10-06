using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(LineRenderer))]
public class CurvedLineRenderer : MonoBehaviour
{
    [Header("Gizmos")] public bool showGizmos = true;
    public float detail = 0.02f;
    public float gizmoSize = 0.1f;
    public float hitboxSize = 1;

    public Vector3[] smoothedPoints;

    public Color gizmoColor = new Color(1, 0, 0, 0.5f);

    private CurvedLinePoint[] _linePoints = new CurvedLinePoint[0];
    private Vector3[] _linePositions = new Vector3[0];

    public LineRenderer line;

    public void Update()
    {
        GetPoints();
        if (_linePoints.Any(point => point.transform.hasChanged))
        {
            smoothedPoints = getCurvePoints(_linePositions, detail);
            GenerateHitboxes();

            line.positionCount = smoothedPoints.Length;
            line.SetPositions(smoothedPoints);
        }
        foreach (var point in _linePoints) point.transform.hasChanged = false;
    }


    public void GenerateHitboxes()
    {
        var previousPoint = new Vector3();
        var first = true;
        for (var i = transform.childCount - 1; i > 0; i--)
        {
            if (transform.GetChild(i).name == "hitbox")
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }
        foreach (var point in smoothedPoints)
        {
            if (!first)
            {
                if ((previousPoint - point).magnitude < 1f) continue;
                var obj = new GameObject {name = "hitbox"};
                obj.tag = "Rail";
                obj.transform.parent = gameObject.transform;
                var capsule = (CapsuleCollider) obj.AddComponent(typeof(CapsuleCollider));
                obj.transform.position = Vector3.Lerp(point, previousPoint, 0.5f);
                capsule.height = (previousPoint - point).magnitude + hitboxSize;
                capsule.radius = hitboxSize;
                capsule.direction = 2;
                capsule.isTrigger = true;
                obj.transform.rotation = Quaternion.LookRotation((previousPoint - point).normalized);
            }

            first = false;
            previousPoint = point;
        }
    }

    private void GetPoints()
    {
        _linePoints = GetComponentsInChildren<CurvedLinePoint>();

        _linePositions = new Vector3[_linePoints.Length];
        for (var i = 0; i < _linePoints.Length; i++)
        {
            _linePositions[i] = _linePoints[i].transform.position;
        }
    }

    /**
 * Generates several 3D points in a continuous Bezier curve based upon 
 * the parameter list of points. 
 * @param controls
 * @param detail
 * @return
 */
    public static Vector3[] getCurvePoints(Vector3[] controls, float detail)
    {
        if (detail > 1 || detail < 0)
        {
            // Illegal state
        }


        var renderingPoints = new List<Vector3>();
        var controlPoints = new List<Vector3>(controls);

        //Generate the detailed points. 
        for (var i = 0; i < controlPoints.Count - 2; i += 4)
        {
            var a0 = controlPoints[i];
            var a1 = controlPoints[i + 1];
            var a2 = controlPoints[i + 2];

            if (i + 3 > controlPoints.Count - 1)
            {
                //quad
                for (float j = 0; j < 1; j += detail)
                {
                    renderingPoints.Add(quadBezier(a0, a1, a2, j));
                }
            }
            else
            {
                //cubic
                var a3 = controlPoints[i + 3];

                for (float j = 0; j < 1; j += detail)
                {
                    renderingPoints.Add(cubicBezier(a0, a1, a2, a3, j));
                }
            }
        }

        return renderingPoints.ToArray();
    }


/**
 * A cubic bezier method to calculate the point at t along the Bezier Curve give
 * the parameter points.
 * @param p1
 * @param p2
 * @param p3
 * @param p4
 * @param t A value between 0 and 1, inclusive. 
 * @return
 */
    public static Vector3 cubicBezier(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, float t)
    {
        return new Vector3(
            cubicBezierPoint(p1.x, p2.x, p3.x, p4.x, t),
            cubicBezierPoint(p1.y, p2.y, p3.y, p4.y, t),
            cubicBezierPoint(p1.z, p2.z, p3.z, p4.z, t));
    }


/**
 * A quadratic Bezier method to calculate the point at t along the Bezier Curve give
 * the parameter points.
 * @param p1
 * @param p2
 * @param p3
 * @param t A value between 0 and 1, inclusive. 
 * @return
 */
    public static Vector3 quadBezier(Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        return new Vector3(
            quadBezierPoint(p1.x, p2.x, p3.x, t),
            quadBezierPoint(p1.y, p2.y, p3.y, t),
            quadBezierPoint(p1.z, p2.z, p3.z, t));
    }

/**
 * The cubic Bezier equation. 
 * @param a0
 * @param a1
 * @param a2
 * @param a3
 * @param t
 * @return
 */
    private static float cubicBezierPoint(float a0, float a1, float a2, float a3, float t)
    {
        return Mathf.Pow(1 - t, 3) * a0 + 3 * Mathf.Pow(1 - t, 2) * t * a1 + 3 * (1 - t) * Mathf.Pow(t, 2) * a2 +
               Mathf.Pow(t, 3) * a3;
    }


/**
 * The quadratic Bezier equation,
 * @param a0
 * @param a1
 * @param a2
 * @param t
 * @return
 */
    private static float quadBezierPoint(float a0, float a1, float a2, float t)
    {
        return Mathf.Pow(1 - t, 2) * a0 + 2 * (1 - t) * t * a1 + Mathf.Pow(t, 2) * a2;
    }


/**
 * Calculates the center point between the two parameter points.
 * @param p1
 * @param p2
 * @return
 */
    public static Vector3 center(Vector3 p1, Vector3 p2)
    {
        return new Vector3(
            (p1.x + p2.x) / 2,
            (p1.y + p2.y) / 2,
            (p1.z + p2.z) / 2
        );
    }

    private void OnDrawGizmosSelected()
    {
        Update();
    }

    private void OnDrawGizmos()
    {
        if (_linePoints.Length == 0)
        {
            GetPoints();
        }

        foreach (var linePoint in _linePoints)
        {
            linePoint.showGizmo = showGizmos;
            linePoint.gizmoSize = gizmoSize;
            linePoint.gizmoColor = gizmoColor;
        }
    }
}