using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoomerangProjectile : MonoBehaviour
{

    public float velocity;
    public Vector3 direction;
    public Vector3 rotation;

    public GameObject boomerang;

    private float curve = 5f;

    private SeekingDestructable nearest;

    private Vector3[] path;

    private void Start()
    {
        var targets = FindObjectsOfType<SeekingDestructable>();

        foreach (var obj in targets)
        {
            var distance = (transform.position - obj.transform.position).magnitude;

            if (distance > 100) continue;

            if (nearest == null)
            {
                nearest = obj;
                continue;
            }
            if (distance < (transform.position - nearest.transform.position).magnitude)
            {
                nearest = obj;
            }
        }

        var controls = new Vector3[4];
        controls[0] = Game.Player.camera.transform.position;
        if (nearest == null)
        {
            controls[3] = Game.Player.camera.transform.position + Game.Player.CrosshairDirection * 50;
        } else
        {
            controls[3] = nearest.transform.position;
        }

        var middle = Vector3.Lerp(controls[3], controls[0], 0.5f);
        var tangent = Game.Player.CrosshairDirection - ((controls[3] - controls[0]).normalized * Vector3.Dot(Game.Player.CrosshairDirection, (controls[3] - controls[0]).normalized));
        controls[2] = middle + (tangent.normalized * (controls[0] - controls[3]).magnitude);

        controls[1] = Game.Player.camera.transform.position + Game.Player.CrosshairDirection * 25;

        path = getCurvePoints(controls, 0.02f);
    }

    private bool hitTarget = false;

    private void Update()
    {
        velocity += Time.deltaTime * 10;
        transform.position += direction * velocity * Time.deltaTime;
        boomerang.transform.Rotate(rotation * Time.deltaTime, Space.Self);

        if (hitTarget)
        {
            var atTarget = Game.Player.camera.transform.position - transform.position;
            curve += Time.deltaTime;
            direction = Vector3.Lerp(direction, atTarget.normalized, Time.deltaTime * curve);
            direction = direction.normalized;
            if (atTarget.magnitude < 5 && Vector3.Dot(atTarget, direction) > 0)
            {
                var forwardAngle = Vector3.Angle(-Game.Player.CrosshairDirection, direction);
                var dot = Vector3.Dot(direction, Game.Player.transform.right);
                if (forwardAngle < 0)
                {
                    Game.Player.weaponManager.EquippedGun.BoomerangCatch(2);
                }
                else
                {
                    if (dot < 0)
                    {
                        Game.Player.weaponManager.EquippedGun.BoomerangCatch(0);
                    }
                    else
                    {
                        Game.Player.weaponManager.EquippedGun.BoomerangCatch(1);
                    }
                }
                Destroy(gameObject);
            }
        }
        else
        {

            var closeIndex = 0;
            var closeDistance = float.MaxValue;
            for (var i = 0; i < path.Length; i++)
            {
                var close = path[i];
                var distance = Vector3.Distance(transform.position, close);
                if (distance > closeDistance) continue;
                closeDistance = distance;
                closeIndex = i;
            }
            if (closeIndex + 1 < path.Length)
            {
                var alongPath = path[closeIndex + 1] - path[closeIndex];
                direction = alongPath.normalized;
            }
            else
            {
                if (nearest != null)
                    nearest.HitArbitrary();
                hitTarget = true;
            }
        }

        var target = Quaternion.LookRotation(direction, Vector3.up);
        if (hitTarget)
            transform.rotation = target;
        else
            transform.rotation = Quaternion.Lerp(transform.rotation, target, Time.deltaTime * 2);
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
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
        for (var i = 1; i < controlPoints.Count - 1; i += 4)
        {
            var a0 = controlPoints[i - 1];
            var a1 = controlPoints[i];
            var a2 = controlPoints[i + 1];

            if (i + 2 > controlPoints.Count - 1)
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
                var a3 = controlPoints[i + 2];

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

}
