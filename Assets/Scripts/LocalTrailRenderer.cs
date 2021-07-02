using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalTrailRenderer : MonoBehaviour
{

    private LineRenderer line;

    public float distIncrement = 0.1f;
    private Vector3 lastPosition;

    public float life = 0.1f;

    private bool worldSpace = true;

    private float[] positionTimes;

    private void Start()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = worldSpace;
        Reset();
        positionTimes = new float[1000];
    }

    private void Reset()
    {
        line.positionCount = 0;
    }

    private void AddPoint()
    {
        line.positionCount++;
        line.SetPosition(line.positionCount - 1, transform.position);
        positionTimes[line.positionCount - 1] = life;

        lastPosition = transform.position;
    }

    public void Lock()
    {
        AddPoint();
        var linePoints = new Vector3[line.positionCount];
        line.GetPositions(linePoints);
        for (int i = 0; i < linePoints.Length; i++)
        {
            linePoints[i] = transform.InverseTransformPoint(linePoints[i]);
        }
        line.SetPositions(linePoints);
        worldSpace = false;
    }


    private void Update()
    {
        var linePoints = new Vector3[line.positionCount];
        line.GetPositions(linePoints);

        for (int i = 0; i < linePoints.Length; i++)
        {
            if (positionTimes[i] <= 0) continue;
            positionTimes[i] -= Time.deltaTime;
            if (positionTimes[i] <= 0)
            {
                positionTimes[i] = 0;

                for (int a = 1; a < linePoints.Length; a++)
                {
                    linePoints[a - 1] = linePoints[a];
                    positionTimes[a - 1] = positionTimes[a];
                }

                line.positionCount--;
            }
        }
        line.SetPositions(linePoints);
        line.useWorldSpace = worldSpace;
        if (!worldSpace) return;
        if (Vector3.Distance(transform.position, lastPosition) > distIncrement)
        {
            AddPoint();
        }
    }
}