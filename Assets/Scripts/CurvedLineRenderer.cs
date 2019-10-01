using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class CurvedLineRenderer : MonoBehaviour
{
    public float lineSegmentSize = 0.15f;
    public float lineWidth = 0.1f;
    [Header("Gizmos")] public bool showGizmos = true;
    public float gizmoSize = 0.1f;

    public Color gizmoColor = new Color(1, 0, 0, 0.5f);

    private CurvedLinePoint[] _linePoints = new CurvedLinePoint[0];
    private Vector3[] _linePositions = new Vector3[0];
    private Vector3[] _linePositionsOld = new Vector3[0];

    public LineRenderer line;

    public void Update()
    {
        GetPoints();
        SetPointsToLine();
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

    public Vector3[] smoothedPoints;

    private void SetPointsToLine()
    {
        if (_linePositionsOld.Length != _linePositions.Length)
        {
            _linePositionsOld = new Vector3[_linePositions.Length];
        }

        var moved = false;
        foreach (var point in _linePoints)
        {
            if (!point.transform.hasChanged) continue;
            moved = true;
            point.transform.hasChanged = false;
        }

        if (moved)
        {
            line = GetComponent<LineRenderer>();

            smoothedPoints = LineSmoother.SmoothLine(_linePositions, lineSegmentSize);

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
                    capsule.height = (previousPoint - point).magnitude + 0.5f;
                    capsule.radius = lineWidth;
                    capsule.direction = 2;
                    capsule.isTrigger = true;
                    obj.transform.rotation = Quaternion.LookRotation((previousPoint - point).normalized);
                }

                first = false;
                previousPoint = point;
            }

            line.positionCount = smoothedPoints.Length;
            line.SetPositions(smoothedPoints);
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
        }
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