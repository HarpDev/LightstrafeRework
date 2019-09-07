using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class Bow : MonoBehaviour
{
    public GameObject top;
    public GameObject bottom;
    public Arrow arrowPrefab;
    public Arrow arrowModel;
    public PlayerMovement player;

    public LineRenderer bowString;

    public Vector3 boltPosition = new Vector3(0, -0.05f, 0.5f);

    public float yVelocityReduction = 100;
    public float yVelocityLimit = 0.4f;
    public float hVelocityReduction = 260;
    public float hVelocityLimit = 0.1f;

    public Vector3 bowPosition = new Vector3(0.3f, -0.35f, 0.8f);

    private float _lerpSpeed = 20;

    public float Drawback { get; set; }

    private MeshRenderer _mesh;

    private void Start()
    {
        _mesh = GetComponent<MeshRenderer>();
    }

    private void Update()
    {
        var m = _mesh.sharedMaterial;
        var c = m.color;
        if (PlayerMovement.DoubleJumpAvailable)
        {
            c.r = 1;
            c.g = 1;
            c.b = 1;
        }
        else
        {
            c.r = 0;
            c.g = 0;
            c.b = 0;
        }

        m.color = c;
        _mesh.sharedMaterial = m;

        var list = new List<Vector3> {top.transform.localPosition};

        var trans = transform;
        var activeTrans = arrowModel.transform;
        var position = trans.position;
        var ease = Drawback < .5
            ? 4 * Drawback * Drawback * Drawback
            : (Drawback - 1) * (2 * Drawback - 2) * (2 * Drawback - 2) + 1;
        activeTrans.position = position + trans.up * (boltPosition.z - ease / 2f) +
                               trans.forward * boltPosition.y + trans.right * boltPosition.x;
        activeTrans.rotation = trans.rotation;

        var relative = transform.InverseTransformPoint(arrowModel.nockPosition.position);
        list.Add(relative);

        list.Add(bottom.transform.localPosition);
        bowString.positionCount = list.Count;
        bowString.SetPositions(list.ToArray());

        var bowAngle = player.velocity.y * 1.8f - 10;

        bowAngle -= ease * 65;

        bowAngle = Mathf.Max(Mathf.Min(bowAngle, 0), -100);
        transform.localRotation = Quaternion.Lerp(transform.localRotation,
            Quaternion.Euler(new Vector3(90 - bowAngle, -90, -90)), Time.deltaTime * _lerpSpeed);

        var yCalc = player.velocity.y / yVelocityReduction;

        yCalc -= ease / 3f;

        yCalc = Mathf.Max(yCalc, -yVelocityLimit);
        yCalc = Mathf.Min(yCalc, yVelocityLimit / 6);

        var xCalc = player.velocity.x / hVelocityReduction;

        xCalc = Mathf.Max(xCalc, bowPosition.x);
        xCalc = Mathf.Min(xCalc, hVelocityLimit);

        var zCalc = player.velocity.z / hVelocityReduction;

        zCalc += ease / 5;

        zCalc = Mathf.Max(zCalc, -hVelocityLimit);
        zCalc = Mathf.Min(zCalc, hVelocityLimit);

        var finalPosition = bowPosition + new Vector3(xCalc, -yCalc, zCalc);

        if (player.IsGrounded) finalPosition += CameraBobbing.BobbingVector / 12;

        transform.localPosition = Vector3.Lerp(transform.localPosition, finalPosition, Time.deltaTime * _lerpSpeed);
        if (Input.GetAxis("Fire1") > 0)
        {
            if (Drawback < 1)
            {
                Drawback += Time.deltaTime;
                _lerpSpeed = 20;
            }
        }
        else if (Drawback > 0)
        {
            Fire(player.camera.transform.position, player.CrosshairDirection);
            _lerpSpeed = 3;
        }
    }

    public void Fire(Vector3 from, Vector3 vel)
    {
        var arrow = Instantiate(arrowPrefab.gameObject).GetComponent<Arrow>();
        arrow.transform.localScale = new Vector3(1, 1, 1);
        arrow.Fire(Quaternion.LookRotation(vel), Drawback * 150 * vel);
        arrow.transform.position = from;
        arrow.FiredVelocity = Flatten(player.velocity).magnitude;

        Drawback = 0;
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}