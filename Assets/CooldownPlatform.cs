using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CooldownPlatform : MonoBehaviour
{

    private const float COOLDOWN = 2.5f;
    private float cooldown;

    private Collider hitbox;

    private Vector3 startScale;

    private float size;

    private void Start()
    {
        hitbox = GetComponent<Collider>();
        startScale = transform.localScale;
    }

    public void PutOnCooldown()
    {
        hitbox.enabled = false;
        cooldown = COOLDOWN;
        startScale = transform.localScale;
    }

    private void Update()
    {
        cooldown = Mathf.Max(cooldown - Time.deltaTime, 0);
        if (!hitbox.enabled && cooldown == 0)
        {
            hitbox.enabled = true;
        }

        if (cooldown > 0)
        {
            if (size > 0) size = Mathf.Max(0, size - Time.deltaTime * 3);

            var ease = 1 - Mathf.Pow(1 - size, 5);
            transform.localScale = startScale * ease;
        }

        if (cooldown == 0)
        {
            if (size < 1) size = Mathf.Min(1, size + Time.deltaTime * 1.2f);

            var n1 = 7.5625f;
            var d1 = 2.75f;

            var ease = size;
            if (ease < 1 / d1)
            {
                ease = n1 * ease * ease;
            }
            else if (ease < 2 / d1)
            {
                ease = n1 * (ease -= 1.5f / d1) * ease + 0.75f;
            }
            else if (ease < 2.5 / d1)
            {
                ease = n1 * (ease -= 2.25f / d1) * ease + 0.9375f;
            }
            else
            {
                ease = n1 * (ease -= 2.625f / d1) * ease + 0.984375f;
            }

            transform.localScale = startScale * ease;
        }

    }
}
