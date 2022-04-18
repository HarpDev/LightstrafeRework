using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movable : MonoBehaviour, MapInteractable
{
    public Transform moveTo;
    public float speed = 2;

    public LineRenderer lineRenderer;

    private Vector3 pos1;
    private Quaternion rot1;
    private Vector3 pos2;
    private Quaternion rot2;

    private int direction = -1;
    private float factor;

    public void Proc(RaycastHit hit)
    {
        direction *= -1;
    }

    private void Start()
    {
        pos1 = transform.position;
        rot1 = transform.rotation;
        pos2 = moveTo.position;
        rot2 = moveTo.rotation;

        if (lineRenderer != null)
        {
            var positions = new Vector3[2];
            positions[0] = pos1;
            positions[1] = pos2;
            lineRenderer.SetPositions(positions);
        }
    }

    private void Update()
    {
        //factor += Time.deltaTime * direction * (speed / 2);
        //factor = Mathf.Clamp01(factor);
        //transform.position = Vector3.Lerp(pos1, pos2, factor);
        //transform.rotation = Quaternion.Lerp(rot1, rot2, factor);
    }

    private void FixedUpdate()
    {
        factor += Time.fixedDeltaTime * direction * speed;
        factor = Mathf.Clamp01(factor);
        transform.position = Vector3.Lerp(pos1, pos2, factor);
        transform.rotation = Quaternion.Lerp(rot1, rot2, factor);
    }
}
