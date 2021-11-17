using System;
using UnityEngine;

public class BackgroundLine : MonoBehaviour
{
    private float time;
    private Vector3 interpPosition;

    public GameObject segment;

    public BackgroundManager manager;
    public float degreeOffset;

    private GameObject[] segments;
    private int segmentAmount = 40;
    private float offsetAmount = 200;

    private void Start()
    {
        segments = new GameObject[segmentAmount];
        for (var i = 0; i < segmentAmount; i++)
        {
            segments[i] = Instantiate(segment, gameObject.transform);
            var seg = segments[i].GetComponent<BackgroundSegment>();
            seg.yOffset = (i - (segmentAmount / 2)) * offsetAmount;
        }
    }

    private void Update()
    {
        if (manager.speed != 0) time += Time.deltaTime;
        var targetDegrees = (time * manager.speed) + degreeOffset;
        targetDegrees -= targetDegrees % 15;

        targetDegrees %= 360;

        for (var i = 0; i < segmentAmount; i++)
        {
            var seg = segments[i].GetComponent<BackgroundSegment>();
            if (i == 0)
            {
                seg.targetDegrees = targetDegrees;
            }
            else
            {
                if (seg.targetDegrees < 0) seg.targetDegrees = targetDegrees;
                else
                {
                    var prevseg = segments[i - 1].GetComponent<BackgroundSegment>();
                    seg.targetDegrees = Mathf.Lerp(seg.targetDegrees, prevseg.targetDegrees, Time.deltaTime * 30);
                }
            }
        }

        transform.position = Game.Player.camera.transform.position;
    }
}