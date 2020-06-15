using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarSystem : MonoBehaviour
{

    private ParticleSystem.Particle[] points;

    public Color starColor;
    private int _starsMax = 600;
    private float _starSize = 1f;
    private float _starDistance = 80f;
    private float _starClipDistanceNear = 15f;
    private float _starClipDistanceFar = 50f;

    private System.Random _random;

    private void CreateStars()
    {
        points = new ParticleSystem.Particle[_starsMax];
        _random = new System.Random();

        for (int i = 0; i < _starsMax; i++)
        {
            var x = _random.Next((int)-_starDistance, (int)_starDistance);
            var z = _random.Next((int)-_starDistance, (int)_starDistance);
            var y = _random.Next((int)(_starDistance - _starClipDistanceFar), (int)_starDistance);
            points[i].position = new Vector3(x, y, z) + transform.position;
            points[i].startColor = starColor;
            points[i].startSize = 0;
        }
    }

    private void Update()
    {
        if (points == null) CreateStars();

        for (int i = 0; i < _starsMax; i++)
        {
            var sqrDistance = (points[i].position - transform.position).sqrMagnitude;

            if (sqrDistance > Mathf.Pow(_starDistance, 2))
            {
                var x = _random.Next((int)-_starDistance, (int)_starDistance);
                var z = _random.Next((int)-_starDistance, (int)_starDistance);
                var y = _random.Next((int)(_starDistance - _starClipDistanceFar), (int)_starDistance);
                points[i].position = new Vector3(x, y, z).normalized * _starDistance + transform.position;
            }

            if (sqrDistance > Mathf.Pow(_starDistance, 2) - Mathf.Pow(_starClipDistanceFar, 2))
            {
                float percentage = 1 - ((sqrDistance - (Mathf.Pow(_starDistance, 2) - Mathf.Pow(_starClipDistanceFar, 2))) / Mathf.Pow(_starClipDistanceFar, 2));
                //points[i].startColor = new Color(starColor.r, starColor.g, starColor.b, starColor.a * percentage);
                points[i].startSize = percentage * _starSize;
            }

            if (sqrDistance <= Mathf.Pow(_starClipDistanceNear, 2))
            {
                float percentage = sqrDistance / Mathf.Pow(_starClipDistanceNear, 2);
                //points[i].startColor = new Color(starColor.r, starColor.g, starColor.b, starColor.a * percentage);
                points[i].startSize = percentage * _starSize;
            }
        }
        GetComponent<ParticleSystem>().SetParticles(points, points.Length);
    }

}
