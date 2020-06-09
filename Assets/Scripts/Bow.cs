using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class Bow : MonoBehaviour
{
    public GameObject top;
    public GameObject bottom;
    public Arrow arrow;
    public PlayerMovement player;

    public LineRenderer bowString;

    private const float hVelocityReduction = 260;
    private const float hVelocityLimit = 0.1f;
    private const float lerpSpeed = 30;

    private Vector3 bowPosition = new Vector3(0.3f, -0.45f, 0.5f);
    private Vector3 boltPosition = new Vector3(-0.15f, -0.05f, 0.5f);

    private Vector3 _angleEulers;

    public float Drawback { get; set; }

    private void Update()
    {

        var list = new List<Vector3> { top.transform.localPosition };

        var trans = transform;
        var activeTrans = arrow.transform;
        var position = trans.position;

        var bowAngle = player.velocity.y * 1.8f - 10;
        var bowPos = bowPosition;
        var boltPos = boltPosition;

        if (Application.isPlaying && player.IsSliding)
        {
            bowAngle -= Drawback * 10;
            bowAngle += 180;
            bowAngle = -bowAngle;
            bowPos.x -= 0.58f;
            bowAngle = Mathf.Max(Mathf.Min(bowAngle, -150), -180);

            transform.localScale = new Vector3(1, 1, -1);
            boltPos.y = -boltPos.y;
        }
        else
        {
            bowPos.y += Drawback / 12;
            bowAngle -= Drawback * 65;
            bowAngle = Mathf.Max(Mathf.Min(bowAngle, 0), -100);

            transform.localScale = new Vector3(1, 1, 1);
        }

        activeTrans.position = position + trans.up * (boltPos.z - Drawback / 2.3f) +
                               trans.forward * boltPos.y + trans.right * boltPos.x;
        activeTrans.rotation = trans.rotation;

        var relative = transform.InverseTransformPoint(arrow.nockPosition.position);
        list.Add(relative);

        list.Add(bottom.transform.localPosition);
        bowString.positionCount = list.Count;
        bowString.SetPositions(list.ToArray());

        _angleEulers = Vector3.Lerp(_angleEulers, new Vector3(90 - bowAngle, -90, -90), Time.deltaTime * lerpSpeed);
        transform.localRotation = Quaternion.Euler(_angleEulers);

        if (player.IsSliding)
            transform.Rotate(new Vector3(-1, 0, 0), Space.Self);

        var rightVelocity = Vector3.Dot(player.velocity, Flatten(player.transform.right));
        var xCalc = rightVelocity / hVelocityReduction;

        xCalc = Mathf.Max(xCalc, bowPosition.x);
        xCalc = Mathf.Min(xCalc, hVelocityLimit);

        var forwardVelocity = Vector3.Dot(player.velocity, Flatten(player.transform.forward));
        var zCalc = forwardVelocity / hVelocityReduction;

        zCalc += Drawback / 5;

        zCalc = Mathf.Max(zCalc, -hVelocityLimit);
        zCalc = Mathf.Min(zCalc, hVelocityLimit);

        var finalPosition = bowPos + new Vector3(xCalc, 0, zCalc);

        if (player.GroundLevel == 0 && !player.IsOnRail && player.WallLevel == 0)
            finalPosition.y += 0f;
        else if (!player.IsSliding)
            finalPosition.y += Drawback / 3f;

        //if (player.GroundLevel > 0) finalPosition += CameraBobbing.BobbingVector;

        transform.localPosition = Vector3.Lerp(transform.localPosition, finalPosition, Time.deltaTime * lerpSpeed);
        if (Input.GetKey(PlayerInput.PrimaryInteract))
        {
            if (Drawback < 1)
            {
                Drawback += Time.deltaTime * 2f;
            }
        }
        else
        {
            if (Drawback >= 0.8f)
            {
                Fire(player.camera.transform.position, player.CrosshairDirection);
            }
            else if (Drawback > 0)
            {
                Drawback -= Time.deltaTime * 2f;
            }
        }
    }

    public void Fire(Vector3 from, Vector3 vel)
    {
        var a = Instantiate(arrow.gameObject).GetComponent<Arrow>();
        a.gameObject.layer = player.gameObject.layer;
        a.model.layer = player.gameObject.layer;

        a.transform.localScale = new Vector3(1, 1, 1);
        var velocity = Drawback * 100 * vel;
        a.Fire(Quaternion.LookRotation(vel), velocity);
        a.transform.position = from + vel * 4;
        a.FiredVelocity = Flatten(player.velocity).magnitude;
        Drawback = 0;
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}