using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bit : MonoBehaviour
{

    public CurvedLineRenderer path;

    private void Start()
    {
        transform.position = path.linePositions[0];
    }

    private void Update()
    {
        var closeIndex = 0;
        var closeDistance = float.MaxValue;
        for (var i = 0; i < path.smoothedPoints.Length; i++)
        {
            var close = path.smoothedPoints[i];
            var distance = Vector3.Distance(transform.position, close);
            if (distance > closeDistance) continue;
            closeDistance = distance;
            closeIndex = i;
        }
        if (closeIndex == path.smoothedPoints.Length - 1 || path.smoothedPoints.Length == 0) return;

        var forward = (path.smoothedPoints[closeIndex + 1] - transform.position).normalized;

        var speed = Mathf.Max(35 - Vector3.Distance(transform.position, Game.Player.transform.position), 0);

        var fromPlayer = (transform.position - Game.Player.transform.position).normalized;
        var projection = Vector3.Dot(fromPlayer, forward);
        if (projection < 0) speed = 50;

        transform.position += forward * Time.deltaTime * speed;
    }
}
