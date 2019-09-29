using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class CurvedLineRenderer : MonoBehaviour
{
    //PUBLIC
    public float lineSegmentSize = 0.15f;
    public float lineWidth = 0.1f;
    [Header("Gizmos")] public bool showGizmos = true;
    public float gizmoSize = 0.1f;

    public Color gizmoColor = new Color(1, 0, 0, 0.5f);

    //PRIVATE
    private CurvedLinePoint[] _linePoints = new CurvedLinePoint[0];
    private Vector3[] _linePositions = new Vector3[0];
    private Vector3[] _linePositionsOld = new Vector3[0];

    // Update is called once per frame
    public void Update()
    {
        GetPoints();
        SetPointsToLine();
    }

    private void GetPoints()
    {
        //find curved points in children
        _linePoints = GetComponentsInChildren<CurvedLinePoint>();

        //add positions
        _linePositions = new Vector3[_linePoints.Length];
        for (var i = 0; i < _linePoints.Length; i++)
        {
            _linePositions[i] = _linePoints[i].transform.position;
        }
    }

    private void SetPointsToLine()
    {
        //create old positions if they dont match
        if (_linePositionsOld.Length != _linePositions.Length)
        {
            _linePositionsOld = new Vector3[_linePositions.Length];
        }

        //check if line points have moved
        var moved = false;
        for (var i = 0; i < _linePositions.Length; i++)
        {
            //compare
            if (_linePositions[i] != _linePositionsOld[i])
            {
                moved = true;
            }
        }

        //update if moved
        if (moved)
        {
            var line = GetComponent<LineRenderer>();

            //get smoothed values
            var smoothedPoints = LineSmoother.SmoothLine(_linePositions, lineSegmentSize);

            //set line settings
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

        //settings for gizmos
        foreach (var linePoint in _linePoints)
        {
            linePoint.showGizmo = showGizmos;
            linePoint.gizmoSize = gizmoSize;
            linePoint.gizmoColor = gizmoColor;
        }
    }
}