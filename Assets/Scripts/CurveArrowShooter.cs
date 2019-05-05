using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurveArrowShooter : MonoBehaviour
{

    public GameObject bolt;
    public StringCurve stringCurve;
    public Vector3 boltPosition = new Vector3(0.3f, -0.35f, 0.8f);

    public PlayerControls playerControls;

    private GameObject activeBolt;
    private GameObject activeLine;

    private Vector3 lerpPosition = new Vector3(0, 0, 0);

    private float drawback = 0;

    private void Start()
    {
        lerpPosition = playerControls.camera.transform.position;
    }

    private float prevYaw = 0;
    private float prevPitch = 0;

    private float curve = 0;
    private float slant = 0;

    private void Update()
    {
        if (activeBolt == null) activeBolt = Instantiate(bolt);

        var r = Mathf.Sqrt(Mathf.Pow(boltPosition.x, 2) + Mathf.Pow(boltPosition.y, 2) + Mathf.Pow(boltPosition.z - drawback, 2));
        var t = Mathf.Acos(boltPosition.y / r);
        var p = Mathf.Atan2(boltPosition.z - drawback, boltPosition.x);
        t += Mathf.Deg2Rad * playerControls.Pitch;
        p -= Mathf.Deg2Rad * playerControls.Yaw;
        var x = r * Mathf.Sin(t) * Mathf.Cos(p);
        var y = r * Mathf.Cos(t);
        var z = r * Mathf.Sin(t) * Mathf.Sin(p);

        lerpPosition = Vector3.Lerp(lerpPosition, playerControls.camera.transform.position + new Vector3(x, y, z), 0.5f);
        activeBolt.transform.position = lerpPosition;
        activeBolt.transform.rotation = Quaternion.Euler(playerControls.Pitch, playerControls.Yaw, 0);

        if (Input.GetAxis("Fire1") > 0)
        {
            if (drawback < 0.3f) drawback += Time.deltaTime / 4;
            if (activeLine == null)
            {
                activeLine = Instantiate(stringCurve.gameObject);
                curve = 0;
                slant = 0;
            }
            var line = activeLine.GetComponent<StringCurve>();
            line.point1.position = lerpPosition;
            line.direction = playerControls.camera.transform.forward;
            line.distance = drawback * 50;
            lookAt(activeBolt.transform, line.list[3]);
        }
        else if (drawback > 0)
        {
            Destroy(activeLine);
            drawback = 0;
        }
    }

    private void FixedUpdate()
    {
        if (Input.GetAxis("Fire1") > 0)
        {
            var line = activeLine.GetComponent<StringCurve>();
            float polarDistance = Mathf.Sqrt(Mathf.Pow(prevYaw - playerControls.Yaw, 2) + Mathf.Pow(prevPitch - playerControls.Pitch, 2));
            Debug.Log(polarDistance);
            if (polarDistance > 0.5f)
                curve += polarDistance / 10;
            line.curve = curve = Mathf.Min(Mathf.Lerp(curve, 0, 0.2f), drawback * 50 / 8);

            float inverseAngle = Mathf.Atan2(prevYaw - playerControls.Yaw, prevPitch - playerControls.Pitch) + Mathf.Deg2Rad * 180;
            slant = Mathf.Lerp(slant, inverseAngle, 0.4f);
            if (polarDistance > 0.5f)
                line.slant = slant;
        }
        prevYaw = playerControls.Yaw;
        prevPitch = playerControls.Pitch;
    }

    private void lookAt(Transform trans, Vector3 vec2)
    {
        float x = vec2.x - trans.position.x;
        float y = vec2.y - trans.position.y;
        float z = vec2.z - trans.position.z;

        float r = Mathf.Sqrt(Mathf.Pow(x, 2) + Mathf.Pow(y, 2) + Mathf.Pow(z, 2));
        float t = Mathf.Acos(y / r) + Mathf.Deg2Rad * 90;
        float p = -Mathf.Atan2(z, x) + Mathf.Deg2Rad * 90;

        trans.rotation = Quaternion.Euler(new Vector3(t * Mathf.Rad2Deg, p * Mathf.Rad2Deg, 0));
    }
}
